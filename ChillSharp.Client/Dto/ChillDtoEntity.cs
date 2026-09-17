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

using System.Text.Json;

namespace ChillSharp.Client.Dto
{
    /// <summary>
    /// Represents a lightweight web-friendly version of an EF Core entity.
    /// This class omits collection properties (navigation lists, etc.) for simplicity
    /// and is intended for serialization or transmission via web APIs.
    /// 
    /// <para>Licensing:
    /// This code is part of the ChillSharp library, released under the terms of the 
    /// GNU Affero General Public License as published by the Free Software Foundation, 
    /// either version 3 of the License, or (at your option) any later version.<br/>
    /// For commercial or LGPL licensing options, please contact the author.<br/>
    /// ©️2025 Andrea Piovesan</para>
    /// </summary>
    public class ChillDtoEntity
    {
        /// <summary>
        /// Globally unique identifier of the object.
        /// This corresponds to the entity's primary key in EF Core.
        /// </summary>
        public Guid Guid { get; set; }

        /// <summary>
        /// Optional string identifying the entity type or category.
        /// Useful for polymorphic handling of different entity types in a generic web model.
        /// </summary>
        public string ChillType { get; set; } = string.Empty;

        /// <summary>
        /// A human-readable label or name for the object.
        /// Commonly used for displaying the entity in UI lists or dropdowns.
        /// </summary>
        public string? Label { get; set; } = null;

        /// <summary>
        /// An abbreviated version of the label, if applicable.
        /// Often used in compact UI representations or summaries.
        /// </summary>
        public string? ShortLabel { get; set; } = null;

        /// <summary>
        /// A dictionary mapping field names (property keys) to their corresponding values.
        /// </summary>
        public Dictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();

        #region HELPERS
        public ChillDtoEntity Mock()
        {
            var mock = new ChillDtoEntity();
            mock.Guid = Guid;
            mock.ChillType = ChillType;
            mock.Label = Label;
            mock.ShortLabel = ShortLabel;
            return mock;
        }

        private (JsonElement?, object? GenericObject) _GetElement(string PropertyName)
        {
            if (!Properties.ContainsKey(PropertyName))
                return (null, null);

            var obj = (JsonElement?)Properties[PropertyName];
            if (!obj.HasValue) 
                return (null, null);

            var type = obj!.GetType();
            if (type == typeof(JsonElement))
            {
                var jel = (JsonElement)obj!;
                switch (jel.ValueKind)
                {
                    case JsonValueKind.Null:
                        return (null, null);
                    case JsonValueKind.Undefined:
                        return (null, null);
                    default:
                        return (jel, null);
                }
            }
            else
                return (null, obj);
        }

