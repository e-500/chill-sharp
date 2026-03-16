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

using ChillSharp.Api;
using ChillSharp.Auth.Api;
using ChillSharp.I18n.Api;
using ChillSharp.Schema;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ChillSharp.Tests;

internal static class TestApiHost
{
    private static readonly object SyncRoot = new();
    private static readonly string DatabasePath = Path.Combine(Path.GetTempPath(), "ChillSharpTestContext", "test-api-host.db");
    private static bool _apiServiceUpAndRunning;

    public static EF.DummyContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EF.DummyContext>()
            .UseSqlite($"Data Source={DatabasePath}")
            .Options;
        return new EF.DummyContext(options);
    }

    public static void EnsureStarted()
    {
        if (_apiServiceUpAndRunning)
        {
            return;
        }

        lock (SyncRoot)
        {
            if (_apiServiceUpAndRunning)
            {
                return;
            }

            var apiServer = Task.Run(() =>
            {
                Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
                var ctx = CreateDbContext();
                ctx.Database.EnsureDeleted();
                ctx.Database.EnsureCreated();

                var builder = WebApplication.CreateBuilder(Array.Empty<string>());
                builder.Services.AddDbContext<EF.DummyContext>(options =>
                    options.UseSqlite($"Data Source={DatabasePath}"));
                builder.Services.AddChillApi<EF.DummyContext>();
                builder.Services.AddChillAuthApi<EF.DummyContext>();
                builder.Services.AddChillI18nApi<EF.DummyContext>();
                builder.Services.AddChillSchema<EF.DummyContext>();

                var app = builder.Build();
                app.MapChillApi();
                app.Run();
            });

            apiServer.Wait(5000);
            _apiServiceUpAndRunning = true;
        }
    }
}
