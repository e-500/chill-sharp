using ChillSharp.Schema;
using ChillSharp.Schema.Model;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Tests.EF
{
    public partial class DummyContext : IChillSchemaDbContext
    {
        public DbSet<ChillSchemaEntry> SchemaEntries { get; set; }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
        {
            modelBuilder.AddChillSchemaModel();
        }
    }
}
