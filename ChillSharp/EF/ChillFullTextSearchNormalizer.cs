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
