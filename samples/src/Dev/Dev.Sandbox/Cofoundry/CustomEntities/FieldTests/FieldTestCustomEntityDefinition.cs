namespace Dev.Sandbox;

/// <summary>
/// A dev-only custom entity with one property per data model field type, used to exercise the admin's
/// dynamic form (see the run-blazor-admin skill's field-types-step.js).
/// </summary>
public class FieldTestCustomEntityDefinition : ICustomEntityDefinition<FieldTestDataModel>
{
    public const string DefinitionCode = "SIMFLD";

    public string CustomEntityDefinitionCode => DefinitionCode;

    public string Name => "Field Test";

    public string NamePlural => "Field Tests";

    public string Description => "One property per data model field type, for testing the admin.";

    public bool ForceUrlSlugUniqueness => false;

    public bool AutoGenerateUrlSlug => true;

    public bool AutoPublish => true;

    public bool HasLocale => false;
}
