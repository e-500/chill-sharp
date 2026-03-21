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
using ChillSharp.Auth.Services;
using ChillSharp.I18n.Api;
using ChillSharp.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Examples.BloggingApiService;

internal static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var configuration = builder.Configuration;

        var databasePath = configuration["CHILLSHARP_DB_PATH"] ?? "/data/blogging.db";
        var enableSchema = GetBoolean(configuration, "CHILLSHARP_ENABLE_SCHEMA", true);
        var enableAuth = GetBoolean(configuration, "CHILLSHARP_ENABLE_AUTH", true);
        var enableI18n = GetBoolean(configuration, "CHILLSHARP_ENABLE_I18N", true);
        var protectedApi = GetBoolean(configuration, "CHILLSHARP_API_PROTECTED", enableAuth);

        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

        builder.Services.AddDbContext<BloggingContext>(options =>
            options.UseSqlite($"Data Source={databasePath}"));

        if (enableAuth)
        {
            builder.Services.AddIdentityCore<IdentityUser>()
                .AddEntityFrameworkStores<BloggingContext>()
                .AddSignInManager()
                .AddDefaultTokenProviders();

            builder.Services.AddAuthentication(ChillAuthIdentityDefaults.AuthenticationScheme)
                .AddChillAuthBearer();
            builder.Services.AddAuthorization();
        }

        builder.Services.AddChillApi<BloggingContext>(options =>
        {
            options.ProtectedApi = protectedApi;
        });

        if (enableSchema)
        {
            builder.Services.AddChillSchema<BloggingContext>();
        }

        if (enableAuth)
        {
            builder.Services.AddChillAuthIdentityApi<BloggingContext, IdentityUser>(options =>
            {
                options.ReturnPasswordResetTokensInResponse = GetBoolean(
                    configuration,
                    "CHILLSHARP_AUTH_RETURN_PASSWORD_RESET_TOKENS",
                    false);
                options.SendPasswordResetEmails = GetBoolean(
                    configuration,
                    "CHILLSHARP_AUTH_SEND_PASSWORD_RESET_EMAILS",
                    false);
                options.InitializeRootUserOnStartup = GetBoolean(
                    configuration,
                    "CHILLSHARP_AUTH_INITIALIZE_ROOT_USER",
                    true);
                options.CreateChillAuthUserForRoot = GetBoolean(
                    configuration,
                    "CHILLSHARP_AUTH_CREATE_ROOT_AUTH_USER",
                    true);
                options.AccessTokenLifetime = TimeSpan.FromMinutes(GetInteger(
                    configuration,
                    "CHILLSHARP_AUTH_ACCESS_TOKEN_MINUTES",
                    20));
                options.RefreshTokenLifetime = TimeSpan.FromDays(GetInteger(
                    configuration,
                    "CHILLSHARP_AUTH_REFRESH_TOKEN_DAYS",
                    14));
                options.SmtpHost = configuration["CHILLSHARP_SMTP_HOST"];
                options.SmtpPort = GetInteger(configuration, "CHILLSHARP_SMTP_PORT", 587);
                options.SmtpEnableSsl = GetBoolean(configuration, "CHILLSHARP_SMTP_ENABLE_SSL", true);
                options.SmtpUserName = configuration["CHILLSHARP_SMTP_USERNAME"];
                options.SmtpPassword = configuration["CHILLSHARP_SMTP_PASSWORD"];
                options.PasswordResetFromEmail = configuration["CHILLSHARP_SMTP_FROM_EMAIL"];
                options.PasswordResetFromDisplayName = configuration["CHILLSHARP_SMTP_FROM_DISPLAY_NAME"];
                options.PasswordResetEmailSubject = configuration["CHILLSHARP_AUTH_PASSWORD_RESET_SUBJECT"]
                    ?? "Reset your password";
                options.PasswordResetUrlBase = configuration["CHILLSHARP_AUTH_PASSWORD_RESET_URL"];
            });
        }

        if (enableI18n)
        {
            builder.Services.AddChillI18nApi<BloggingContext>();
        }

        var app = builder.Build();

        if (enableAuth)
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BloggingContext>();
            db.Database.EnsureCreated();
        }

        app.MapChillApi();
        app.MapGet("/", () => new
        {
            Service = "ChillSharp.Examples.BloggingApiService",
            DatabasePath = databasePath,
            Modules = new
            {
                Api = true,
                Schema = enableSchema,
                Auth = enableAuth,
                I18n = enableI18n,
                ProtectedApi = protectedApi
            }
        });

        app.Run();
    }

    private static bool GetBoolean(IConfiguration configuration, string key, bool fallback)
    {
        return bool.TryParse(configuration[key], out var value) ? value : fallback;
    }

    private static int GetInteger(IConfiguration configuration, string key, int fallback)
    {
        return int.TryParse(configuration[key], out var value) ? value : fallback;
    }
}
