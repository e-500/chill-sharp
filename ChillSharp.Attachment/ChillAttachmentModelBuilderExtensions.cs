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

using ChillSharp.Attachment.Model;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Attachment;

/// <summary>
/// Provides shared EF Core model configuration for attachment persistence.
/// </summary>
public static class ChillAttachmentModelBuilderExtensions
{
    /// <summary>
    /// Applies the attachment model to an existing <see cref="ModelBuilder"/>.
    /// </summary>
    public static ModelBuilder AddChillAttachmentModel(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileMetadata>(builder =>
        {
            builder.Property(x => x.AttachToChillType).HasMaxLength(512);
            builder.Property(x => x.OriginalFilename).HasMaxLength(512);
            builder.Property(x => x.Extension).HasMaxLength(32);
            builder.Property(x => x.MimeType).HasMaxLength(255);
            builder.Property(x => x.Title).HasMaxLength(512);
            builder.Property(x => x.Description).HasMaxLength(4096);

            builder.HasIndex(x => x.AttachToGuid);
            builder.HasIndex(x => new { x.AttachToChillType, x.AttachToGuid });
            builder.HasIndex(x => x.Public);
        });

        return modelBuilder;
    }
}
