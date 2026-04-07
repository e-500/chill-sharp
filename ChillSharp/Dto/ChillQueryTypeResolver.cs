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

using ChillSharp.EF;

namespace ChillSharp.Dto
{
    public static class ChillQueryTypeResolver
    {
        public static Type? ResolveRelatedEntityType(Type queryType)
        {
            return ResolveFromQueryInterfaces(queryType)
                ?? ResolveFromGenericArguments(queryType)
                ?? ResolveFromQueryName(queryType);
        }

        public static Type? ResolveFromQueryInterfaces(Type queryType)
        {
            return queryType
                .GetInterfaces()
                .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IChillQuery<>))
                .Select(x => x.GetGenericArguments()[0])
                .Where(IsConcreteEntityType)
                .OrderBy(x => x == typeof(IChillEntity) ? 1 : 0)
                .FirstOrDefault();
        }

        private static Type? ResolveFromGenericArguments(Type queryType)
        {
            foreach (var genericArgument in queryType.GetGenericArguments())
            {
                if (IsConcreteEntityType(genericArgument))
                {
                    return genericArgument;
                }

                if (genericArgument.IsGenericParameter)
                {
                    var inferredType = ResolveEntityTypeByName(queryType, genericArgument.Name);
                    if (inferredType != null)
                    {
                        return inferredType;
                    }
                }
            }

            return null;
        }

        private static Type? ResolveFromQueryName(Type queryType)
        {
            var queryName = queryType.Name;
            var genericTickIndex = queryName.IndexOf('`');
            if (genericTickIndex >= 0)
            {
                queryName = queryName[..genericTickIndex];
            }

            if (!queryName.EndsWith("Query", StringComparison.Ordinal) || queryName.Length <= "Query".Length)
            {
                return null;
            }

            return ResolveEntityTypeByName(queryType, queryName[..^"Query".Length]);
        }

        private static Type? ResolveEntityTypeByName(Type queryType, string entityTypeName)
        {
            if (string.IsNullOrWhiteSpace(entityTypeName))
            {
                return null;
            }

            var matches = queryType.Assembly
                .GetTypes()
                .Where(IsConcreteEntityType)
                .Where(x => string.Equals(x.Name, entityTypeName, StringComparison.Ordinal))
                .ToList();

            return matches.Count == 1 ? matches[0] : null;
        }

        private static bool IsConcreteEntityType(Type type)
        {
            return type != typeof(IChillEntity)
                && type.IsClass
                && !type.IsAbstract
                && typeof(IChillEntity).IsAssignableFrom(type);
        }
    }
}
