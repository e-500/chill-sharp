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

using ChillSharp.Annotations;
using ChillSharp.Dto;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace ChillSharp.EF
{
    internal static class ChillDataAnnotationsValidator
    {
        public static IReadOnlyList<ChillValidationError> ValidateChillProperties(
            object instance,
            IChillContext? context,
            IEnumerable<ChillValidationMessageDefinition>? messageDefinitions)
        {
            var errors = new List<ChillValidationError>();
            var messageDefinitionsByGuid = messageDefinitions?
                .Where(x => x != null && x.MessageGuid != Guid.Empty)
                .GroupBy(x => x.MessageGuid)
                .ToDictionary(x => x.Key, x => x.Last()) ?? new Dictionary<Guid, ChillValidationMessageDefinition>();

            foreach (var property in GetChillProperties(instance.GetType()))
            {
                var validationContext = new ValidationContext(instance)
                {
                    MemberName = property.Name
                };

                var propertyErrors = new List<ValidationResult>();
                Validator.TryValidateProperty(property.GetValue(instance), validationContext, propertyErrors);

                foreach (var error in propertyErrors)
                {
                    var memberNames = error.MemberNames
                        .Where(memberName => string.Equals(memberName, property.Name, StringComparison.Ordinal))
                        .DefaultIfEmpty(property.Name);

                    foreach (var memberName in memberNames)
                    {
                        errors.Add(new ChillValidationError
                        {
                            FieldName = memberName,
                            Message = ResolveValidationMessage(
                                error.ErrorMessage,
                                property.Name,
                                context,
                                messageDefinitionsByGuid)
                        });
                    }
                }
            }

            return errors;
        }

        public static void ThrowIfInvalid(IEnumerable<ChillValidationError> errors)
        {
            var errorList = errors.ToList();
            if (errorList.Count == 0)
                return;

            throw new ChillValidationException(BuildMessage(errorList));
        }

        public static string BuildMessage(IEnumerable<ChillValidationError> errors)
        {
            var builder = new StringBuilder();
            foreach (var error in errors)
            {
                if (builder.Length > 0)
                    builder.AppendLine();

                if (!string.IsNullOrWhiteSpace(error.FieldName))
                {
                    builder.Append(error.FieldName);
                    builder.Append(": ");
                }

                builder.Append(error.Message);
            }

            return builder.ToString();
        }

        private static IEnumerable<PropertyInfo> GetChillProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.IsDefined(typeof(ChillPropertyAttribute), inherit: true));
        }

        private static string ResolveValidationMessage(
            string? errorMessage,
            string propertyName,
            IChillContext? context,
            IReadOnlyDictionary<Guid, ChillValidationMessageDefinition> messageDefinitionsByGuid)
        {
            if (!string.IsNullOrWhiteSpace(errorMessage)
                && Guid.TryParse(errorMessage, out var messageGuid)
                && messageDefinitionsByGuid.TryGetValue(messageGuid, out var messageDefinition))
            {
                return ChillLabelResolver.Resolve(
                    messageDefinition.PrimaryLanguageMessage,
                    messageDefinition.SecondaryLanguageMessage,
                    messageGuid.ToString(),
                    context);
            }

            return errorMessage ?? $"The field {propertyName} is invalid.";
        }
    }
}
