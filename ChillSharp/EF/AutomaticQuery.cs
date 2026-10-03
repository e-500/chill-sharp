/*
 * ChillSharp is a lightweight .NET library that sits on top of Entity Framework Core
 * and turns an existing data model into a fully working REST API with almost no setup.
 * Copyright (C) 2025 Andrea Piovesan
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */

using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.EF;

/// <summary>
/// A provider-neutral, structured filter definition that can be applied to an
/// <see cref="IQueryable{T}"/> without writing an expression tree by hand.
/// </summary>
public sealed class AutomaticQuery
{
    public const int MaximumFilterCount = 100;
    public const int MaximumGroupDepth = 8;
    public const int MaximumPropertyPathLength = 512;

    /// <summary>The root filter group. Empty groups do not change the query.</summary>
    public AutomaticQueryGroup Filter { get; set; } = new();

    /// <summary>Applies <see cref="Filter"/> to <paramref name="query"/>.</summary>
    public IQueryable<T> ApplyTo<T>(IQueryable<T> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        Validate();

        var parameter = Expression.Parameter(typeof(T), "entity");
        var body = AutomaticQueryExpressionBuilder.BuildGroup(Filter, parameter);
        if (body == null)
            return query;

        return query.Where(Expression.Lambda<Func<T, bool>>(body, parameter));
    }

    private void Validate()
    {
        if (Filter == null)
            throw new ChillException("An automatic query requires a root filter group.");

        var filterCount = 0;
        ValidateGroup(
            Filter,
            depth: 0,
            ref filterCount,
            new HashSet<AutomaticQueryGroup>(ReferenceEqualityComparer.Instance));
    }

    private static void ValidateGroup(
        AutomaticQueryGroup group,
        int depth,
        ref int filterCount,
        HashSet<AutomaticQueryGroup> activeGroups)
    {
        if (depth > MaximumGroupDepth)
            throw new ChillException($"Automatic query groups cannot exceed a depth of {MaximumGroupDepth}.");
        if (!activeGroups.Add(group))
            throw new ChillException("Automatic query groups cannot contain cycles.");
        if (!Enum.IsDefined(group.LogicalOperator))
            throw new ChillException($"Unsupported automatic query logical operator '{group.LogicalOperator}'.");
        if (group.Filters == null || group.Groups == null)
            throw new ChillException("Automatic query filter and group collections cannot be null.");

        foreach (var filter in group.Filters)
        {
            if (filter == null)
                throw new ChillException("Automatic query filter collections cannot contain null values.");
            filterCount++;
            if (filterCount > MaximumFilterCount)
                throw new ChillException($"An automatic query cannot contain more than {MaximumFilterCount} filters.");
            if (filter.PropertyName?.Length > MaximumPropertyPathLength)
                throw new ChillException($"Automatic query property paths cannot exceed {MaximumPropertyPathLength} characters.");
            if (filter.ItemFilter != null)
                ValidateGroup(filter.ItemFilter, depth + 1, ref filterCount, activeGroups);
        }

        foreach (var nestedGroup in group.Groups)
        {
            if (nestedGroup == null)
                throw new ChillException("Automatic query group collections cannot contain null values.");
            ValidateGroup(nestedGroup, depth + 1, ref filterCount, activeGroups);
        }

        activeGroups.Remove(group);
    }
}

/// <summary>Combines filters and nested groups using one logical operator.</summary>
public sealed class AutomaticQueryGroup
{
    public AutomaticQueryLogicalOperator LogicalOperator { get; set; } = AutomaticQueryLogicalOperator.And;
    public IList<AutomaticQueryFilter> Filters { get; set; } = new List<AutomaticQueryFilter>();
    public IList<AutomaticQueryGroup> Groups { get; set; } = new List<AutomaticQueryGroup>();
}

/// <summary>Describes one comparison against a CLR property path.</summary>
public sealed class AutomaticQueryFilter
{
    public string PropertyName { get; set; } = string.Empty;
    public AutomaticQueryOperator Operator { get; set; } = AutomaticQueryOperator.Equal;
    public object? Value { get; set; }
    public object? SecondValue { get; set; }

    /// <summary>
    /// Defines the predicate for <see cref="AutomaticQueryOperator.Any"/> and
    /// <see cref="AutomaticQueryOperator.All"/> collection filters.
    /// Property paths in this group are relative to a collection item.
    /// </summary>
    public AutomaticQueryGroup? ItemFilter { get; set; }

