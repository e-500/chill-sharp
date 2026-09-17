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

using Microsoft.Extensions.Options;

namespace ChillSharp.Attachment.Services;

/// <summary>
/// Default filesystem-backed attachment archive.
/// </summary>
public sealed class ChillAttachmentArchive : IChillAttachmentArchive
{
    private readonly ChillAttachmentOptions _options;

    public ChillAttachmentArchive(IOptions<ChillAttachmentOptions> options)
    {
        _options = options.Value;
    }

    public string BuildPath(Model.Attachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        return BuildAttachmentPath(
            ResolveArchiveRoot(),
            attachment.AttachToChillType,
            attachment.Guid,
            attachment.Extension,
            attachment.CreatedAtUtc);
    }

    public async Task SaveAsync(Model.Attachment attachment, Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        ArgumentNullException.ThrowIfNull(content);

        var path = BuildPath(attachment);
        var directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("Attachment path does not contain a directory.");

        Directory.CreateDirectory(directory);
        await using var targetStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(targetStream, cancellationToken);
    }

    public Stream OpenRead(Model.Attachment attachment)
    {
        return new FileStream(BuildPath(attachment), FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public bool Exists(Model.Attachment attachment)
    {
        return File.Exists(BuildPath(attachment));
    }

    public void DeleteIfExists(Model.Attachment attachment)
    {
        var path = BuildPath(attachment);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public static string BuildAttachmentPath(string archiveRoot, string attachToChillType, Guid id, string extension, DateTime createdAtUtc)
    {
        var normalizedRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(archiveRoot) ? Path.Combine(AppContext.BaseDirectory, "attachments") : archiveRoot);
        var section = SanitizePathSegment(attachToChillType);
        var normalizedExtension = NormalizeExtension(extension);
        var guid = id.ToString("N");
        var p1 = guid.Substring(0, 2);
        var p2 = guid.Substring(2, 2);

        return Path.Combine(
            normalizedRoot,
            section,
            createdAtUtc.Year.ToString(),
            p1,
            p2,
            guid + normalizedExtension);
    }

    private string ResolveArchiveRoot()
    {
        var environmentOverride = Environment.GetEnvironmentVariable(ChillAttachmentOptions.ArchiveRootEnvironmentVariableName)?.Trim();
        var configuredRoot = string.IsNullOrWhiteSpace(environmentOverride) ? _options.ArchiveRoot : environmentOverride;
        return Path.GetFullPath(configuredRoot);
    }

    private static string SanitizePathSegment(string? value)
    {
        var raw = string.IsNullOrWhiteSpace(value) ? "general" : value.Trim();
        var invalidChars = Path.GetInvalidFileNameChars()
            .Concat(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar })
            .Distinct()
            .ToArray();

        var sanitized = new string(raw.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "general" : sanitized;
    }

    private static string NormalizeExtension(string? extension)
    {
        var normalized = extension?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        return normalized.StartsWith('.') ? normalized : "." + normalized;
    }
}
