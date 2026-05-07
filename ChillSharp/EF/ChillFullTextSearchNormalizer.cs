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
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ChillSharp.EF;

/// <summary>
/// Normalizes text used by ChillSharp generic full-text search.
/// </summary>
public static class ChillFullTextSearchNormalizer
{
    private static readonly char[] TokenSeparators = [' ', '\t', '\r', '\n', '*', '%'];
    private static readonly MethodInfo StringIsNullOrEmptyMethod = typeof(string).GetMethod(nameof(string.IsNullOrEmpty), [typeof(string)])!;
    private static readonly MethodInfo StringContainsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
    private static readonly MethodInfo StringStartsWithMethod = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;
    private static readonly MethodInfo StringEndsWithMethod = typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!;

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
    /// Returns <c>true</c> when the search text contains advanced selectors such as grouping brackets
    /// or standalone AND/OR operators outside quoted phrases.
    /// </summary>
    public static bool HasAdvancedSelectors(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var wordStart = -1;
        char? quotedBy = null;

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];

            if (quotedBy.HasValue)
            {
                if (character == quotedBy.Value)
                    quotedBy = null;

                continue;
            }

            if (character is '"' or '\'')
            {
                if (wordStart >= 0 && IsAdvancedOperatorToken(value, wordStart, index - wordStart))
                    return true;

                wordStart = -1;
                quotedBy = character;
                continue;
            }

            if (IsGroupingCharacter(character))
                return true;

            if (char.IsWhiteSpace(character))
            {
                if (wordStart >= 0 && IsAdvancedOperatorToken(value, wordStart, index - wordStart))
                    return true;

                wordStart = -1;
                continue;
            }

