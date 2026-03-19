using ChillSharp.EF;

namespace ChillSharp.Dto
{
    internal static class ChillQueryTypeResolver
    {
        internal static Type? ResolveRelatedEntityType(Type queryType)
        {
            return ResolveFromQueryInterfaces(queryType)
                ?? ResolveFromGenericArguments(queryType)
                ?? ResolveFromQueryName(queryType);
        }

        private static Type? ResolveFromQueryInterfaces(Type queryType)
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
