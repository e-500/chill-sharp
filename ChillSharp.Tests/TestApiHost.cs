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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ChillSharp.Tests;

internal static class TestApiHost
{
    private static readonly object SyncRoot = new();
    private static readonly string DatabasePath = Path.Combine(Path.GetTempPath(), "ChillSharpTestContext", "test-api-host.db");
    private static readonly string HttpsDatabasePath = Path.Combine(Path.GetTempPath(), "ChillSharpTestContext", "test-api-host-https.db");
    private static readonly X509Certificate2 HttpsCertificate = CreateHttpsCertificate();
    private static bool _apiServiceUpAndRunning;
    private static bool _httpsApiServiceUpAndRunning;

    public const string HttpBaseUrl = "http://localhost:6002/";
    public const string HttpsBaseUrl = "https://localhost:5002/";

    public static EF.DummyContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EF.DummyContext>()
            .UseSqlite($"Data Source={DatabasePath}")
            .Options;
        return new EF.DummyContext(options);
    }

    public static EF.DummyContext CreateHttpsDbContext()
    {
        var options = new DbContextOptionsBuilder<EF.DummyContext>()
            .UseSqlite($"Data Source={HttpsDatabasePath}")
            .Options;
        return new EF.DummyContext(options);
    }

    public static void EnsureStarted(int HttpPort = 5000)
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
                builder.WebHost.UseUrls($"http://localhost:{HttpPort}");
                builder.Services.AddDbContext<EF.DummyContext>(options =>
                    options.UseSqlite($"Data Source={DatabasePath}"));
                builder.Services.AddChillApi<EF.DummyContext>();

                var app = builder.Build();
                app.MapChillApi();
                app.Run();
            });

            apiServer.Wait(5000);
            _apiServiceUpAndRunning = true;
        }
    }

    public static void EnsureHttpsStarted(int HttpsPort = 5002)
    {
        if (_httpsApiServiceUpAndRunning)
        {
            return;
        }

        lock (SyncRoot)
        {
            if (_httpsApiServiceUpAndRunning)
            {
                return;
            }

            var apiServer = Task.Run(() =>
            {
                Directory.CreateDirectory(Path.GetDirectoryName(HttpsDatabasePath)!);
                var ctx = CreateHttpsDbContext();
                ctx.Database.EnsureDeleted();
                ctx.Database.EnsureCreated();

                var builder = WebApplication.CreateBuilder(Array.Empty<string>());
                builder.WebHost.ConfigureKestrel(options =>
                {
                    options.ListenLocalhost(HttpsPort, listenOptions => listenOptions.UseHttps(HttpsCertificate));
                });
                builder.Services.AddDbContext<EF.DummyContext>(options =>
                    options.UseSqlite($"Data Source={HttpsDatabasePath}"));
                builder.Services.AddChillApi<EF.DummyContext>();

                var app = builder.Build();
                app.MapChillApi();
                app.Run();
            });

            apiServer.Wait(5000);
            _httpsApiServiceUpAndRunning = true;
        }
    }

    private static X509Certificate2 CreateHttpsCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=localhost",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                new OidCollection
                {
                    new("1.3.6.1.5.5.7.3.1")
                },
                critical: false));
        request.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("localhost");
        sanBuilder.AddIpAddress(System.Net.IPAddress.Loopback);
        sanBuilder.AddIpAddress(System.Net.IPAddress.IPv6Loopback);
        request.CertificateExtensions.Add(sanBuilder.Build());

        var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(5));
        return new X509Certificate2(
            certificate.Export(X509ContentType.Pfx),
            string.Empty,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);
    }
}
