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
using System.Globalization;
using System.Reflection;
using System.Text;

namespace ChillSharp.EF
{
    /// <summary>
    /// Implements IChillEntity and IChillValidable interfaces for entities managed by ChillSharp with EF Core integration.
    /// <para>Implementing this interface allows automatic handling of entity lifecycle events.</para>
    /// 
    /// <para>Licensing:
    /// This code is part of the ChillSharp library, released under the GNU GENERAL PUBLIC LICENSE v3 (GPLv3).<br/>
    /// Any modification or redistribution must comply with the GPLv3 license terms.<br/>
    /// For commercial or LGPL licensing options, please contact the author.<br/>
    /// © 2025 Andrea Piovesan
    /// </para>
    /// </summary>
    public abstract class ChillEntity : IChillValidable, IChillEntity
    {
        /// <summary>
        /// Encourages the use of GUIDs as primary keys to improve offline entity creation and synchronization.
        /// 
        /// <para>You can use this property as the primary key by decorating it with the <c>[Key]</c> attribute 
        /// in your derived class overriding this virtual property.</para>
        /// </summary>
        [ChillProperty]
        public virtual Guid Guid { get; set; }
        public string Label { get; set; } = string.Empty;
        public string ShortLabel { get; set; } = string.Empty;
        public string FullTextContent { get; set; } = string.Empty;

        [ChillProperty(
            UniquePropertyKeyString: "4C9CB824-15A8-4281-AF21-1C46868E4152",
            PrimaryLanguageLabel: "Checksum",
            SecondaryLanguageLabel: "Checksum")]
        public long Checksum { get; set; }

        [ChillProperty(
            UniquePropertyKeyString: "E6E02296-B840-48F1-85B3-9B7221CF8AB9",
            PrimaryLanguageLabel: "Last update user",
            SecondaryLanguageLabel: "Utente ultimo aggiornamento")]
        public string LastUpdateUser { get; set; } = string.Empty;

        [ChillProperty(
            UniquePropertyKeyString: "19604008-C926-4A9C-90C4-73D6FB8D37BB",
            PrimaryLanguageLabel: "Last update timestamp",
            SecondaryLanguageLabel: "Timestamp ultimo aggiornamento")]
        public DateTime LastUpdateUtc { get; set; }

        #region IChillEntity implementation
        #region CREATE
        /// <summary>
        /// Initializes default fields or calculated values when the entity is created.
        /// Called automatically by the <c>CREATE()</c> method.
        /// <para>Example: <c>CreatedAt = DateTime.UtcNow;</c></para>
        /// </summary>
        /// <param name="Context">The active database context.</param>
        public virtual void OnCreate(IChillContext Context) { Guid = Guid.NewGuid(); }
        #endregion

        #region SELECT
        /// <summary>
        /// Performs lightweight recalculations or adjustments before returning the entity to the UI.
        /// Called automatically by the <c>SEARCH()</c> method.
        /// </summary>
        /// <param name="Context">The active database context.</param>
        public virtual void OnSelect(IChillContext Context) { }

        /// <summary>
        /// Inflate a required property/reference/collection that can't be loaded automatically with EF
        /// To force ChillSharp to call OnInflate() anyway for a specific property/reference/collection
        /// Set 
        /// Called automatically by the <c>SEARCH()</c> method.
        /// </summary>
        /// <param name="Context">The active database context.</param>
        public virtual void OnInflate(IChillContext Context, string PropertyName) { }
        #endregion

        #region UPDATE
        /// <summary>
        /// Performs recalculations or adjustments before persisting entity changes to the database.
        /// Called by the <c>UPDATE()</c> method, before the transaction is committed.
        /// <para><b>Validation Note:</b> The client may run <c>VALIDATE()</c> before <c>UPDATE()</c> 
        /// to handle validation errors gracefully. However, if the client skips validation and proceeds 
        /// directly with <c>UPDATE()</c>, any validation errors will result in an exception (HTTP 500).</para>
        /// </summary>
        /// <param name="Context">The active database context.</param>
        public virtual void OnUpdate(IChillContext Context) { }

        /// <summary>
        /// Executes post-update logic after the entity changes have been saved and committed.
        /// </summary>
        /// <param name="Context">The active database context.</param>
        public virtual void OnAfterUpdate(IChillContext Context) { }

        // ChillEngine invokes OnAfterUpdate through the IChillEntity interface, not through the concrete class.
        // That means this explicit interface implementation is the real runtime entry point used by the engine.
        // It can therefore enforce audit-field maintenance first and only then delegate to the user-overridable
        // public virtual OnAfterUpdate(...) method, so derived classes keep a clean override surface without
        // being able to accidentally skip the base audit logic.
        void IChillEntity.OnAfterUpdate(IChillContext Context)
        {
            ChillDataAnnotationsValidator.ThrowIfInvalid(((IChillValidable)this).OnValidation(Context));
            UpdateAuditFields(Context);
            OnAfterUpdate(Context);
        }

        /// <summary>
        /// Recomputes the checksum and audit trail fields from the current Chill property values.
        /// </summary>
        /// <param name="Context">The active database context.</param>
        private void UpdateAuditFields(IChillContext Context)
        {
            LastUpdateUtc = DateTime.UtcNow;
            LastUpdateUser = Context.GetCurrentUserName() ?? string.Empty;
            Checksum = CalculateChecksum();
        }
        #endregion

