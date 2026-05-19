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
using ChillSharp.EF;

namespace ChillSharp.Attachment.Model;

/// <summary>
/// Lists attachments linked to a specific Chill entity.
/// </summary>
[ChillEntity(
    UniquePropertyKeyString: "2C9303C2-7693-4341-B365-C05F8ED2FBA4",
    PrimaryLanguageLabel: "Attachment query",
    SecondaryLanguageLabel: "Ricerca allegati")]
public sealed class AttachmentQuery : ChillQuery
{
    [ChillProperty(
        UniquePropertyKeyString: "45EAF070-9057-4258-9EB8-C6D3F17D8344",
        PrimaryLanguageLabel: "Attached chill type",
        SecondaryLanguageLabel: "Tipo chill collegato")]
    public string AttachToChillType { get; set; } = string.Empty;

    [ChillProperty(
        UniquePropertyKeyString: "E5B578A7-90B5-41E2-A4C3-52C86A8A03B6",
        PrimaryLanguageLabel: "Attached guid",
        SecondaryLanguageLabel: "Guid collegato")]
    public Guid? AttachToGuid { get; set; }

    public override IQueryable<IChillEntity> OnQuery(IChillContext Context, bool LightweightRequired = false)
    {
        var query = base.OnQuery(Context, LightweightRequired).OfType<FileMetadata>();

        var normalizedChillType = AttachToChillType?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedChillType))
        {
            query = query.Where(x => x.AttachToChillType == normalizedChillType);
        }

        if (AttachToGuid is Guid attachToGuid && attachToGuid != System.Guid.Empty)
        {
            query = query.Where(x => x.AttachToGuid == attachToGuid);
        }

        return query;
    }

    public override IQueryable<IChillEntity> OnOrderingBy(IChillContext Context, IQueryable<IChillEntity> Query)
    {
        return Query
            .OfType<FileMetadata>()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenBy(x => x.Guid)
            .Cast<IChillEntity>();
    }
}
