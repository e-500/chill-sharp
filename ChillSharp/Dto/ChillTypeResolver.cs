using ChillSharp.EF;
using System.Reflection;

namespace ChillSharp.Dto
{
    public static class ChillTypeResolver
    {
        public static string NormalizeChillType(Type type, string shrinkTypePrefix)
        {
            var displayType = type.IsConstructedGenericType ? type.GetGenericTypeDefinition() : type;
            var typeName = displayType.FullName ?? displayType.Name;
            typeName = StripGenericArity(typeName);

            if (!string.IsNullOrEmpty(shrinkTypePrefix) && !shrinkTypePrefix.EndsWith("."))
            {
                shrinkTypePrefix += ".";
            }

            return typeName.Replace(shrinkTypePrefix, string.Empty);
        }

        public static Type ResolveType(Assembly assembly, string chillType, string chillTypePrefix)
        {
            var normalizedInput = NormalizeLookupKey(chillType, chillTypePrefix);
            var matches = assembly
                .GetTypes()
                .Select(type => new
                {
                    Type = type,
                    ShortName = NormalizeLookupKey(NormalizeChillType(type, chillTypePrefix), chillTypePrefix),
                    FullName = NormalizeLookupKey(type.FullName ?? type.Name, chillTypePrefix)
                })
                .Where(x => x.ShortName == normalizedInput
                    || x.FullName == normalizedInput
                    || normalizedInput.EndsWith("." + x.ShortName, StringComparison.Ordinal)
                    || x.ShortName.EndsWith("." + normalizedInput, StringComparison.Ordinal))
                .Select(x => x.Type)
                .ToList();

            if (matches.Count == 1)
            {
                return matches[0];
            }

            var exactFullName = assembly.GetType(PrepareFullChillType(chillType, chillTypePrefix));
            if (exactFullName != null)
            {
                return exactFullName;
            }

            throw new ChillException($"Unable to resolve ChillType '{chillType}' in assembly '{assembly.GetName().Name}'.");
        }

        public static object ActivateType(Assembly assembly, string chillType, string chillTypePrefix)
        {
            var resolvedType = ResolveType(assembly, chillType, chillTypePrefix);
            var concreteType = CloseQueryTypeIfNeeded(resolvedType)
                ?? throw new ChillException($"Unable to activate type '{resolvedType.FullName ?? resolvedType.Name}' for ChillType '{chillType}'.");

            var instance = Activator.CreateInstance(concreteType);
            if (instance == null)
            {
                throw new ChillException($"Activator was unable to instantiate type '{concreteType.FullName ?? concreteType.Name}'.");
            }

            return instance;
        }

        public static string PrepareFullChillType(string chillType, string chillTypePrefix)
        {
            var prefix = chillTypePrefix.Trim().TrimEnd('.');
            var normalized = StripGenericArity(chillType?.Trim().Trim('.') ?? string.Empty);

            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new ChillException("ChillType is required.");
            }

            return normalized.StartsWith(prefix + ".", StringComparison.Ordinal) ? normalized : $"{prefix}.{normalized}";
        }

        private static Type? CloseQueryTypeIfNeeded(Type type)
        {
            if (!type.ContainsGenericParameters)
            {
                return type;
            }

            if (!typeof(IChillQuery<IChillEntity>).IsAssignableFrom(type))
            {
                return null;
            }

            var entityType = ChillQueryTypeResolver.ResolveRelatedEntityType(type);
            if (entityType == null)
            {
                return null;
            }

            var genericArguments = type.GetGenericArguments()
                .Select(_ => entityType)
                .ToArray();

            return type.MakeGenericType(genericArguments);
        }

        private static string NormalizeLookupKey(string value, string chillTypePrefix)
        {
            var normalized = StripGenericArity(value?.Trim().Trim('.') ?? string.Empty);
            var prefix = chillTypePrefix.Trim().TrimEnd('.');

            if (!string.IsNullOrEmpty(prefix) && normalized.StartsWith(prefix + ".", StringComparison.Ordinal))
            {
                normalized = normalized[(prefix.Length + 1)..];
            }

            return normalized;
        }

        private static string StripGenericArity(string value)
        {
            var genericMetadataIndex = value.IndexOf("[[", StringComparison.Ordinal);
            if (genericMetadataIndex >= 0)
            {
                value = value[..genericMetadataIndex];
            }

            var tickIndex = value.IndexOf('`');
            if (tickIndex >= 0)
            {
                value = value[..tickIndex];
            }

            return value;
        }
    }
}
