using Cofoundry.Web.Internal;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// Runs domain queries and commands on behalf of the signed-in admin user from inside a Blazor
/// circuit. Each call uses its own short-lived DI scope, which avoids holding a long-lived
/// <c>CofoundryDbContext</c> for the lifetime of the circuit and lets the domain layer resolve
/// the current user without an <c>HttpContext</c>.
/// </summary>
public interface IAdminBlazorExecutor
{
    /// <summary>
    /// Executes a query as the current admin user. <see cref="NotPermittedException"/> and
    /// other domain exceptions propagate to the caller.
    /// </summary>
    Task<TResult> QueryAsync<TResult>(IQuery<TResult> query);

    /// <summary>
    /// Validates and executes a command as the current admin user. Validation and permission
    /// failures are returned in the result. Any output values (e.g. <c>OutputPageId</c>) are
    /// set on the command instance that was passed in.
    /// </summary>
    Task<AdminCommandResult> ExecuteAsync(ICommand command);

    /// <summary>
    /// Runs arbitrary work against services resolved from a fresh scope that is seeded with the
    /// current admin user, e.g. to check several permissions in one go.
    /// </summary>
    Task<TResult> WithServicesAsync<TResult>(Func<IServiceProvider, Task<TResult>> action);
}

/// <inheritdoc cref="IAdminBlazorExecutor"/>
public class AdminBlazorExecutor : IAdminBlazorExecutor
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public AdminBlazorExecutor(
        IServiceScopeFactory serviceScopeFactory,
        AuthenticationStateProvider authenticationStateProvider
        )
    {
        _serviceScopeFactory = serviceScopeFactory;
        _authenticationStateProvider = authenticationStateProvider;
    }

    public async Task<TResult> QueryAsync<TResult>(IQuery<TResult> query)
    {
        ArgumentNullException.ThrowIfNull(query);

        await using var scope = await CreateSeededScopeAsync();
        var repository = scope.ServiceProvider.GetRequiredService<IDomainRepository>();

        return await repository.ExecuteQueryAsync(query);
    }

    public async Task<AdminCommandResult> ExecuteAsync(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        await using var scope = await CreateSeededScopeAsync();

        var validationService = scope.ServiceProvider.GetRequiredService<IModelValidationService>();
        var errors = validationService.GetErrors(command).ToList();

        // The same annotations again, keeping the messages of nested data model items. If they pass, the
        // errors came from a replaced validation service, so its own are shown.
        if (errors.Count > 0)
        {
            var detailed = CommandValidationErrors.Get(command);
            if (detailed.Count > 0)
            {
                errors = detailed.ToList();
            }
        }

        if (errors.Count > 0)
        {
            return new AdminCommandResult(errors);
        }

        var repository = scope.ServiceProvider.GetRequiredService<IDomainRepository>();

        try
        {
            await repository.ExecuteCommandAsync(command);
        }
        catch (ValidationException ex)
        {
            errors.Add(MapValidationException(ex));
        }
        catch (NotPermittedException ex)
        {
            errors.Add(new ValidationError { Message = ex.Message });
        }
        catch (EntityNotFoundException ex)
        {
            // e.g. the record was deleted in another tab. The API returned a 404 for this.
            errors.Add(new ValidationError { Message = ex.Message });
        }

        return errors.Count == 0 ? AdminCommandResult.Success : new AdminCommandResult(errors);
    }

    public async Task<TResult> WithServicesAsync<TResult>(Func<IServiceProvider, Task<TResult>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        await using var scope = await CreateSeededScopeAsync();

        return await action(scope.ServiceProvider);
    }

    private async Task<AsyncServiceScope> CreateSeededScopeAsync()
    {
        var userId = await GetSignedInAdminUserIdAsync();
        var scope = _serviceScopeFactory.CreateAsyncScope();

        if (userId.HasValue)
        {
            // Seed before anything that depends on the user session is resolved from this scope.
            scope.ServiceProvider.GetRequiredService<AdminBlazorScopeUser>().Set(userId.Value);
        }

        return scope;
    }

    private async Task<int?> GetSignedInAdminUserIdAsync()
    {
        var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var user = state.User;

        var areaCode = user.FindFirst(CofoundryClaimTypes.UserAreaCode)?.Value;
        if (areaCode != CofoundryAdminUserArea.Code)
        {
            return null;
        }

        var userIdClaim = user.FindFirst(CofoundryClaimTypes.UserId)?.Value;

        return int.TryParse(userIdClaim, out var userId) && userId > 0 ? userId : null;
    }

    private static ValidationError MapValidationException(ValidationException ex)
    {
        return new ValidationError
        {
            Message = ex.ValidationResult?.ErrorMessage ?? ex.Message,
            Properties = ex.ValidationResult?.MemberNames?.ToArray() ?? [],
            ErrorCode = (ex as ValidationErrorException)?.ErrorCode
        };
    }
}
