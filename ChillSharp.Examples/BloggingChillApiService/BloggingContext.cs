/*
 * Author: Andrea Piovesan
 * Year: 2025
 * License: GNU General Public License (GPL)
 *
 * Disclaimer:
 * You are free to use, modify, and distribute it under the terms of the GPL license.
 * This code comes with no warranty; use it at your own risk.
 * 
 * For further information, please refer to README and LICENSE files.
 */

using ChillSharp.Examples.CustomChillApiService.Model;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Examples.CustomChillApiService
{
    public class BloggingContext : DbContext, IChillContext
    {
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Post> Posts { get; set; }

        public string DbPath { get; }

        public BloggingContext()
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = System.IO.Path.Join(path, "ChillSharp.Examples.CustomChillApiService");
            Directory.CreateDirectory(DbPath);
            DbPath = System.IO.Path.Join(path, "ChillSharp.Examples.CustomChillApiService", "blogging.db");
        }

        public BloggingContext(DbContextOptions<BloggingContext> options) : base(options) 
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = System.IO.Path.Join(path, "ChillSharp.Examples.CustomChillApiService");
            Directory.CreateDirectory(DbPath);
            DbPath = System.IO.Path.Join(path, "ChillSharp.Examples.CustomChillApiService", "blogging.db");
        }

        // The following configures EF to create a Sqlite database file in the
        // special "local" folder for your platform.
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        { 
            //SetInitializer(new MigrateDatabaseToLatestVersion<MyContext, MigrateDBConfiguration>());
            options.UseSqlite($"Data Source={DbPath}");
        }

        public string GetChillTypePrefix()
        {
            return "ChillSharp.Examples.CustomChillApiService";
        }
    }
}
