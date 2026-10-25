using System.Reflection;

namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// Required only when another boolean property on the same object is true. Used where the AngularJS
/// admin set <c>ng-required</c> from settings, e.g. a username is only needed when the user area
/// doesn't use the email address as the username.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RequiredIfAttribute : RequiredAttribute
{
    public RequiredIfAttribute(string conditionPropertyName)
    {
        ConditionPropertyName = conditionPropertyName;
    }

    public string ConditionPropertyName { get; }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var condition = validationContext.ObjectType.GetProperty(ConditionPropertyName, BindingFlags.Public | BindingFlags.Instance);

        if (condition?.GetValue(validationContext.ObjectInstance) is not true)
        {
            return ValidationResult.Success;
        }

        return base.IsValid(value, validationContext);
    }
}
