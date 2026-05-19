using ChillSharp;
using ChillSharp.Annotations;
using ChillSharp.EF;
using ChillSharp.Template.Model;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Template.Query;

[ChillEntity(
    UniquePropertyKeyString: "A62BD0BF-E4AF-466A-B8C4-7D0743B9EC1F",
    PrimaryLanguageLabel: "Example query",
    SecondaryLanguageLabel: "Ricerca esempio",
    EnableMCP = true,
    MCPDescription = "Query the example entity by identifier, code, or title.")]
public class ExampleQuery : ChillQuery
{
    [ChillProperty(
        UniquePropertyKeyString: "3B8CE55B-C676-46B2-8248-9B73772FE186",
        PrimaryLanguageLabel: "Code",
        SecondaryLanguageLabel: "Codice",
        MCPDescription = "Contains-match filter for the example code.")]
    public string Code { get; set; } = string.Empty;

    [ChillProperty(
        UniquePropertyKeyString: "28352A25-1F12-456C-A919-0D99CC1267E5",
        PrimaryLanguageLabel: "Title",
        SecondaryLanguageLabel: "Titolo",
        MCPDescription = "Contains-match filter for the example title.")]
    public string Title { get; set; } = string.Empty;

    public override IQueryable<IChillEntity> OnQuery(IChillContext Context, bool LightweightRequired = false)
    {
        var db = (ChillSharpTemplateContext)Context;
        var query = db.Examples.AsQueryable();

        if (Guid.HasValue)
        {
            query = query.Where(x => x.Guid == Guid.Value);
        }

        if (!string.IsNullOrWhiteSpace(Code))
        {
            var code = Code.Trim();
            query = query.Where(x => x.Code.Contains(code));
        }

        if (!string.IsNullOrWhiteSpace(Title))
        {
            var title = Title.Trim();
            query = query.Where(x => x.Title.Contains(title));
        }

        return query;
    }
}