    /// <summary>Normalize both operands before applying string operators.</summary>
    public bool IgnoreCase { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AutomaticQueryLogicalOperator
{
    And,
    Or
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AutomaticQueryOperator
{
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Between,
    Contains,
    StartsWith,
    EndsWith,
    In,
    IsNull,
    IsNotNull,
    IsEmpty,
    IsNotEmpty,
    Any,
    All
}

/// <summary>
/// An opt-in Chill query implementation backed by an <see cref="AutomaticQuery"/>.
/// Existing <see cref="ChillQuery"/> subclasses and <c>ChillEngine.Query</c> remain unchanged.
/// </summary>
/// <typeparam name="TEntity">The entity type to query.</typeparam>
public class AutomaticQuery<TEntity> : ChillQuery, IChillQuery<TEntity>, IAutomaticChillQuery
    where TEntity : class, IChillEntity
{
    public AutomaticQuery Definition { get; set; } = new();

    public override IQueryable<IChillEntity> OnQuery(IChillContext Context, bool LightweightRequired = false)
    {
        var query = ((DbContext)Context).Set<TEntity>().AsQueryable();
        if (LightweightRequired)
            query = query.AsNoTracking();
        if (Guid.HasValue)
            query = query.Where(entity => entity.Guid == Guid.Value);

        return Definition.ApplyTo(query).Cast<IChillEntity>();
    }

    public override IQueryable<IChillEntity> OnOrderingBy(IChillContext Context, IQueryable<IChillEntity> Query)
    {
        return ChillOrderingApplier.ApplyOrdering(Query, Ordering, typeof(TEntity));
    }

    IQueryable<TEntity> IChillQuery<TEntity>.OnQuery(IChillContext Context, bool LightweightRequired)
    {
        return OnQuery(Context, LightweightRequired).Cast<TEntity>();
    }

    IQueryable<TEntity> IChillQuery<TEntity>.OnOrderingBy(IChillContext Context, IQueryable<TEntity> Query)
    {
        return ChillOrderingApplier.ApplyOrdering(Query, Ordering, typeof(TEntity));
    }

    IQueryable<TEntity> IChillQuery<TEntity>.OnPaginate(IChillContext Context, IQueryable<TEntity> Query)
    {
        if (Pagination == null)
            return Query;

        return Query.Skip((Pagination.Page - 1) * Pagination.PageResults).Take(Pagination.PageResults);
    }
}

internal interface IAutomaticChillQuery
{
    AutomaticQuery Definition { get; set; }
}

internal static class AutomaticQueryExpressionBuilder
{
    private static readonly MethodInfo EnumerableContains = GetEnumerableMethod(nameof(Enumerable.Contains), 2);
    private static readonly MethodInfo EnumerableAny = GetEnumerablePredicateMethod(nameof(Enumerable.Any));
    private static readonly MethodInfo EnumerableAll = GetEnumerablePredicateMethod(nameof(Enumerable.All));
    private static readonly MethodInfo EnumerableAnyWithoutPredicate = GetEnumerableMethod(nameof(Enumerable.Any), 1);

    public static Expression? BuildGroup(AutomaticQueryGroup group, ParameterExpression parameter)
    {
        ArgumentNullException.ThrowIfNull(group);

        var expressions = new List<Expression>();
        expressions.AddRange(group.Filters.Select(filter => BuildFilter(filter, parameter)));
        expressions.AddRange(group.Groups.Select(nested => BuildGroup(nested, parameter)).OfType<Expression>());

        return expressions.Count switch
        {
            0 => null,
            1 => expressions[0],
            _ => expressions.Aggregate(group.LogicalOperator == AutomaticQueryLogicalOperator.And
                ? Expression.AndAlso
                : Expression.OrElse)
        };
    }

    private static Expression BuildFilter(AutomaticQueryFilter filter, ParameterExpression parameter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        if (string.IsNullOrWhiteSpace(filter.PropertyName))
            throw new ChillException("An automatic query filter requires a property name.");

        var (member, nullGuard) = BuildPropertyPath(parameter, filter.PropertyName);
        Expression comparison = filter.Operator switch
        {
            AutomaticQueryOperator.Equal => BuildEquality(member, filter.Value, false, filter.IgnoreCase),
            AutomaticQueryOperator.NotEqual => BuildEquality(member, filter.Value, true, filter.IgnoreCase),
            AutomaticQueryOperator.GreaterThan => BuildComparison(member, filter.Value, Expression.GreaterThan),
            AutomaticQueryOperator.GreaterThanOrEqual => BuildComparison(member, filter.Value, Expression.GreaterThanOrEqual),
            AutomaticQueryOperator.LessThan => BuildComparison(member, filter.Value, Expression.LessThan),
            AutomaticQueryOperator.LessThanOrEqual => BuildComparison(member, filter.Value, Expression.LessThanOrEqual),
            AutomaticQueryOperator.Between => Expression.AndAlso(
                BuildComparison(member, filter.Value, Expression.GreaterThanOrEqual),
                BuildComparison(member, filter.SecondValue, Expression.LessThanOrEqual)),
            AutomaticQueryOperator.Contains => BuildContains(member, filter.Value, filter.IgnoreCase),
            AutomaticQueryOperator.StartsWith => BuildStringOperation(member, filter.Value, nameof(string.StartsWith), filter.IgnoreCase),
            AutomaticQueryOperator.EndsWith => BuildStringOperation(member, filter.Value, nameof(string.EndsWith), filter.IgnoreCase),
            AutomaticQueryOperator.In => BuildIn(member, filter.Value, filter.IgnoreCase),
            AutomaticQueryOperator.IsNull => BuildNullComparison(member, false),
            AutomaticQueryOperator.IsNotNull => BuildNullComparison(member, true),
            AutomaticQueryOperator.IsEmpty => BuildEmpty(member, false),
            AutomaticQueryOperator.IsNotEmpty => BuildEmpty(member, true),
            AutomaticQueryOperator.Any => BuildQuantifier(member, filter.ItemFilter, all: false),
            AutomaticQueryOperator.All => BuildQuantifier(member, filter.ItemFilter, all: true),
            _ => throw new ChillException($"Unsupported automatic query operator '{filter.Operator}'.")
        };

        if (nullGuard == null)
            return comparison;

        if (filter.Operator == AutomaticQueryOperator.IsNull)
            return Expression.OrElse(Expression.Not(nullGuard), comparison);

        return Expression.AndAlso(nullGuard, comparison);
    }

    private static Expression BuildEquality(Expression member, object? value, bool negate, bool ignoreCase)
    {
        if (value != null && TryGetEntityGuidMember(member, out var guidMember))
        {
            var entityIsNull = Expression.Equal(member, Expression.Constant(null, member.Type));
            var guidEquality = Expression.Equal(guidMember, CreateConstant(value, guidMember.Type));
            return negate
                ? Expression.OrElse(entityIsNull, Expression.Not(guidEquality))
                : Expression.AndAlso(Expression.Not(entityIsNull), guidEquality);
        }

        if (ignoreCase && member.Type == typeof(string) && value != null)
        {
            var normalizedMember = Expression.Call(member, nameof(string.ToLower), Type.EmptyTypes);
            var normalizedValue = Convert.ToString(value, CultureInfo.InvariantCulture)?.ToLowerInvariant();
            var equality = Expression.Equal(normalizedMember, Expression.Constant(normalizedValue, typeof(string)));
            var memberIsNull = Expression.Equal(member, Expression.Constant(null, member.Type));
            return negate
                ? Expression.OrElse(memberIsNull, Expression.Not(equality))
                : Expression.AndAlso(Expression.Not(memberIsNull), equality);
        }

        var constant = CreateConstant(value, member.Type);
        return negate ? Expression.NotEqual(member, constant) : Expression.Equal(member, constant);
    }

    private static Expression BuildComparison(
        Expression member,
        object? value,
        Func<Expression, Expression, BinaryExpression> comparison)
    {
        if (value == null)
            throw new ChillException($"Operator requires a value for member type '{member.Type.Name}'.");
        return comparison(member, CreateConstant(value, member.Type));
    }

    private static Expression BuildContains(Expression member, object? value, bool ignoreCase)
    {
        if (member.Type == typeof(string))
            return BuildStringOperation(member, value, nameof(string.Contains), ignoreCase);

        var elementType = GetEnumerableElementType(member.Type)
            ?? throw new ChillException($"Contains is not supported for '{member.Type.Name}'.");
        if (typeof(IChillEntity).IsAssignableFrom(elementType))
        {
            var item = Expression.Parameter(elementType, "item");
            var itemGuid = Expression.Property(item, nameof(IChillEntity.Guid));
            var predicate = Expression.Equal(itemGuid, CreateConstant(value, typeof(Guid)));
            return GuardNotNull(member,
                Expression.Call(EnumerableAny.MakeGenericMethod(elementType), member, Expression.Lambda(predicate, item)));
        }

        return GuardNotNull(member,
            Expression.Call(EnumerableContains.MakeGenericMethod(elementType), member, CreateConstant(value, elementType)));
    }

    private static Expression BuildStringOperation(Expression member, object? value, string methodName, bool ignoreCase)
    {
        if (member.Type != typeof(string))
            throw new ChillException($"{methodName} is only supported for strings (actual type: '{member.Type.Name}').");
        if (value == null)
            throw new ChillException($"{methodName} requires a value.");

        var sourceMember = member;
        var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        if (ignoreCase)
        {
            member = Expression.Call(member, nameof(string.ToLower), Type.EmptyTypes);
            text = text.ToLowerInvariant();
        }

        return GuardNotNull(sourceMember,
            Expression.Call(member, methodName, Type.EmptyTypes, Expression.Constant(text)));
    }

    private static Expression BuildIn(Expression member, object? value, bool ignoreCase)
    {
        IEnumerable? values = value switch
        {
            JsonElement { ValueKind: JsonValueKind.Array } jsonArray => jsonArray.EnumerateArray().ToArray(),
            string => null,
            IEnumerable enumerable => enumerable,
            _ => null
        };
        if (values == null)
            throw new ChillException("In requires a collection value.");

        var sourceMember = member;
        var isEntityReference = TryGetEntityGuidMember(member, out var guidMember);
        if (isEntityReference)
            member = guidMember;

        var converted = values.Cast<object?>().Select(item => ConvertValue(item, member.Type)).ToArray();
        if (ignoreCase && member.Type == typeof(string))
        {
            member = Expression.Call(member, nameof(string.ToLower), Type.EmptyTypes);
            converted = converted.Select(item => ((string?)item)?.ToLowerInvariant()).ToArray();
        }

        var array = Array.CreateInstance(member.Type, converted.Length);
        for (var index = 0; index < converted.Length; index++)
            array.SetValue(converted[index], index);

        var contains = Expression.Call(EnumerableContains.MakeGenericMethod(member.Type), Expression.Constant(array), member);
        return isEntityReference || (ignoreCase && sourceMember.Type == typeof(string))
            ? GuardNotNull(sourceMember, contains)
            : contains;
    }

    private static Expression BuildNullComparison(Expression member, bool negate)
    {
        if (member.Type.IsValueType && Nullable.GetUnderlyingType(member.Type) == null)
            throw new ChillException($"Null comparison is not valid for non-nullable '{member.Type.Name}'.");
        return negate
            ? Expression.NotEqual(member, Expression.Constant(null, member.Type))
            : Expression.Equal(member, Expression.Constant(null, member.Type));
    }

    private static Expression BuildEmpty(Expression member, bool negate)
    {
        Expression empty;
        if (member.Type == typeof(string))
        {
            empty = Expression.OrElse(
                Expression.Equal(member, Expression.Constant(null, typeof(string))),
                Expression.Equal(member, Expression.Constant(string.Empty)));
        }
        else
        {
            var elementType = GetEnumerableElementType(member.Type)
                ?? throw new ChillException($"Empty is not supported for '{member.Type.Name}'.");
            var any = Expression.Call(EnumerableAnyWithoutPredicate.MakeGenericMethod(elementType), member);
            empty = CanBeNull(member.Type)
                ? Expression.OrElse(Expression.Equal(member, Expression.Constant(null, member.Type)), Expression.Not(any))
                : Expression.Not(any);
        }

        return negate ? Expression.Not(empty) : empty;
    }

    private static Expression BuildQuantifier(Expression member, AutomaticQueryGroup? itemFilter, bool all)
    {
        var elementType = GetEnumerableElementType(member.Type)
            ?? throw new ChillException($"{(all ? "All" : "Any")} is only supported for collections.");
        if (itemFilter == null)
            throw new ChillException($"{(all ? "All" : "Any")} requires an item filter.");

        var item = Expression.Parameter(elementType, "item");
        var body = BuildGroup(itemFilter, item)
            ?? throw new ChillException($"{(all ? "All" : "Any")} requires at least one item filter.");
        var method = all ? EnumerableAll : EnumerableAny;
        return GuardNotNull(member,
            Expression.Call(method.MakeGenericMethod(elementType), member, Expression.Lambda(body, item)));
    }

    private static (Expression Member, Expression? NullGuard) BuildPropertyPath(Expression root, string propertyPath)
    {
        Expression current = root;
        Expression? guard = null;
        foreach (var segment in propertyPath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var property = current.Type.GetProperty(segment, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)
                ?? throw new ChillException($"Property '{segment}' was not found on '{current.Type.Name}' while resolving '{propertyPath}'.");

            if (current != root && CanBeNull(current.Type))
            {
                var notNull = Expression.NotEqual(current, Expression.Constant(null, current.Type));
                guard = guard == null ? notNull : Expression.AndAlso(guard, notNull);
            }
            current = Expression.Property(current, property);
        }

        return (current, guard);
    }

    private static Expression CreateConstant(object? value, Type targetType)
    {
        var converted = ConvertValue(value, targetType);
        var underlyingType = Nullable.GetUnderlyingType(targetType);
        if (converted != null && underlyingType != null)
            return Expression.Convert(Expression.Constant(converted, underlyingType), targetType);

        return Expression.Constant(converted, targetType);
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value is IChillEntity entity)
            value = entity.Guid;

        if (value is JsonElement json)
            value = ConvertJsonElement(json);

        if (value == null)
        {
            if (!CanBeNull(targetType))
                throw new ChillException($"Null cannot be converted to '{targetType.Name}'.");
            return null;
        }

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (underlyingType.IsInstanceOfType(value))
            return value;
        if (underlyingType.IsEnum)
            return value is string enumName
                ? Enum.Parse(underlyingType, enumName, ignoreCase: true)
                : Enum.ToObject(underlyingType, value);
        if (underlyingType == typeof(Guid))
            return value is Guid guid ? guid : Guid.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!);
        if (underlyingType == typeof(DateOnly))
            return value is DateOnly date ? date : DateOnly.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture);
        if (underlyingType == typeof(TimeOnly))
            return value is TimeOnly time ? time : TimeOnly.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture);
        if (underlyingType == typeof(DateTimeOffset))
            return value is DateTimeOffset offset ? offset : DateTimeOffset.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture);

        try
        {
            return Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            throw new ChillException($"Value '{value}' cannot be converted to '{targetType.Name}'.", exception);
        }
    }

    private static object? ConvertJsonElement(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => value.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when value.TryGetInt64(out var integer) => integer,
            JsonValueKind.Number => value.GetDouble(),
            _ => value.ToString()
        };
    }

    private static bool TryGetEntityGuidMember(Expression member, out Expression guidMember)
    {
        if (typeof(IChillEntity).IsAssignableFrom(member.Type))
        {
            guidMember = Expression.Property(member, nameof(IChillEntity.Guid));
            return true;
        }

        guidMember = null!;
        return false;
    }

    private static Type? GetEnumerableElementType(Type type)
    {
        if (type == typeof(string))
            return null;
        if (type.IsArray)
            return type.GetElementType();
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return type.GetGenericArguments()[0];

        return type.GetInterfaces()
            .FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            ?.GetGenericArguments()[0];
    }

    private static bool CanBeNull(Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) != null;

    private static Expression GuardNotNull(Expression member, Expression expression)
    {
        return CanBeNull(member.Type)
            ? Expression.AndAlso(Expression.NotEqual(member, Expression.Constant(null, member.Type)), expression)
            : expression;
    }

    private static MethodInfo GetEnumerableMethod(string name, int parameterCount)
    {
        return typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == name
                && method.GetParameters().Length == parameterCount
                && method.GetGenericArguments().Length == 1);
    }

    private static MethodInfo GetEnumerablePredicateMethod(string name)
    {
        return typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == name
                && method.GetParameters().Length == 2
                && method.GetGenericArguments().Length == 1
                && method.GetParameters()[1].ParameterType.GetGenericArguments().Length == 2);
    }
}
