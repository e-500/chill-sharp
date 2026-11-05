using ChillSharp.Attachment.Model;
using ChillSharp.Attachment.Services;
using Microsoft.Extensions.Options;

namespace ChillSharp.Test;

[TestClass]
public sealed class AttachmentArchiveTests
{
    [TestMethod]
    public async Task SaveOpenExistsAndDelete_RoundTripsFile()
    {
        var root = Path.Combine(Path.GetTempPath(), $"chill-archive-{Guid.NewGuid():N}");
        var archive = new ChillAttachmentArchive(Options.Create(new ChillAttachmentOptions { ArchiveRoot = root }));
        var metadata = new FileMetadata
        {
            Guid = Guid.NewGuid(),
            AttachToChillType = "Post",
            Extension = " txt ",
            CreatedAtUtc = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        };

        try
        {
            Assert.IsFalse(archive.Exists(metadata));

            await archive.SaveAsync(metadata, new MemoryStream("archive content"u8.ToArray()));

            Assert.IsTrue(archive.Exists(metadata));
            using (var stream = archive.OpenRead(metadata))
            using (var reader = new StreamReader(stream))
            {
                Assert.AreEqual("archive content", await reader.ReadToEndAsync());
            }

            archive.DeleteIfExists(metadata);
            Assert.IsFalse(archive.Exists(metadata));
            archive.DeleteIfExists(metadata);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [TestMethod]
    public async Task BuildPathAndSave_RejectNullArguments()
    {
        var archive = new ChillAttachmentArchive(Options.Create(new ChillAttachmentOptions
        {
            ArchiveRoot = Path.GetTempPath()
        }));

        Assert.Throws<ArgumentNullException>(() => archive.BuildPath(null!));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => archive.SaveAsync(new FileMetadata(), null!));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => archive.SaveAsync(null!, Stream.Null));
    }

    [TestMethod]
    public void BuildAttachmentPath_SanitizesSegmentsAndNormalizesExtension()
    {
        var id = Guid.Parse("12345678-9abc-def0-1234-56789abcdef0");

        var path = ChillAttachmentArchive.BuildAttachmentPath(
            Path.GetTempPath(),
            "folder\\Post",
            id,
            " .txt ",
            new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));

        Assert.IsTrue(path.Contains($"folder_Post{Path.DirectorySeparatorChar}2026{Path.DirectorySeparatorChar}12{Path.DirectorySeparatorChar}34", StringComparison.Ordinal));
        Assert.IsTrue(path.EndsWith($"{id:N}.txt", StringComparison.Ordinal));
        Assert.IsTrue(ChillAttachmentArchive.BuildAttachmentPath(
            Path.GetTempPath(), " ", Guid.NewGuid(), " ", DateTime.UtcNow)
            .Contains($"{Path.DirectorySeparatorChar}general{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
        Assert.IsTrue(ChillAttachmentArchive.BuildAttachmentPath(
            " ", "Post", Guid.NewGuid(), null!, DateTime.UtcNow)
            .StartsWith(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "attachments")), StringComparison.Ordinal));
    }
}
