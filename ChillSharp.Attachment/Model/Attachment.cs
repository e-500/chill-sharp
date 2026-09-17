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
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChillSharp.Attachment.Model;

/// <summary>
/// Stores metadata for a file archived by the attachment module.
/// </summary>
[ChillEntity(
    UniquePropertyKeyString: "D6A01B75-2EA0-44F4-AFC9-47FB9C566CD4",
    PrimaryLanguageLabel: "Attachment",
    SecondaryLanguageLabel: "Allegato",
    LabelFormatString = "{Title}",
    ShortLabelFormatString = "{OriginalFilename}",
    FullTextContentFormatString = "{Title} {Description} {OriginalFilename}")]
[Table("attachment")]
public class Attachment : ChillEntity
{
    [Key]
    public override Guid Guid { get; set; }

    [ChillProperty(
        UniquePropertyKeyString: "6510409C-C2E1-4DCA-B3CB-631ABAB77E54",
        PrimaryLanguageLabel: "Attached chill type",
        SecondaryLanguageLabel: "Tipo chill collegato")]
    public string AttachToChillType { get; set; } = string.Empty;

    [ChillProperty(
        UniquePropertyKeyString: "11A3AFBF-6CEB-4024-BD51-B444279B3DAF",
        PrimaryLanguageLabel: "Attached guid",
        SecondaryLanguageLabel: "Guid collegato")]
    public Guid AttachToGuid { get; set; }

    [ChillProperty(
        UniquePropertyKeyString: "CB6D7E58-22E6-42F2-A828-B14C0D2C4EC2",
        PrimaryLanguageLabel: "Original filename",
        SecondaryLanguageLabel: "Nome file originale",
        MaxLength: 512)]
    public string OriginalFilename { get; set; } = string.Empty;

    [ChillProperty(
        UniquePropertyKeyString: "2B819641-7042-4A86-BC66-91FF0D0C15A5",
        PrimaryLanguageLabel: "Extension",
        SecondaryLanguageLabel: "Estensione",
        MaxLength: 32)]
    public string Extension { get; set; } = string.Empty;

    [ChillProperty(
        UniquePropertyKeyString: "7E465DA7-9E16-48D4-984A-B66A8554A8AF",
        PrimaryLanguageLabel: "Mime type",
        SecondaryLanguageLabel: "Mime type",
        MaxLength: 255)]
    public string MimeType { get; set; } = "application/octet-stream";

    [ChillProperty(
        UniquePropertyKeyString: "92C31A96-D747-490D-8A4D-2175C181E80C",
        PrimaryLanguageLabel: "Title",
        SecondaryLanguageLabel: "Titolo",
        MaxLength: 512)]
    public string Title { get; set; } = string.Empty;

    [ChillProperty(
        UniquePropertyKeyString: "D679D4D4-FB4E-474B-848C-5BBBE38A4F0C",
        PrimaryLanguageLabel: "Description",
        SecondaryLanguageLabel: "Descrizione",
        MaxLength: 4096)]
    public string Description { get; set; } = string.Empty;

    [ChillProperty(
        UniquePropertyKeyString: "BC4B1775-3937-4B56-9EF2-A4F1962A5AF7",
        PrimaryLanguageLabel: "Public",
        SecondaryLanguageLabel: "Pubblico")]
    public bool Public { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public override void OnCreate(IChillContext context)
    {
        if (Guid == Guid.Empty)
        {
            Guid = Guid.NewGuid();
        }

        if (CreatedAtUtc == default)
        {
            CreatedAtUtc = DateTime.UtcNow;
        }
    }

    public override void OnDelete(IChillContext context)
    {
        if (context is not DbContext dbContext)
        {
            return;
        }

        try
        {
            var serviceProvider = ((IInfrastructure<IServiceProvider>)dbContext).Instance;
            var archive = serviceProvider.GetService(typeof(Services.IChillAttachmentArchive)) as Services.IChillAttachmentArchive;
            archive?.DeleteIfExists(this);
        }
        catch
        {
        }
    }
}
