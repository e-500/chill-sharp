using ChillSharp.Tests.EF.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Tests.EF
{
    public partial class DummyContext : IdentityDbContext<IdentityUser>, IChillContext
    {
        public string DbPath { get; }

        public DbSet<Post> Post { get; set; }
        public DbSet<Blog> Blog { get; set; }

        public DummyContext()
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = Path.Join(path, "ChillSharpTestContext");
            Directory.CreateDirectory(DbPath);
            DbPath = Path.Join(path, "ChillSharpTestContext", "test.db");
        }

        public DummyContext(DbContextOptions<DummyContext> options) : base(options)
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = Path.Join(path, "ChillSharpTestContext");
            Directory.CreateDirectory(DbPath);
            DbPath = Path.Join(path, "ChillSharpTestContext", "test.db");
        }

        // The following configures EF to create a Sqlite database file in the
        // special "local" folder for your platform.
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!options.IsConfigured)
            {
                options.UseSqlite($"Data Source={DbPath}");
            }
        }

        public string GetChillTypePrefix()
        {
            return "ChillSharp.Tests.EF";
        }
    }
}
