using ChillSharp.Annotations;
using ChillSharp.Dto;
using ChillSharp.EF;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ChillSharp;

internal static class ChillEntityLabelFormatter
{
    private static readonly Regex PlaceholderRegex = new(@"\{(?<field>[A-Za-z_][A-Za-z0-9_]*)\}", RegexOptions.Compiled);
    private enum FormatTarget
    {
        Label,
        ShortLabel,
        FullTextContent
    }

    public static string ResolveLabel(IChillEntity entity, IChillContext context)
    {
        return TryResolveConfiguredLabel(entity, context, FormatTarget.Label) ?? entity.GetLabel(context);
    }

    public static string ResolveShortLabel(IChillEntity entity, IChillContext context)
    {
        return TryResolveConfiguredLabel(entity, context, FormatTarget.ShortLabel) ?? entity.GetShortLabel(context);
    }

    public static string ResolveFullTextContent(IChillEntity entity, IChillContext context)
    {
        var configuredValue = TryResolveConfiguredFullTextContent(entity, context);
        if (configuredValue != null)
            return configuredValue;

        return HasCustomFullTextContentOverride(entity)
            ? entity.GetFullTextContent(context)
            : entity.GetLabel(context);
    }

    public static string? TryResolveConfiguredLabel(IChillEntity entity, IChillContext context, bool useShortLabel)
    {
        return TryResolveConfiguredLabel(entity, context, useShortLabel ? FormatTarget.ShortLabel : FormatTarget.Label);
    }

    public static string? TryResolveConfiguredFullTextContent(IChillEntity entity, IChillContext context)
    {
        return TryResolveConfiguredLabel(entity, context, FormatTarget.FullTextContent);
    }

    private static string? TryResolveConfiguredLabel(IChillEntity entity, IChillContext context, FormatTarget target)
    {
        var chillType = ChillTypeResolver.NormalizeChillType(entity.GetType(), context.GetChillTypePrefix());
        var entityOptions = context.GetEntityOptions(chillType);
        var format = target switch
        {
            FormatTarget.Label => entityOptions.LabelFormatString,
            FormatTarget.ShortLabel => entityOptions.ShortLabelFormatString,
            FormatTarget.FullTextContent => entityOptions.FullTextContentFormatString,
            _ => null
        };
        if (string.IsNullOrWhiteSpace(format))
            return null;

        return PlaceholderRegex.Replace(format, match =>
        {
            var propertyName = match.Groups["field"].Value;
            var property = entity.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.IsDefined(typeof(ChillPropertyAttribute), inherit: true))
                return string.Empty;

            return FormatValue(property.GetValue(entity));
        });
    }

    private static bool HasCustomFullTextContentOverride(IChillEntity entity)
    {
        var method = entity.GetType().GetMethod(nameof(IChillEntity.GetFullTextContent), BindingFlags.Instance | BindingFlags.Public);
        return method?.DeclaringType != typeof(ChillEntity);
    }

    private static string FormatValue(object? value)
    {
        if (value == null)
            return string.Empty;

        if (value is string text)
            return text;

        if (value is IChillEntity chillEntity)
            return string.IsNullOrWhiteSpace(chillEntity.Label)
                ? chillEntity.Guid.ToString("D", CultureInfo.InvariantCulture)
                : chillEntity.Label;

        if (value is IEnumerable values and not string)
        {
            return string.Join(", ", values.Cast<object?>().Select(FormatValue).Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        if (value is IFormattable formattable)
            return formattable.ToString(null, CultureInfo.InvariantCulture);

        return value.ToString() ?? string.Empty;
    }
}
