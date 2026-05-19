using ChillSharp.Attachment;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Tests.EF
{
    public partial class DummyContext : IChillAttachmentDbContext
    {
        public DbSet<ChillSharp.Attachment.Model.FileMetadata> Attachments { get; set; }
    }
}
