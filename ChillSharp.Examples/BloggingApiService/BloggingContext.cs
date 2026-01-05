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

using ChillSharp.Examples.BloggingApiService.Model;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Examples.BloggingApiService
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
            DbPath = System.IO.Path.Join(path, "ChillSharp.Examples.BloggingApiService");
            Directory.CreateDirectory(DbPath);
            DbPath = System.IO.Path.Join(path, "ChillSharp.Examples.BloggingApiService", "blogging.db");
        }

        public BloggingContext(DbContextOptions<BloggingContext> options) : base(options) 
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = System.IO.Path.Join(path, "ChillSharp.Examples.BloggingApiService");
            Directory.CreateDirectory(DbPath);
            DbPath = System.IO.Path.Join(path, "ChillSharp.Examples.BloggingApiService", "blogging.db");
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
            return "ChillSharp.Examples.BloggingApiService";
        }
    }
}
