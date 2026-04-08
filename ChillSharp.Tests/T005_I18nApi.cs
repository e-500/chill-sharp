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

using Microsoft.EntityFrameworkCore;
using ChillSharp.I18n.Model;
using ChillSharp.I18n.Contracts;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using ChillSharp.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using ChillSharp.I18n.Api;

namespace ChillSharp.Tests;

[TestClass]
public sealed class I18nApi
{
    [TestMethod]
    public async Task Step001_SetTextAndGetTextUsesCacheUntilInvalidatedBySetText()
    {
        TestApiHost.EnsureStarted(6002);

        var labelGuid = Guid.NewGuid();

        using var client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:6002/")
        };

        var setResponse = await client.PutAsJsonAsync("api/chill-i18n/set-text", new SetTextRequest
        {
            LabelGuid = labelGuid,
            CultureName = "it-IT",
            Value = "Ciao mondo"
        });
        setResponse.EnsureSuccessStatusCode();

        var createdPayload = await setResponse.Content.ReadFromJsonAsync<GetTextResponse>();
        Assert.IsNotNull(createdPayload);
        Assert.AreEqual("Ciao mondo", createdPayload.Value);

        var firstGetResponse = await SendPostAsJsonAsync(client, "api/chill-i18n/get-text", new GetTextRequest
        {
            LabelGuid = labelGuid,
            CultureName = "it-IT"
        });
        firstGetResponse.EnsureSuccessStatusCode();
        var firstPayload = await firstGetResponse.Content.ReadFromJsonAsync<GetTextResponse>();
        Assert.IsNotNull(firstPayload);
        Assert.AreEqual("Ciao mondo", firstPayload.Value);

        await using (var context = TestApiHost.CreateDbContext())
        {
            var text = await context.Texts.FirstAsync(x => x.LabelGuid == labelGuid && x.CultureCode == "it-IT");
            text.Value = "Aggiornamento esterno";
            await context.SaveChangesAsync();
        }

        var cachedGetResponse = await SendPostAsJsonAsync(client, "api/chill-i18n/get-text", new GetTextRequest
        {
            LabelGuid = labelGuid,
            CultureName = "it-IT"
        });
        cachedGetResponse.EnsureSuccessStatusCode();
        var cachedPayload = await cachedGetResponse.Content.ReadFromJsonAsync<GetTextResponse>();
        Assert.IsNotNull(cachedPayload);
        Assert.AreEqual("Ciao mondo", cachedPayload.Value);

        var updateResponse = await client.PutAsJsonAsync("api/chill-i18n/set-text", new SetTextRequest
        {
            LabelGuid = labelGuid,
            CultureName = "it-IT",
            Value = "Ciao Italia"
        });
        updateResponse.EnsureSuccessStatusCode();

        var updatedPayload = await updateResponse.Content.ReadFromJsonAsync<GetTextResponse>();
        Assert.IsNotNull(updatedPayload);
        Assert.AreEqual("Ciao Italia", updatedPayload.Value);

