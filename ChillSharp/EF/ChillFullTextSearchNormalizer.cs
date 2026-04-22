/*
 * ChillSharp is a lightweight .NET library that sits on top of Entity Framework Core
 * and turns an existing data model into a fully working REST API with almost no setup.
 * Copyright (C) 2025 Andrea Piovesan
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Globalization;
using System.Text;

namespace ChillSharp.EF;

/// <summary>
/// Normalizes text used by ChillSharp generic full-text search.
/// </summary>
public static class ChillFullTextSearchNormalizer
{
    private static readonly char[] TokenSeparators = [' ', '\t', '\r', '\n', '*', '%'];

    /// <summary>
    /// Converts text to the normalized form stored and queried by generic full-text search.
    /// </summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category is UnicodeCategory.NonSpacingMark
                or UnicodeCategory.SpacingCombiningMark
                or UnicodeCategory.EnclosingMark)
                continue;

            AppendFoldedCharacter(builder, character);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    /// <summary>
    /// Returns normalized full-text search terms. Text enclosed by matching single or double quotes is searched as one phrase.
    /// </summary>
    public static ChillFullTextSearchTerm[] NormalizeSearchTerms(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        var trimmed = value.Trim();
        if (IsQuotedPhrase(trimmed))
            return NormalizeQuotedPhrase(trimmed[1..^1]);

        return NormalizeTokenTerms(trimmed);
    }

    /// <summary>
    /// Applies a normalized full-text search term to a query.
    /// </summary>
    public static IQueryable<IChillEntity> ApplySearchTerm(IQueryable<IChillEntity> query, ChillFullTextSearchTerm term)
    {
        var value = term.Value;
        if (!term.MatchStartBoundary && !term.MatchEndBoundary)
            return query.Where(x => !string.IsNullOrEmpty(x.FullTextContent) && x.FullTextContent.Contains(value));

        if (term.MatchStartBoundary && term.MatchEndBoundary)
        {
            var startValue = value + " ";
            var middleValue = " " + value + " ";
            var endValue = " " + value;
            return query.Where(x => !string.IsNullOrEmpty(x.FullTextContent)
                && (x.FullTextContent == value
                    || x.FullTextContent.StartsWith(startValue)
                    || x.FullTextContent.Contains(middleValue)
                    || x.FullTextContent.EndsWith(endValue)));
        }

        if (term.MatchStartBoundary)
        {
            var startValue = value;
            var middleValue = " " + value;
            return query.Where(x => !string.IsNullOrEmpty(x.FullTextContent)
                && (x.FullTextContent.StartsWith(startValue)
                    || x.FullTextContent.Contains(middleValue)));
        }

        var endTermValue = value;
        var beforeEndValue = value + " ";
        return query.Where(x => !string.IsNullOrEmpty(x.FullTextContent)
            && (x.FullTextContent.EndsWith(endTermValue)
                || x.FullTextContent.Contains(beforeEndValue)));
    }

    private static ChillFullTextSearchTerm[] NormalizeQuotedPhrase(string value)
    {
        var phrase = value.Trim();
        if (string.IsNullOrWhiteSpace(phrase))
            return [];

        if (ContainsMiddleWildcard(phrase))
            return NormalizeTokenTerms(phrase);

        var matchStartBoundary = !IsWildcard(phrase[0]);
        var matchEndBoundary = !IsWildcard(phrase[^1]);
        var startIndex = matchStartBoundary ? 0 : 1;
        var endIndex = matchEndBoundary ? phrase.Length : phrase.Length - 1;

        if (endIndex < startIndex)
            return [];

        var normalized = Normalize(phrase[startIndex..endIndex].Trim());
        return string.IsNullOrWhiteSpace(normalized)
            ? []
            : [new ChillFullTextSearchTerm(normalized, matchStartBoundary, matchEndBoundary)];
    }

    private static ChillFullTextSearchTerm[] NormalizeTokenTerms(string value)
    {
        return value
            .Split(TokenSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(Normalize)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(x => new ChillFullTextSearchTerm(x, false, false))
            .ToArray();
    }

    private static bool IsQuotedPhrase(string value)
    {
        if (value.Length < 2)
            return false;

        return (value[0] == '"' && value[^1] == '"')
            || (value[0] == '\'' && value[^1] == '\'');
    }

    private static bool ContainsMiddleWildcard(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            if (IsWildcard(value[index]) && index != 0 && index != value.Length - 1)
                return true;
        }

        return false;
    }

    private static bool IsWildcard(char value)
    {
        return value is '*' or '%';
    }

    private static void AppendFoldedCharacter(StringBuilder builder, char character)
    {
        switch (character)
        {
            case 'Æ':
            case 'Ǽ':
            case 'æ':
            case 'ǽ':
                builder.Append("ae");
                break;
            case 'Œ':
            case 'œ':
                builder.Append("oe");
                break;
            case 'ß':
                builder.Append("ss");
                break;
            case 'Ð':
            case 'Đ':
            case 'ð':
            case 'đ':
                builder.Append('d');
                break;
            case 'Þ':
            case 'þ':
                builder.Append("th");
                break;
            case 'Ł':
            case 'ł':
                builder.Append('l');
                break;
            case 'Ø':
            case 'ø':
                builder.Append('o');
                break;
            default:
                builder.Append(character);
                break;
        }
    }
}

public readonly record struct ChillFullTextSearchTerm(
    string Value,
    bool MatchStartBoundary,
    bool MatchEndBoundary);
