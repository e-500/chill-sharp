using ChillSharp.Attachment;
using ChillSharp.Attachment.Model;
using ChillSharp.Attachment.Services;
using ChillSharp.Tests.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ChillSharp.Test;

[TestClass]
public class FileMetadataTests
{
    [TestMethod]
    public void OnCreate_InitializesMissingIdentityAndTimestamp()
    {
        var metadata = new FileMetadata();

        metadata.OnCreate(new DummyContext());

        Assert.AreNotEqual(Guid.Empty, metadata.Guid);
        Assert.IsTrue(metadata.CreatedAtUtc > DateTime.UtcNow.AddMinutes(-1));
        Assert.IsTrue(metadata.CreatedAtUtc <= DateTime.UtcNow);
    }

    [TestMethod]
    public void OnCreate_PreservesExistingIdentityAndTimestamp()
    {
        var guid = Guid.NewGuid();
        var createdAt = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var metadata = new FileMetadata { Guid = guid, CreatedAtUtc = createdAt };

        metadata.OnCreate(new DummyContext());

        Assert.AreEqual(guid, metadata.Guid);
        Assert.AreEqual(createdAt, metadata.CreatedAtUtc);
    }

    [TestMethod]
    public void OnDelete_UsesRegisteredArchiveService()
    {
        var archive = new RecordingArchive();
        var services = new ServiceCollection();
        services.AddEntityFrameworkInMemoryDatabase();
        services.AddSingleton<IChillAttachmentArchive>(archive);
        using var provider = services.BuildServiceProvider();
        var options = new DbContextOptionsBuilder<ChillAttachmentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseInternalServiceProvider(provider)
            .Options;
        using var context = new ChillAttachmentDbContext(options);
        var metadata = new FileMetadata();

        metadata.OnDelete(context);

        Assert.AreSame(metadata, archive.Deleted);
    }

    [TestMethod]
    public void OnDelete_DoesNotPropagateArchiveErrors()
    {
        var archive = new RecordingArchive { ThrowOnDelete = true };
        var services = new ServiceCollection();
        services.AddEntityFrameworkInMemoryDatabase();
        services.AddSingleton<IChillAttachmentArchive>(archive);
        using var provider = services.BuildServiceProvider();
        var options = new DbContextOptionsBuilder<ChillAttachmentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseInternalServiceProvider(provider)
            .Options;
        using var context = new ChillAttachmentDbContext(options);

        new FileMetadata().OnDelete(context);

        Assert.IsTrue(archive.DeleteAttempted);
    }

    [TestMethod]
    public void OnDelete_IgnoresNonEntityFrameworkContexts()
    {
        new FileMetadata().OnDelete(new NonDbContext());
    }

    private sealed class RecordingArchive : IChillAttachmentArchive
    {
        public FileMetadata? Deleted { get; private set; }
        public bool DeleteAttempted { get; private set; }
        public bool ThrowOnDelete { get; init; }

        public string BuildPath(FileMetadata attachment) => string.Empty;
        public Task SaveAsync(FileMetadata attachment, Stream content, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Stream OpenRead(FileMetadata attachment) => Stream.Null;
        public bool Exists(FileMetadata attachment) => false;

        public void DeleteIfExists(FileMetadata attachment)
        {
            DeleteAttempted = true;
            if (ThrowOnDelete)
            {
                throw new IOException("Test archive failure.");
            }

            Deleted = attachment;
        }
    }

    private sealed class NonDbContext : ChillSharp.IChillContext
    {
        public string GetChillTypePrefix() => string.Empty;
    }
}
