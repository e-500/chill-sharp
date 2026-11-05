using ChillSharp.Attachment;
using ChillSharp.Attachment.Model;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Test;

[TestClass]
public class FileMetadataQueryTests
{
    [TestMethod]
    public async Task OnQuery_FiltersByTrimmedTypeAndNonEmptyGuid()
    {
        var selectedGuid = Guid.NewGuid();
        await using var context = CreateContext();
        context.Attachments.AddRange(
            new FileMetadata { Guid = Guid.NewGuid(), AttachToChillType = "Post", AttachToGuid = selectedGuid },
            new FileMetadata { Guid = Guid.NewGuid(), AttachToChillType = "Post" },
            new FileMetadata { Guid = Guid.NewGuid(), AttachToChillType = "Blog" });
        await context.SaveChangesAsync();
        Assert.AreEqual(3, new FileMetadataQuery().OnQuery(context).Count());

        var results = new FileMetadataQuery
        {
            AttachToChillType = " Post ",
            AttachToGuid = selectedGuid
        }.OnQuery(context).ToArray();

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(selectedGuid, ((FileMetadata)results[0]).AttachToGuid);
    }

    [TestMethod]
    public async Task OnQuery_IgnoresBlankTypeAndEmptyGuid()
    {
        await using var context = CreateContext();
        context.Attachments.AddRange(
            new FileMetadata { Guid = Guid.NewGuid(), AttachToChillType = "Post" },
            new FileMetadata { Guid = Guid.NewGuid(), AttachToChillType = "Blog" });
        await context.SaveChangesAsync();

        var results = new FileMetadataQuery
        {
            AttachToChillType = "  ",
            AttachToGuid = Guid.Empty
        }.OnQuery(context).ToArray();

        Assert.AreEqual(2, results.Length);
    }

    [TestMethod]
    public void OnOrderingBy_SortsNewestFirstThenGuid()
    {
        var firstGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var secondGuid = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var time = DateTime.UtcNow;
        IQueryable<ChillSharp.EF.IChillEntity> entities = new ChillSharp.EF.IChillEntity[]
        {
            new FileMetadata { Guid = secondGuid, CreatedAtUtc = time },
            new FileMetadata { Guid = Guid.NewGuid(), CreatedAtUtc = time.AddMinutes(-1) },
            new FileMetadata { Guid = firstGuid, CreatedAtUtc = time }
        }.AsQueryable();

        var results = new FileMetadataQuery().OnOrderingBy(null!, entities).ToArray();

        Assert.AreEqual(firstGuid, results[0].Guid);
        Assert.AreEqual(secondGuid, results[1].Guid);
    }

    private static ChillAttachmentDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ChillAttachmentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ChillAttachmentDbContext(options);
    }
}