        var refreshedGetResponse = await SendPostAsJsonAsync(client, "api/chill-i18n/get-text", new GetTextRequest
        {
            LabelGuid = labelGuid,
            CultureName = "it-IT"
        });
        refreshedGetResponse.EnsureSuccessStatusCode();
        var refreshedPayload = await refreshedGetResponse.Content.ReadFromJsonAsync<GetTextResponse>();
        Assert.IsNotNull(refreshedPayload);
        Assert.AreEqual("Ciao Italia", refreshedPayload.Value);
    }

    [TestMethod]
    public async Task Step002_MissingTranslationReturnsNotFound()
    {
        TestApiHost.EnsureStarted(6002);

        using var client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000/")
        };

        var response = await SendPostAsJsonAsync(client, "api/chill-i18n/get-text", new GetTextRequest
        {
            LabelGuid = Guid.NewGuid(),
            CultureName = "it-IT"
        });
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Step003_GetTextSeedsConfiguredPrimaryAndSecondaryCulturesWhenMissing()
    {
        TestApiHost.EnsureStarted(6002);

        using var client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:6002/")
        };

        var labelGuid = Guid.NewGuid();
        var response = await SendPostAsJsonAsync(client, "api/chill-i18n/get-text", new GetTextRequest
        {
            LabelGuid = labelGuid,
            CultureName = "en-GB",
            PrimaryCultureName = "en-GB",
            PrimaryDefaultText = "Hello",
            SecondaryCultureName = "it-IT",
            SecondaryDefaultText = "Ciao"
        });

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<GetTextResponse>();
        Assert.IsNotNull(payload);
        Assert.AreEqual("Hello", payload.Value);

        var secondaryResponse = await SendPostAsJsonAsync(client, "api/chill-i18n/get-text", new GetTextRequest
        {
            LabelGuid = labelGuid,
            CultureName = "it-IT"
        });
        secondaryResponse.EnsureSuccessStatusCode();

        var secondaryPayload = await secondaryResponse.Content.ReadFromJsonAsync<GetTextResponse>();
        Assert.IsNotNull(secondaryPayload);
        Assert.AreEqual("Ciao", secondaryPayload.Value);
    }

    [TestMethod]
    public async Task Step004_GetTextIgnoresSeedDefaultsWhenCulturesDoNotMatchServerConfig()
    {
        TestApiHost.EnsureStarted(6002);

        using var client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000/")
        };

        var labelGuid = Guid.NewGuid();
        var response = await SendPostAsJsonAsync(client, "api/chill-i18n/get-text", new GetTextRequest
        {
            LabelGuid = labelGuid,
            CultureName = "en-GB",
            PrimaryCultureName = "fr-FR",
            PrimaryDefaultText = "Bonjour",
            SecondaryCultureName = "de-DE",
            SecondaryDefaultText = "Hallo"
        });

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        await using var context = TestApiHost.CreateDbContext();
        Assert.IsFalse(await context.Texts.AnyAsync(x => x.LabelGuid == labelGuid));
    }

    [TestMethod]
    public async Task Step005_AnonymousGetTextReturnsDefaultWithoutPersistingWhenApiIsProtected()
    {
        AnonymousFriendlyI18nApiHost.EnsureStarted();

        using var client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5004/")
        };

        var labelGuid = Guid.NewGuid();
        var response = await SendPostAsJsonAsync(client, "api/chill-i18n/get-text", new GetTextRequest
        {
            LabelGuid = labelGuid,
            CultureName = "en-GB",
            PrimaryCultureName = "en-GB",
            PrimaryDefaultText = "Anonymous hello",
            SecondaryCultureName = "it-IT",
            SecondaryDefaultText = "Anonymous ciao"
        });

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<GetTextResponse>();
        Assert.IsNotNull(payload);
        Assert.AreEqual("Anonymous hello", payload.Value);

        await using var context = AnonymousFriendlyI18nApiHost.CreateDbContext();
        Assert.IsFalse(await context.Texts.AnyAsync(x => x.LabelGuid == labelGuid));

        var setResponse = await client.PutAsJsonAsync("api/chill-i18n/set-text", new SetTextRequest
        {
            LabelGuid = labelGuid,
            CultureName = "en-GB",
            Value = "Should fail"
        });
        Assert.IsTrue(setResponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task Step006_GetMultipleTextProcessesArrayOfRequests()
    {
        TestApiHost.EnsureStarted(6002);

        using var client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:6002/")
        };

        var existingLabelGuid = Guid.NewGuid();
        await client.PutAsJsonAsync("api/chill-i18n/set-text", new SetTextRequest
        {
            LabelGuid = existingLabelGuid,
            CultureName = "it-IT",
            Value = "Esistente"
        });

        var seededLabelGuid = Guid.NewGuid();
        var response = await SendPostAsJsonAsync(client, "api/chill-i18n/get-multiple-text", new[]
        {
            new GetTextRequest
            {
                LabelGuid = existingLabelGuid,
                CultureName = "it-IT"
            },
            new GetTextRequest
            {
                LabelGuid = seededLabelGuid,
                CultureName = "en-GB",
                PrimaryCultureName = "en-GB",
                PrimaryDefaultText = "Bulk hello",
                SecondaryCultureName = "it-IT",
                SecondaryDefaultText = "Bulk ciao"
            },
            new GetTextRequest
            {
                LabelGuid = Guid.NewGuid(),
                CultureName = "it-IT"
            }
        });

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<List<GetTextResponse?>>();
        Assert.IsNotNull(payload);
        Assert.HasCount(3, payload);
        Assert.IsNotNull(payload[0]);
        Assert.AreEqual("Esistente", payload[0]!.Value);
        Assert.IsNotNull(payload[1]);
        Assert.AreEqual("Bulk hello", payload[1]!.Value);
        Assert.IsNull(payload[2]);
    }

    [TestMethod]
    public void Step007_ClientLibrarySupportsI18nSingleAndBulkRequests()
    {
        TestApiHost.EnsureStarted(6002);

        var client = new ChillSharpClient("http://localhost:6002/api/chill", CultureName: "it-IT");
        var labelGuid = Guid.NewGuid();

        var stored = client.SetText(new SetTextRequest
        {
            LabelGuid = labelGuid,
            CultureName = "it-IT",
            Value = "Client text"
        });

        Assert.IsNotNull(stored);
        Assert.AreEqual("Client text", stored.Value);

        var single = client.GetText(new GetTextRequest
        {
            LabelGuid = labelGuid,
            CultureName = "it-IT"
        });

        Assert.IsNotNull(single);
        Assert.AreEqual("Client text", single!.Value);

        var seededLabelGuid = Guid.NewGuid();
        var bulk = client.GetTexts(new[]
        {
            new GetTextRequest
            {
                LabelGuid = labelGuid,
                CultureName = "it-IT"
            },
            new GetTextRequest
            {
                LabelGuid = seededLabelGuid,
                CultureName = "en-GB",
                PrimaryCultureName = "en-GB",
                PrimaryDefaultText = "Hello from client",
                SecondaryCultureName = "it-IT",
                SecondaryDefaultText = "Ciao dal client"
            },
            new GetTextRequest
            {
                LabelGuid = Guid.NewGuid(),
                CultureName = "it-IT"
            }
        });

        Assert.HasCount(3, bulk);
        Assert.IsNotNull(bulk[0]);
        Assert.AreEqual("Client text", bulk[0]!.Value);
        Assert.IsNotNull(bulk[1]);
        Assert.AreEqual("Hello from client", bulk[1]!.Value);
        Assert.IsNull(bulk[2]);
    }

    [TestMethod]
    public async Task Step008_SetTextRejectsEmptyLabelGuid()
    {
        TestApiHost.EnsureStarted(6002);

        using var client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:6002/")
        };

        var response = await client.PutAsJsonAsync("api/chill-i18n/set-text", new SetTextRequest
        {
            LabelGuid = Guid.Empty,
            CultureName = "it-IT",
            Value = "Invalid"
        });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static Task<HttpResponseMessage> SendPostAsJsonAsync<T>(HttpClient client, string requestUri, T payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(payload)
        };

        return client.SendAsync(request);
    }

    //private static Task<HttpResponseMessage> SendGetAsJsonAsync<T>(HttpClient client, string requestUri, T payload)
    //{
    //    var request = new HttpRequestMessage(HttpMethod.Get, requestUri)
    //    {
    //        Content = JsonContent.Create(payload)
    //    };

    //    return client.SendAsync(request);
    //}

    private static class AnonymousFriendlyI18nApiHost
    {
        private static readonly object SyncRoot = new();
        private static readonly string DatabasePath = Path.Combine(Path.GetTempPath(), "ChillSharpTestContext", "anonymous-friendly-i18n-api-host.db");
        private static bool _apiServiceUpAndRunning;

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
                    builder.WebHost.UseUrls("http://localhost:5004");
                    builder.Services.AddDbContext<EF.DummyContext>(options =>
                        options.UseSqlite($"Data Source={DatabasePath}"));
                    builder.Services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestHeaderAuthenticationHandler>("Test", _ => { });
                    builder.Services.AddAuthorization();
                    builder.Services.AddChillI18nApi<EF.DummyContext>();

                    var app = builder.Build();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.MapControllers().RequireAuthorization();
                    app.Run();
                });

                apiServer.Wait(5000);
                _apiServiceUpAndRunning = true;
            }
        }

        public static EF.DummyContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<EF.DummyContext>()
                .UseSqlite($"Data Source={DatabasePath}")
                .Options;
            return new EF.DummyContext(options);
        }
    }

    private sealed class TestHeaderAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestHeaderAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder) : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-User", out var userId) || string.IsNullOrWhiteSpace(userId))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
