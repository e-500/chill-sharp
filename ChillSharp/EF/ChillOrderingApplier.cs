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

using System.Linq.Expressions;
using System.Reflection;

namespace ChillSharp.EF;

internal static class ChillOrderingApplier
{
    public static IQueryable<T> ApplyOrdering<T>(IQueryable<T> query, ChillOrdering? ordering, Type? entityType = null)
        where T : IChillEntity
    {
        var effectiveOrdering = ordering ?? new ChillOrdering();
        var propertyName = string.IsNullOrWhiteSpace(effectiveOrdering.PropertyName)
            ? nameof(IChillEntity.Position)
            : effectiveOrdering.PropertyName.Trim();

        var effectiveEntityType = entityType ?? typeof(T);
        var property = ResolveProperty(effectiveEntityType, propertyName);
        if (property == null)
        {
            return ApplyStableGuidOrdering(query, effectiveOrdering.IsDescending());
        }

        var parameter = Expression.Parameter(typeof(T), "entity");
        Expression propertyAccess = property.DeclaringType == typeof(T)
            ? Expression.Property(parameter, property)
            : Expression.Property(Expression.Convert(parameter, property.DeclaringType!), property);

        Type keyType = property.PropertyType;
        if (typeof(IChillEntity).IsAssignableFrom(property.PropertyType))
        {
            propertyAccess = Expression.Property(propertyAccess, nameof(IChillEntity.Label));
            propertyAccess = Expression.Coalesce(propertyAccess, Expression.Constant(string.Empty));
            keyType = typeof(string);
        }

        var lambda = Expression.Lambda(propertyAccess, parameter);
        var orderedQuery = InvokeOrderingMethod(
            effectiveOrdering.IsDescending() ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy),
            query,
            lambda,
            typeof(T),
            keyType);

        return InvokeOrderingMethod(nameof(Queryable.ThenBy), orderedQuery, Expression.Lambda(Expression.Property(parameter, nameof(IChillEntity.Guid)), parameter), typeof(T), typeof(Guid));
    }

    private static PropertyInfo? ResolveProperty(Type entityType, string propertyName)
    {
        return entityType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)
            ?? typeof(IChillEntity).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
    }

    private static IQueryable<T> ApplyStableGuidOrdering<T>(IQueryable<T> query, bool descending)
        where T : IChillEntity
    {
        return descending
            ? query.OrderByDescending(x => x.Guid)
            : query.OrderBy(x => x.Guid);
    }

    private static IQueryable<T> InvokeOrderingMethod<T>(string methodName, IQueryable<T> source, LambdaExpression keySelector, Type sourceType, Type keyType)
        where T : IChillEntity
    {
        var method = typeof(Queryable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m =>
                m.Name == methodName &&
                m.IsGenericMethodDefinition &&
                m.GetParameters().Length == 2);

        return (IQueryable<T>)method
            .MakeGenericMethod(sourceType, keyType)
            .Invoke(null, [source, keySelector])!;
    }
}
