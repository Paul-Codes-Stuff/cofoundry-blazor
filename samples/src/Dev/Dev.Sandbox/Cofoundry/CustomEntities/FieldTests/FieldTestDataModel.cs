using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Dev.Sandbox;

/// <summary>
/// One property per data model field type, including the different property types an attribute allows
/// (e.g. a date as a DateTime, DateTimeOffset or string).
/// </summary>
public class FieldTestDataModel : ICustomEntityDataModel
{
    [Required]
    [Date]
    [Display(Description = "Required, UTC.")]
    public DateTime? Date { get; set; }

    [DateLocal]
    [Display(Description = "Midnight in the browser's time zone, stored in UTC.")]
    public DateTime? DateLocal { get; set; }

    [Date(Min = "2020-01-01", Max = "2030-12-31")]
    [Display(Description = "A string property, between 2020 and 2030.")]
    public string? DateAsString { get; set; }

    [DateAndTime]
    [Display(Description = "A DateTimeOffset property, UTC.")]
    public DateTimeOffset? DateAndTime { get; set; }

    [DateAndTimeLocal(Step = 1)]
    [Display(Description = "The browser's time zone, stored in UTC, with seconds.")]
    public DateTime? DateAndTimeLocal { get; set; }

    [Display(Description = "No attribute: the DateTime template.")]
    public DateTime? PlainDateTime { get; set; }

    [Time]
    public TimeSpan? Time { get; set; }

    [Time(Step = 1)]
    [Display(Description = "A string property, with seconds.")]
    public string? TimeAsString { get; set; }

    [Color]
    public string? Color { get; set; }

    [SelectList(typeof(FieldTestColour), DefaultItemText = "No colour")]
    [Display(Description = "An enum, nullable.")]
    public FieldTestColour? EnumDropdown { get; set; }

    [SelectList(typeof(FieldTestNumberOptionSource))]
    [Display(Description = "An IListOptionSource of ints.")]
    public int? NumberDropdown { get; set; }

    [SelectList(typeof(PetsApiOptionSource))]
    [Display(Description = "An IListOptionApiSource, loaded by the browser.")]
    public int? ApiDropdown { get; set; }

    [Required]
    [RadioList(typeof(FieldTestColour))]
    [Display(Description = "Required, so no 'None' choice.")]
    public FieldTestColour? EnumRadio { get; set; }

    [RadioList(typeof(PetsApiOptionSource), DefaultItemText = "No pet")]
    public int? ApiRadio { get; set; }

    [CheckboxList(typeof(FieldTestColour), NoValueText = "No colours")]
    public IReadOnlyCollection<FieldTestColour> EnumCheckboxes { get; set; } = Array.Empty<FieldTestColour>();

    [CheckboxList(typeof(PetsApiOptionSource))]
    public ICollection<int> ApiCheckboxes { get; set; } = new List<int>();

    [Image]
    public int? Image { get; set; }

    [Image(MinWidth = 50, MinHeight = 50)]
    [Display(Description = "The picker only offers images at least 50x50.")]
    public int? MinSizeImage { get; set; }

    [ImageCollection]
    public ICollection<int> Images { get; set; } = new List<int>();

    [Document(FileExtensions = ["pdf"])]
    [Display(Description = "The picker only offers PDFs.")]
    public int? PdfDocument { get; set; }

    [DocumentCollection]
    public ICollection<int> Documents { get; set; } = new List<int>();

    [Page]
    public int? Page { get; set; }

    [PageCollection]
    [Display(Description = "Not orderable, so listed newest first.")]
    public ICollection<int> Pages { get; set; } = new List<int>();

    [PageDirectory]
    public int? Directory { get; set; }

    [Html(HtmlToolbarPreset.Headings, HtmlToolbarPreset.BasicFormatting, HtmlToolbarPreset.Media, HtmlToolbarPreset.Source, Rows = 8)]
    public string? Html { get; set; }

    [UIHint("NotARealFieldType")]
    [Display(Description = "A template with no field component, to show the unsupported placeholder.")]
    public string? Unsupported { get; set; }
}

public enum FieldTestColour
{
    Red,
    [Description("Leafy green")]
    Green,
    Blue
}

public class FieldTestNumberOptionSource : IListOptionSource
{
    public IReadOnlyCollection<ListOption> Create()
    {
        return
        [
            new ListOption("One", 1),
            new ListOption("Two", 2),
            new ListOption("Three", 3)
        ];
    }
}
