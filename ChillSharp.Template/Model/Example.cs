using ChillSharp.Annotations;
using ChillSharp.EF;
using System.ComponentModel.DataAnnotations;

namespace ChillSharp.Template.Model;

[ChillEntity(
    UniquePropertyKeyString: "C65A0497-8D09-4A30-B641-B02453D735CC",
    PrimaryLanguageLabel: "Example",
    SecondaryLanguageLabel: "Esempio",
    LabelFormatString = "{Code} {Title}",
    ShortLabelFormatString = "{Code}",
    FullTextContentFormatString = "{Code} {Title}",
    EnableMCP = true,
    MCPDescription = "Minimal example entity included in the ChillSharp backend starter.")]
public partial class Example : ChillEntity
{
    [Key]
    public override Guid Guid { get; set; }

    [Required]
    [MaxLength(64)]
    [ChillProperty(
        UniquePropertyKeyString: "42AF176F-91BF-4B8F-A56F-E697A0C34EA9",
        PrimaryLanguageLabel: "Code",
        SecondaryLanguageLabel: "Codice",
        MCPDescription = "Short code used to identify the example entity.")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    [ChillProperty(
        UniquePropertyKeyString: "B13A4C7A-FE90-4D40-BF39-9E7FD25EEC26",
        PrimaryLanguageLabel: "Title",
        SecondaryLanguageLabel: "Titolo",
        MCPDescription = "Human-readable title of the example entity.")]
    public string Title { get; set; } = string.Empty;
}
