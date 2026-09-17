using ChillSharp;
using System.Text;

namespace ChillSharp.Template.Model;

public partial class Example
{
    public override void OnCreate(IChillContext context)
    {
        base.OnCreate(context);
        ApplyNormalizedValues();
    }

    public override void OnUpdate(IChillContext context)
    {
        ApplyNormalizedValues();
    }

    public override void OnAutocomplete(IChillContext context)
    {
        ApplyNormalizedValues();
    }

    public override string GetLabel(IChillContext context)
    {
        if (string.IsNullOrWhiteSpace(Code))
        {
            return Title;
        }

        if (string.IsNullOrWhiteSpace(Title))
        {
            return Code;
        }

        return $"{Code} - {Title}";
    }

    public override string GetFullTextContent(IChillContext context)
    {
        return $"{Code} {Title}".Trim();
    }

    private void ApplyNormalizedValues()
    {
        Title = (Title ?? string.Empty).Trim();
        Code = NormalizeCode(Code);

        if (string.IsNullOrWhiteSpace(Code) && !string.IsNullOrWhiteSpace(Title))
        {
            Code = BuildCodeFromTitle(Title);
        }
    }

    private static string NormalizeCode(string? value)
    {
        return (value ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static string BuildCodeFromTitle(string title)
    {
        var builder = new StringBuilder();
        var previousWasSeparator = false;

        foreach (var ch in title.Trim().ToUpperInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                previousWasSeparator = false;
                continue;
            }

            if (char.IsWhiteSpace(ch) || ch is '-' or '_')
            {
                if (!previousWasSeparator && builder.Length > 0)
                {
                    builder.Append('-');
                    previousWasSeparator = true;
                }
            }
        }

        return builder.ToString().Trim('-');
    }
}
