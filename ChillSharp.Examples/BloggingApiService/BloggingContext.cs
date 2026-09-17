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

using ChillSharp.Auth;
using ChillSharp.Auth.Model;
using ChillSharp.EF.ServiceModel.I18n;
using ChillSharp.Examples.BloggingApiService.Model;
using ChillSharp.I18n;
using ChillSharp.Schema;
using ChillSharp.Schema.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Examples.BloggingApiService;

public class BloggingContext : IdentityDbContext<IdentityUser>, IChillContext, IChillAuthDbContext, IChillSchemaDbContext, IChillI18nDbContext
{
    private readonly IConfiguration? _configuration;

    public BloggingContext()
    {
    }

    public BloggingContext(DbContextOptions<BloggingContext> options, IConfiguration configuration) : base(options)
    {
        _configuration = configuration;
    }

    public DbSet<Blog> Blogs => Set<Blog>();

    public DbSet<Post> Posts => Set<Post>();

    public new DbSet<AuthUser> Users => Set<AuthUser>();

    public new DbSet<AuthRole> Roles => Set<AuthRole>();

    public new DbSet<AuthUserRole> UserRoles => Set<AuthUserRole>();

    public DbSet<AuthPermissionRule> PermissionRules => Set<AuthPermissionRule>();

    public DbSet<AuthRefreshToken> RefreshTokens => Set<AuthRefreshToken>();

    public DbSet<ChillSchemaEntry> SchemaEntries => Set<ChillSchemaEntry>();

    public DbSet<Text> Texts => Set<Text>();

    public string GetChillTypePrefix()
    {
        return "ChillSharp.Examples.BloggingApiService";
    }

    public string GetPrimaryCultureName()
    {
        return _configuration?["CHILLSHARP_PRIMARY_CULTURE"] ?? "en-GB";
    }

    public string GetSecondaryCultureName()
    {
        return _configuration?["CHILLSHARP_SECONDARY_CULTURE"] ?? "it-IT";
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            return;
        }

        var databasePath = _configuration?["CHILLSHARP_DB_PATH"];
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            databasePath = Path.Combine(localAppData, "ChillSharp.Examples.BloggingApiService", "blogging.db");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        optionsBuilder.UseSqlite($"Data Source={databasePath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddChillAuthModel();
        modelBuilder.AddChillSchemaModel();
        modelBuilder.AddChillI18nModel();
    }
}
