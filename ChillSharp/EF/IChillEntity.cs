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

using ChillSharp.Dto;

namespace ChillSharp.EF
{
    /// <summary>
    /// Defines an interface for entities managed by ChillSharp with EF Core integration.
    /// Implementing this interface allows automatic handling of entity lifecycle events.
    /// 
    /// <para>Licensing:
    /// This code is part of the ChillSharp library, released under the terms of the 
    /// GNU Affero General Public License as published by the Free Software Foundation, 
    /// either version 3 of the License, or (at your option) any later version.<br/>
    /// For commercial or LGPL licensing options, please contact the author.<br/>
    /// © 2025 Andrea Piovesan
    /// </para>
    /// </summary>
    public interface IChillEntity : IChillable
    {
        /// <summary>
        /// Encourages the use of GUIDs as primary keys to improve offline entity creation and synchronization.
        /// <para>
        /// You can use this property as the primary key by decorating it with the <c>[Key]</c> attribute.
        /// </para>
        /// </summary>
        Guid Guid { get; set; }
        int Position { get; set; }
        string Label { get; set; }
        string ShortLabel { get; set; }
        string FullTextContent { get; set; }
        long Checksum { get; set; }
        string? LastUpdateUser { get; set; }
        DateTime? LastUpdate { get; set; }
        int LastUpdateUtcOffset { get; set; }

        #if DEBUG
        public virtual void OnDebugRequestDto(IChillContext Context, ChillDtoEntity Entity) { }
        public virtual void OnDebugResponseDto(IChillContext Context, ChillDtoEntity Entity) { }
        #endif

        #region CREATE
        /// <summary>
        /// Initializes default fields or calculated values when the entity is created.
        /// Called automatically by the <c>CREATE()</c> method.
        /// <para>Example: <c>CreatedAt = DateTime.Now;</c></para>
        /// </summary>
        /// <param name="Context">The active database context.</param>
        void OnCreate(IChillContext Context);
        #endregion

        #region SELECT
        /// <summary>
        /// Performs lightweight recalculations or adjustments before returning the entity to the UI.
        /// Called automatically by the <c>SEARCH()</c> method.
        /// </summary>
        /// <param name="Context">The active database context.</param>
        void OnSelect(IChillContext Context, bool LightweightRequired = false);

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
        void OnUpdate(IChillContext Context);

        /// <summary>
        /// Executes post-update logic after the entity changes have been saved and committed.
        /// </summary>
        /// <param name="Context">The active database context.</param>
        void OnAfterUpdate(IChillContext Context);

        /// <summary>
        /// Collection change or adjustment needed.<br/>
        /// <para>ChillSharp, at the moment, need your help to handle the collection properly<br/>
        /// This method is called during ToEntity() process, analyze the current (db) collection to guess what was added or removed without loading the entities giving you only the guids.</para>
        /// </summary>
        /// <param name="Context"></param>
        /// <param name="CollectionName"></param>
        /// <param name="AddedEntityGuids"></param>
        /// <param name="RemovedEntitiesGuids"></param>
        public virtual void OnCollectionUpdate(IChillContext Context, string CollectionName, Guid[] AddedEntityGuids, Guid[] RemovedEntitiesGuids) { }
        #endregion

        #region DELETE
        /// <summary>
        /// Performs cleanup operations before marking the entity as deleted.
        /// Typically used to handle foreign key relationships.
        /// </summary>
        /// <param name="Context">The active database context.</param>
        void OnDelete(IChillContext Context);

        /// <summary>
        /// Executes cleanup or post-deletion logic after the entity has been removed.
        /// <para><b>Note:</b> The entity might be deleted at this point — handle accordingly.</para>
        /// </summary>
        /// <param name="Context">The active database context.</param>
        void OnAfterDelete(IChillContext Context);
        #endregion

        #region HELPERS
        /// <summary>
        /// Returns a human-readable, descriptive string for the entity.
        /// </summary>
        /// <param name="Context">The active database context.</param>
        /// <returns>A descriptive label for the entity.</returns>
        string GetLabel(IChillContext Context);

        /// <summary>
        /// Returns a shorter, human-readable string for the entity (used in compact UI elements).
        /// </summary>
        /// <param name="Context">The active database context.</param>
        /// <returns>A short descriptive label for the entity.</returns>
        string GetShortLabel(IChillContext Context);

        /// <summary>
        /// Builds the full-text representation of the entity by combining its main fields.
        /// <para>Note: GetLabel() is used by default</para>
        /// </summary>
        /// <param name="context">The database context used to access related data.</param>
        /// <returns>The full-text string representing the entity.</returns>
        string GetFullTextContent(IChillContext Context);

        /// <summary>
        /// Returns optional localized validation message definitions that can be referenced
        /// by placing a GUID string inside a DataAnnotations <c>ErrorMessage</c>.
        /// </summary>
        /// <param name="Context">The active database context.</param>
        /// <returns>The validation message definitions available for the entity.</returns>
        IEnumerable<ChillValidationMessageDefinition> GetValidationMessageDefinitions(IChillContext Context)
        {
            return [];
        }
        #endregion
    }
}