        #region DELETE
        /// <summary>
        /// Performs cleanup operations before marking the entity as deleted.
        /// Typically used to handle foreign key relationships.
        /// </summary>
        /// <param name="Context">The active database context.</param>
        public virtual void OnDelete(IChillContext Context) { }

        /// <summary>
        /// Executes cleanup or post-deletion logic after the entity has been removed.
        /// <para><b>Note:</b> The entity might be deleted at this point — handle accordingly.</para>
        /// </summary>
        /// <param name="Context">The active database context.</param>
        public virtual void OnAfterDelete(IChillContext Context) { }
        #endregion

        #region HELPERS
        /// <summary>
        /// Returns a human-readable, descriptive string for the entity.
        /// </summary>
        /// <param name="Context">The active database context.</param>
        /// <returns>A descriptive label for the entity.</returns>
        public virtual string GetLabel(IChillContext Context)
        { 
            return $"ChillEntity Guid = {Guid}"; 
        }

        /// <summary>
        /// Returns a shorter, human-readable string for the entity (used in compact UI elements).
        /// </summary>
        /// <param name="Context">The active database context.</param>
        /// <returns>A short descriptive label for the entity.</returns>
        public virtual string GetShortLabel(IChillContext Context) 
        {
            string label = GetLabel(Context);
            if (label == null)
                return $"Chill({Guid.ToString().Substring(0,8)})";
            else 
                return $"Chill({(label.Length > 8 ? label.Substring(0, 8) : label)}";
        }

        /// <summary>
        /// Builds the full-text representation of the entity by combining its main fields.
        /// <para>Note: GetLabel() is used by default</para>
        /// </summary>
        /// <param name="context">The database context used to access related data.</param>
        /// <returns>The full-text string representing the entity.</returns>
        public virtual string GetFullTextContent(IChillContext Context)
        {
            return GetLabel(Context);
        }

        /// <summary>
        /// Calculates the entity checksum by summing the serialized values of all Chill properties.
        /// </summary>
        /// <returns>The checksum for the current entity state.</returns>
        protected virtual long CalculateChecksum()
        {
            long checksum = 0;
            var chillProperties = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.IsDefined(typeof(ChillPropertyAttribute), inherit: true));

            foreach (var property in chillProperties)
            {
                if (property.Name is nameof(Checksum) or nameof(LastUpdateUser) or nameof(LastUpdateUtc))
                {
                    continue;
                }

                var serializedValue = SerializeChecksumValue(property.GetValue(this));
                foreach (var b in Encoding.UTF8.GetBytes(serializedValue))
                {
                    checksum += b;
                }
            }

            return checksum;
        }

        private static string SerializeChecksumValue(object? value)
        {
            if (value is null)
            {
                return string.Empty;
            }

            if (value is IChillEntity chillEntity)
            {
                return chillEntity.Guid.ToString("N", CultureInfo.InvariantCulture);
            }

            if (value is System.Collections.IEnumerable enumerable and not string)
            {
                var builder = new StringBuilder();
                foreach (var item in enumerable)
                {
                    builder.Append(SerializeChecksumValue(item));
                    builder.Append('|');
                }

                return builder.ToString();
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }
        #endregion
        #endregion

        #region IChillValidation implementation
        /// <summary>
        /// Called by the <c>AUTOCOMPLETE()</c> method to fill or adjust fields 
        /// based on the current ("dirty") state of the entity.
        /// </summary>
        /// <param name="Context">The active database context.</param>
        public virtual void OnAutocomplete(IChillContext Context) { }

        /// <summary>
        /// Validates entity fields before an update operation.
        /// <para>Called by:</para>
        /// <list type="bullet">
        /// <item><description><c>VALIDATE()</c> — returns validation issues gracefully to the client.</description></item>
        /// <item><description><c>UPDATE()</c> — throws an exception if validation fails.</description></item>
        /// </list>
        /// </summary>
        /// <param name="Context">The active database context.</param>
        /// <returns>A collection of validation results.</returns>
        public virtual IEnumerable<ChillValidationError> OnValidation(IChillContext Context) { return new List<ChillValidationError>(); }

        /// <summary>
        /// Returns optional validation message definitions that can translate GUID-based
        /// DataAnnotations error messages using ChillSharp primary/secondary texts.
        /// </summary>
        /// <param name="Context">The active database context.</param>
        /// <returns>The validation message definitions available for the entity.</returns>
        public virtual IEnumerable<ChillValidationMessageDefinition> GetValidationMessageDefinitions(IChillContext Context) { return new List<ChillValidationMessageDefinition>(); }

        // ChillSharp invokes validation through the IChillValidable interface, not through the concrete class.
        // This explicit implementation guarantees that framework DataAnnotations run first for the Chill
        // properties only, then the user-defined OnValidation(...) logic is appended without requiring
        // derived classes to call base.OnValidation(...).
        IEnumerable<ChillValidationError> IChillValidable.OnValidation(IChillContext Context)
        {
            var errors = new List<ChillValidationError>();
            errors.AddRange(ChillDataAnnotationsValidator.ValidateChillProperties(this, Context, GetValidationMessageDefinitions(Context)));
            errors.AddRange(OnValidation(Context));
            return errors;
        }
        #endregion
    }
}
