using ChillSharp.Schema;
using ChillSharp.Schema.Model;
using ChillSharp.I18n;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Tests.EF
{
    public partial class DummyContext : IChillSchemaDbContext
    {
        public DbSet<ChillSchemaEntry> SchemaEntries { get; set; }

        public DbSet<ChillEntityOptionsEntry> EntityOptionsEntries { get; set; }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
        {
            modelBuilder.AddChillSchemaModel();
            modelBuilder.AddChillI18nModel();
        }
    }
}