            if (wordStart < 0)
                wordStart = index;
        }

        return wordStart >= 0 && IsAdvancedOperatorToken(value, wordStart, value.Length - wordStart);
    }

    /// <summary>
    /// Applies the best available full-text strategy for the provided search text.
    /// Plain searches stay on the traditional fast path, while grouped AND/OR searches
    /// are parsed into a single boolean predicate.
    /// </summary>
    public static IQueryable<IChillEntity> ApplySearch(IQueryable<IChillEntity> query, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return query;

        if (!HasAdvancedSelectors(value))
        {
            foreach (var term in NormalizeSearchTerms(value))
                query = ApplySearchTerm(query, term);

            return query;
        }

        var tokens = TokenizeAdvancedSearch(value);
        if (tokens.Count == 0)
            return query;

        var entityParameter = Expression.Parameter(typeof(IChillEntity), "x");
        var parser = new AdvancedSearchParser(tokens, entityParameter);
        var predicateBody = parser.ParseExpression();
        if (predicateBody == null)
            predicateBody = Expression.Constant(false);

        var predicate = Expression.Lambda<Func<IChillEntity, bool>>(predicateBody, entityParameter);
        return query.Where(predicate);
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

    internal static Expression? BuildSearchTermExpression(ParameterExpression entityParameter, string rawValue)
    {
        Expression? result = null;
        foreach (var term in NormalizeSearchTerms(rawValue))
            result = CombineAnd(result, BuildSearchTermExpression(entityParameter, term));

        return result;
    }

    internal static Expression BuildSearchTermExpression(ParameterExpression entityParameter, ChillFullTextSearchTerm term)
    {
        var fullTextContent = Expression.Property(entityParameter, nameof(IChillEntity.FullTextContent));
        var notNullOrEmpty = Expression.Not(Expression.Call(StringIsNullOrEmptyMethod, fullTextContent));
        var value = term.Value;

        if (!term.MatchStartBoundary && !term.MatchEndBoundary)
            return Expression.AndAlso(notNullOrEmpty, Expression.Call(fullTextContent, StringContainsMethod, Expression.Constant(value)));

        if (term.MatchStartBoundary && term.MatchEndBoundary)
        {
            var startValue = value + " ";
            var middleValue = " " + value + " ";
            var endValue = " " + value;
            var matchesWholeWord = Expression.OrElse(
                Expression.Equal(fullTextContent, Expression.Constant(value)),
                Expression.OrElse(
                    Expression.Call(fullTextContent, StringStartsWithMethod, Expression.Constant(startValue)),
                    Expression.OrElse(
                        Expression.Call(fullTextContent, StringContainsMethod, Expression.Constant(middleValue)),
                        Expression.Call(fullTextContent, StringEndsWithMethod, Expression.Constant(endValue)))));

            return Expression.AndAlso(notNullOrEmpty, matchesWholeWord);
        }

        if (term.MatchStartBoundary)
        {
            var startValue = value;
            var middleValue = " " + value;
            var matchesStartBoundary = Expression.OrElse(
                Expression.Call(fullTextContent, StringStartsWithMethod, Expression.Constant(startValue)),
                Expression.Call(fullTextContent, StringContainsMethod, Expression.Constant(middleValue)));

            return Expression.AndAlso(notNullOrEmpty, matchesStartBoundary);
        }

        var beforeEndValue = value + " ";
        var matchesEndBoundary = Expression.OrElse(
            Expression.Call(fullTextContent, StringEndsWithMethod, Expression.Constant(value)),
            Expression.Call(fullTextContent, StringContainsMethod, Expression.Constant(beforeEndValue)));

        return Expression.AndAlso(notNullOrEmpty, matchesEndBoundary);
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

    private static List<ChillFullTextSearchToken> TokenizeAdvancedSearch(string value)
    {
        var tokens = new List<ChillFullTextSearchToken>();

        for (var index = 0; index < value.Length;)
        {
            if (char.IsWhiteSpace(value[index]))
            {
                index++;
                continue;
            }

            if (value[index] is '(' or '[')
            {
                tokens.Add(new ChillFullTextSearchToken(ChillFullTextSearchTokenType.OpenGroup, value[index].ToString()));
                index++;
                continue;
            }

            if (value[index] is ')' or ']')
            {
                tokens.Add(new ChillFullTextSearchToken(ChillFullTextSearchTokenType.CloseGroup, value[index].ToString()));
                index++;
                continue;
            }

            if (value[index] is '"' or '\'')
            {
                var quote = value[index];
                var endIndex = index + 1;
                while (endIndex < value.Length && value[endIndex] != quote)
                    endIndex++;

                if (endIndex < value.Length)
                {
                    tokens.Add(new ChillFullTextSearchToken(ChillFullTextSearchTokenType.Term, value[index..(endIndex + 1)]));
                    index = endIndex + 1;
                }
                else
                {
                    tokens.Add(new ChillFullTextSearchToken(ChillFullTextSearchTokenType.Term, value[index..]));
                    index = value.Length;
                }

                continue;
            }

            var start = index;
            while (index < value.Length && !char.IsWhiteSpace(value[index]) && !IsGroupingCharacter(value[index]))
                index++;

            var tokenValue = value[start..index];
            if (string.Equals(tokenValue, "and", StringComparison.OrdinalIgnoreCase))
                tokens.Add(new ChillFullTextSearchToken(ChillFullTextSearchTokenType.And, tokenValue));
            else if (string.Equals(tokenValue, "or", StringComparison.OrdinalIgnoreCase))
                tokens.Add(new ChillFullTextSearchToken(ChillFullTextSearchTokenType.Or, tokenValue));
            else
                tokens.Add(new ChillFullTextSearchToken(ChillFullTextSearchTokenType.Term, tokenValue));
        }

        return tokens;
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

    private static bool IsGroupingCharacter(char value)
    {
        return value is '(' or ')' or '[' or ']';
    }

    private static bool IsAdvancedOperatorToken(string value, int start, int length)
    {
        if (length == 3)
            return string.Compare(value, start, "and", 0, 3, StringComparison.OrdinalIgnoreCase) == 0;

        if (length == 2)
            return string.Compare(value, start, "or", 0, 2, StringComparison.OrdinalIgnoreCase) == 0;

        return false;
    }

    internal static Expression? CombineAnd(Expression? left, Expression? right)
    {
        if (left == null)
            return right;

        if (right == null)
            return left;

        return Expression.AndAlso(left, right);
    }

    internal static Expression? CombineOr(Expression? left, Expression? right)
    {
        if (left == null)
            return right;

        if (right == null)
            return left;

        return Expression.OrElse(left, right);
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

internal sealed class AdvancedSearchParser(
    IReadOnlyList<ChillFullTextSearchToken> tokens,
    ParameterExpression entityParameter)
{
    private readonly IReadOnlyList<ChillFullTextSearchToken> _tokens = tokens;
    private readonly ParameterExpression _entityParameter = entityParameter;
    private int _index;

    public Expression? ParseExpression()
    {
        return ParseOrExpression();
    }

    private Expression? ParseOrExpression()
    {
        var expression = ParseAndExpression();

        while (TryMatch(ChillFullTextSearchTokenType.Or))
            expression = ChillFullTextSearchNormalizer.CombineOr(expression, ParseAndExpression());

        return expression;
    }

    private Expression? ParseAndExpression()
    {
        var expression = ParsePrimaryExpression();

        while (true)
        {
            if (TryMatch(ChillFullTextSearchTokenType.And))
            {
                expression = ChillFullTextSearchNormalizer.CombineAnd(expression, ParsePrimaryExpression());
                continue;
            }

            if (CanStartPrimary(CurrentType))
            {
                expression = ChillFullTextSearchNormalizer.CombineAnd(expression, ParsePrimaryExpression());
                continue;
            }

            return expression;
        }
    }

    private Expression? ParsePrimaryExpression()
    {
        while (_index < _tokens.Count)
        {
            var token = _tokens[_index];
            switch (token.Type)
            {
                case ChillFullTextSearchTokenType.Term:
                    _index++;
                    return ChillFullTextSearchNormalizer.BuildSearchTermExpression(_entityParameter, token.Value);

                case ChillFullTextSearchTokenType.OpenGroup:
                    _index++;
                    var innerExpression = ParseOrExpression();
                    TryMatch(ChillFullTextSearchTokenType.CloseGroup);
                    return innerExpression;

                case ChillFullTextSearchTokenType.And:
                case ChillFullTextSearchTokenType.Or:
                    _index++;
                    continue;

                case ChillFullTextSearchTokenType.CloseGroup:
                    return null;
            }
        }

        return null;
    }

    private ChillFullTextSearchTokenType? CurrentType
        => _index < _tokens.Count ? _tokens[_index].Type : null;

    private bool TryMatch(ChillFullTextSearchTokenType type)
    {
        if (CurrentType != type)
            return false;

        _index++;
        return true;
    }

    private static bool CanStartPrimary(ChillFullTextSearchTokenType? type)
    {
        return type is ChillFullTextSearchTokenType.Term or ChillFullTextSearchTokenType.OpenGroup;
    }
}

internal readonly record struct ChillFullTextSearchToken(
    ChillFullTextSearchTokenType Type,
    string Value);

internal enum ChillFullTextSearchTokenType
{
    Term,
    And,
    Or,
    OpenGroup,
    CloseGroup
}
