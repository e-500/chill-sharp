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
        // Set also culture name bindings
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

        public string GetPrimaryCultureName()
        {
            return "en-GB"; // We prefer DD/MM/YYYY date format
        }

        public string GetSecondaryCultureName()
        {
            return "it-IT"; // We are italian
        }

        public string GetCurrentUserName()
        {
            return "dummy-user";
        }
    }
}
