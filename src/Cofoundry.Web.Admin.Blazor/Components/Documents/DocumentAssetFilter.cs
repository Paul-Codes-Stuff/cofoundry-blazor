namespace Cofoundry.Web.Admin.Blazor.Components.Documents;

/// <summary>
/// Restrictions a [Document] or [DocumentCollection] property puts on the documents it accepts. The
/// document picker opens with them applied and doesn't let them be changed, as the AngularJS picker did.
/// </summary>
public sealed record DocumentAssetFilter
{
    public static readonly DocumentAssetFilter None = new();

    /// <summary>
    /// Tags in the format TagParser reads: comma separated, quoted if they contain a space.
    /// </summary>
    public string? Tags { get; init; }

    /// <summary>
    /// The one file extension allowed, when the attribute lists a single extension.
    /// </summary>
    public string? FileExtension { get; init; }

    /// <summary>
    /// The file extensions allowed, comma separated, when the attribute lists more than one.
    /// </summary>
    public string? FileExtensions { get; init; }

    public bool HasFileTypeRestrictions => FileExtension != null || FileExtensions != null;
}