        /// <summary>
        /// Return true if property name exists and if value is not null or undefined in any type (generally incapsulated in a JsonElement)
        /// </summary>
        /// <param name="PropertyName"></param>
        /// <returns></returns>
        public bool HasValue(string PropertyName)
        {
            (var jEl, var obj) = _GetElement(PropertyName);

            if (jEl.HasValue)
                return true;
            else if (obj != null)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Return true if property name exists and if value is not null or undefined in any type (generally incapsulated in a JsonElement)
        /// </summary>
        /// <param name="PropertyName"></param>
        /// <returns></returns>
        public object? GetValue(string PropertyName)
        {
            (var jEl, var obj) = _GetElement(PropertyName);

            if (jEl.HasValue)
            {
                switch (jEl.Value.ValueKind)
                {
                    case JsonValueKind.Null:
                        return null;
                    case JsonValueKind.Undefined:
                        return null;
                    case JsonValueKind.String:
                        return jEl.Value.GetString();
                    case JsonValueKind.Number:
                        return jEl.Value.GetDouble();
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        return jEl.Value.GetBoolean();
                    default:
                        return jEl;
                }
            }
            else if (obj != null)
                return obj;
            else
                return null;
        }

        /// <summary>
        /// Return property value as string type (generally incapsulated in a JsonElement)
        /// </summary>
        /// <param name="PropertyName"></param>
        /// <returns></returns>
        public bool? GetBoolean(string PropertyName)
        {
            (var jEl, var obj) = _GetElement(PropertyName);

            if (jEl.HasValue && (jEl.Value.ValueKind == JsonValueKind.True || jEl.Value.ValueKind == JsonValueKind.False))
                return jEl.Value.GetBoolean();
            else if (obj != null && obj.GetType() == typeof(bool))
                return (bool)obj;
            else
                throw new ChillClientException("Can't return property value as a string");
        }

        /// <summary>
        /// Return property value as int type (generally incapsulated in a JsonElement)
        /// </summary>
        /// <param name="PropertyName"></param>
        /// <returns></returns>
        public int? GetInt32(string PropertyName)
        {
            (var jEl, var obj) = _GetElement(PropertyName);

            if (jEl.HasValue && jEl.Value.ValueKind == JsonValueKind.Number)
                return jEl.Value.GetInt32();
            else if (obj != null && obj.GetType() == typeof(int))
                return (int)obj;
            else
                throw new ChillClientException("Can't return property value as a int");
        }

        /// <summary>
        /// Return property value as int type (generally incapsulated in a JsonElement)
        /// </summary>
        /// <param name="PropertyName"></param>
        /// <returns></returns>
        public long? GetInt64(string PropertyName)
        {
            (var jEl, var obj) = _GetElement(PropertyName);

            if (jEl.HasValue && jEl.Value.ValueKind == JsonValueKind.Number)
                return jEl.Value.GetInt64();
            else if (obj != null && obj.GetType() == typeof(long))
                return (long)obj;
            else
                throw new ChillClientException("Can't return property value as a int");
        }

        /// <summary>
        /// Return property value as int type (generally incapsulated in a JsonElement)
        /// </summary>
        /// <param name="PropertyName"></param>
        /// <returns></returns>
        public float? GetSingle(string PropertyName)
        {
            (var jEl, var obj) = _GetElement(PropertyName);

            if (jEl.HasValue && jEl.Value.ValueKind == JsonValueKind.Number)
                return jEl.Value.GetSingle();
            else if (obj != null && obj.GetType() == typeof(float))
                return (float)obj;
            else
                throw new ChillClientException("Can't return property value as a int");
        }

        /// <summary>
        /// Return property value as int type (generally incapsulated in a JsonElement)
        /// </summary>
        /// <param name="PropertyName"></param>
        /// <returns></returns>
        public double? GetDouble(string PropertyName)
        {
            (var jEl, var obj) = _GetElement(PropertyName);

            if (jEl.HasValue && jEl.Value.ValueKind == JsonValueKind.Number)
                return jEl.Value.GetDouble();
            else if (obj != null && obj.GetType() == typeof(double))
                return (double)obj;
            else
                throw new ChillClientException("Can't return property value as a int");
        }

        /// <summary>
        /// Return property value as string type (generally incapsulated in a JsonElement)
        /// </summary>
        /// <param name="PropertyName"></param>
        /// <returns></returns>
        public string? GetString(string PropertyName)
        {
            (var jEl, var obj) = _GetElement(PropertyName);

            if (jEl.HasValue && jEl.Value.ValueKind == JsonValueKind.String)
                return jEl.Value.GetString();
            else if (obj != null && obj.GetType() == typeof(string))
                return (string)obj;
            else
                throw new ChillClientException("Can't return property value as a string");
        }

        /// <summary>
        /// Return property value as array of ChillDtoEntity type (generally incapsulated in a JsonElement)
        /// </summary>
        /// <param name="PropertyName"></param>
        /// <returns></returns>
        public IEnumerable<ChillDtoEntity> GetCollection(string PropertyName)
        {
            (var jEl, var obj) = _GetElement(PropertyName);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            if (jEl.HasValue && jEl.Value.ValueKind == JsonValueKind.Array)
                return jEl.Value.EnumerateArray().Select(x => x.Deserialize<ChillSharp.Client.Dto.ChillDtoEntity>(options)!);
            else if (obj != null && obj.GetType() == typeof(List<ChillDtoEntity>))
                return (List<ChillDtoEntity>)obj;
            else
                throw new ChillClientException("Can't return property value as a List<ChillDtoEntity>");
        }

        #endregion
    }
}
