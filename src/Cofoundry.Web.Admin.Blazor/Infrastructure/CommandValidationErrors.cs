using System.ComponentModel.DataAnnotations;

namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// Turns a command's data annotation results into errors, as <see cref="IModelValidationService"/> does,
/// but keeps the messages of items in nested data models. The domain flattens one level of
/// <see cref="CompositeValidationResult"/> (the command's [ValidateObject] model), so an invalid item in
/// a model's nested collection only reported "{Collection} validation failed". Here the item's own
/// messages are kept and filed under the collection property, the field that shows the items.
/// </summary>
public static class CommandValidationErrors
{
    public static IReadOnlyList<ValidationError> Get(object command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var results = new List<ValidationResult>();
        Validator.TryValidateObject(command, new ValidationContext(command), results, true);

        return Flatten(results);
    }

    public static IReadOnlyList<ValidationError> Flatten(IEnumerable<ValidationResult> results)
    {
        var errors = new List<ValidationError>();

        foreach (var result in results)
        {
            if (result is not CompositeValidationResult composite)
            {
                errors.Add(Map(result.ErrorMessage, result.MemberNames));
                continue;
            }

            // The command's own model: its properties are fields on the form, so keep their names.
            foreach (var child in composite.Results)
            {
                if (child is CompositeValidationResult nested)
                {
                    var leaves = GetLeaves(nested).ToList();
                    errors.AddRange(leaves.Count == 0
                        ? [Map(nested.ErrorMessage, nested.MemberNames)]
                        : leaves.Select(l => Map(l.ErrorMessage, nested.MemberNames)));
                }
                else
                {
                    errors.Add(Map(child.ErrorMessage, child.MemberNames));
                }
            }
        }

        // Two items with the same problem would otherwise repeat the message.
        return errors
            .DistinctBy(e => (e.Message, string.Join(",", e.Properties)))
            .ToList();
    }

    private static IEnumerable<ValidationResult> GetLeaves(CompositeValidationResult composite)
    {
        foreach (var result in composite.Results)
        {
            if (result is CompositeValidationResult nested)
            {
                foreach (var leaf in GetLeaves(nested))
                {
                    yield return leaf;
                }
            }
            else
            {
                yield return result;
            }
        }
    }

    private static ValidationError Map(string? message, IEnumerable<string> memberNames)
    {
        return new ValidationError
        {
            Message = message ?? string.Empty,
            Properties = memberNames.ToArray()
        };
    }
}
