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

using System.Collections.Concurrent;
using ChillSharp.Api;
using ChillSharp.Auth.Api;
using ChillSharp.Auth.Contracts;
using ChillSharp.Client;
using ChillSharp.Client.Dto;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChillSharp.Tests
{
    [TestClass]
    [DoNotParallelize]
    public sealed class SignalRNotifications
    {
        [TestMethod]
        public async Task Step001_TypeSubscriptionReceivesCreateUpdateDeleteNotifications()
        {
            TestApiHost.EnsureStarted(6002);

            var connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:6002/api/chill/notify")
                .Build();

            var receivedChanges = new ConcurrentQueue<ChillEntityChangeNotification[]>();
            var notificationSignal = new SemaphoreSlim(0);

            connection.On<ChillEntityChangeNotification[]>(
                ChillEntityChangeHub.NotificationMethodName,
                changes =>
                {
                    receivedChanges.Enqueue(changes);
                    notificationSignal.Release();
                });

            await connection.StartAsync();
            await connection.InvokeAsync("Register", "Model.Post", (Guid?)null);

            try
            {
                var client = new ChillSharpClient("http://localhost:6002/api/chill");
                var postGuid = Guid.NewGuid();

                var createEntity = new ChillDtoEntity
                {
                    ChillType = "Model.Post",
                    Guid = postGuid
                };
                createEntity.Properties.Add("Title", "SignalR create");
                createEntity.Properties.Add("Author", "SignalR create");
                var createdEntity = client.Create(createEntity);
                postGuid = createdEntity.Guid;

                var createChanges = await WaitForNotificationAsync(receivedChanges, notificationSignal);
                CollectionAssert.AreEqual(new[] { "Model.Post" }, createChanges.Select(x => x.ChillType).ToArray());
                CollectionAssert.AreEqual(new[] { postGuid }, createChanges.Select(x => x.Guid).ToArray());
                CollectionAssert.AreEqual(new[] { ChillEntityChangeNotification.CreatedAction }, createChanges.Select(x => x.Action).ToArray());

                var updateEntity = new ChillDtoEntity
                {
                    ChillType = "Model.Post",
                    Guid = postGuid
                };
                updateEntity.Properties.Add("Title", "SignalR update");
                client.Update(updateEntity);

                var updateChanges = await WaitForNotificationAsync(receivedChanges, notificationSignal);
                CollectionAssert.AreEqual(new[] { "Model.Post" }, updateChanges.Select(x => x.ChillType).ToArray());
                CollectionAssert.AreEqual(new[] { postGuid }, updateChanges.Select(x => x.Guid).ToArray());
                CollectionAssert.AreEqual(new[] { ChillEntityChangeNotification.UpdatedAction }, updateChanges.Select(x => x.Action).ToArray());

                client.Delete(new ChillDtoEntity
                {
                    ChillType = "Model.Post",
                    Guid = postGuid
                });

                var deleteChanges = await WaitForNotificationAsync(receivedChanges, notificationSignal);
                CollectionAssert.AreEqual(new[] { "Model.Post" }, deleteChanges.Select(x => x.ChillType).ToArray());
                CollectionAssert.AreEqual(new[] { postGuid }, deleteChanges.Select(x => x.Guid).ToArray());
                CollectionAssert.AreEqual(new[] { ChillEntityChangeNotification.DeletedAction }, deleteChanges.Select(x => x.Action).ToArray());
            }
            finally
            {
                await connection.InvokeAsync("Unregister", "Model.Post", (Guid?)null);
                await connection.DisposeAsync();
            }
        }

        [TestMethod]
        public async Task Step002_EntitySubscriptionOnlyReceivesTheRegisteredEntity()
        {
            TestApiHost.EnsureStarted(6002);

            var connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:6002/api/chill/notify")
                .Build();

            var receivedChanges = new ConcurrentQueue<ChillEntityChangeNotification[]>();
            var notificationSignal = new SemaphoreSlim(0);

            connection.On<ChillEntityChangeNotification[]>(
                ChillEntityChangeHub.NotificationMethodName,
                changes =>
                {
                    receivedChanges.Enqueue(changes);
                    notificationSignal.Release();
                });

            await connection.StartAsync();

            try
            {
                var client = new ChillSharpClient("http://localhost:6002/api/chill");
                var targetPost = new ChillDtoEntity
                {
                    ChillType = "Model.Post",
                    Guid = Guid.NewGuid()
                };
                targetPost.Properties.Add("Title", "Target");
                targetPost.Properties.Add("Author", "Target");
                var createdTargetPost = client.Create(targetPost);

                var subscribedGuid = createdTargetPost.Guid;
                await connection.InvokeAsync("Register", "Model.Post", subscribedGuid);

                var otherPost = new ChillDtoEntity
                {
                    ChillType = "Model.Post",
                    Guid = Guid.NewGuid()
                };
                otherPost.Properties.Add("Title", "Other");
                otherPost.Properties.Add("Author", "Other");
                client.Create(otherPost);

                Assert.IsFalse(await TryWaitForNotificationAsync(receivedChanges, notificationSignal, 500));

                var subscribedPost = new ChillDtoEntity
                {
                    ChillType = "Model.Post",
                    Guid = subscribedGuid
                };
                subscribedPost.Properties.Add("Title", "Target updated");
                client.Update(subscribedPost);

                var createChanges = await WaitForNotificationAsync(receivedChanges, notificationSignal);
                CollectionAssert.AreEqual(new[] { "Model.Post" }, createChanges.Select(x => x.ChillType).ToArray());
                CollectionAssert.AreEqual(new[] { subscribedGuid }, createChanges.Select(x => x.Guid).ToArray());
                CollectionAssert.AreEqual(new[] { ChillEntityChangeNotification.UpdatedAction }, createChanges.Select(x => x.Action).ToArray());
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        [TestMethod]
        public async Task Step003_ProtectedHubRequiresBearerAuthenticationAndAcceptsSignalRAccessToken()
        {
            ProtectedSignalRAuthApiHost.EnsureHttpStarted(6002);

            var anonymousConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:6002/api/chill/notify")
                .Build();

            try
            {
                await anonymousConnection.StartAsync();
                Assert.Fail("Anonymous SignalR connection should have been rejected.");
            }
            catch (HttpRequestException)
            {
            }
            finally
            {
                await anonymousConnection.DisposeAsync();
            }

            var client = new ChillSharpClient("http://localhost:6002/api/chill");
            var registerResponse = client.RegisterAuthAccount(new RegisterAuthIdentityRequest
            {
                UserName = $"signalr.user.{Guid.NewGuid():N}",
                Email = $"signalr.{Guid.NewGuid():N}@test.local",
                Password = "Pass123$",
                DisplayName = "SignalR User",
                DisplayCultureName = "it-IT",
                CreateChillAuthUser = true
            });

            var authenticatedConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:6002/api/chill/notify", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(registerResponse.AccessToken);
                })
                .Build();

            await authenticatedConnection.StartAsync();

            try
            {
                await authenticatedConnection.InvokeAsync("Register", "Model.Post", (Guid?)null);
            }
            finally
            {
                await authenticatedConnection.DisposeAsync();
            }
        }

        private static async Task<ChillEntityChangeNotification[]> WaitForNotificationAsync(
            ConcurrentQueue<ChillEntityChangeNotification[]> receivedChanges,
            SemaphoreSlim notificationSignal,
            int timeoutMs = 5000)
        {
            var hasNotification = await notificationSignal.WaitAsync(timeoutMs);
            Assert.IsTrue(hasNotification, "Expected a SignalR notification but none was received.");
            Assert.IsTrue(receivedChanges.TryDequeue(out var changes));
            Assert.IsNotNull(changes);
            return changes;
        }

        private static async Task<bool> TryWaitForNotificationAsync(
            ConcurrentQueue<ChillEntityChangeNotification[]> receivedChanges,
            SemaphoreSlim notificationSignal,
            int timeoutMs)
        {
            var hasNotification = await notificationSignal.WaitAsync(timeoutMs);
            if (!hasNotification)
                return false;

            receivedChanges.TryDequeue(out _);
            return true;
        }

        private static class ProtectedSignalRAuthApiHost
        {
            private static readonly object SyncRoot = new();
            private static readonly string DatabasePath = Path.Combine(Path.GetTempPath(), "ChillSharpTestContext", "protected-signalr-auth-api-host.db");
            private static bool _apiServiceUpAndRunning;

            public static void EnsureHttpStarted(int HttpPort = 5002)
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
                        builder.Logging.ClearProviders();
                        builder.Services.AddDbContext<EF.DummyContext>(options =>
                            options.UseSqlite($"Data Source={DatabasePath}"));
                        builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
                        builder.Services.AddIdentityCore<IdentityUser>()
                            .AddEntityFrameworkStores<EF.DummyContext>()
                            .AddSignInManager()
                            .AddDefaultTokenProviders();
                        builder.Services.AddAuthentication(ChillAuthIdentityDefaults.AuthenticationScheme)
                            .AddChillAuthBearer();
                        builder.Services.AddAuthorization();
                        builder.Services.AddChillApi<EF.DummyContext>(options => options.ProtectedApi = true);
                        builder.Services.AddChillAuthIdentityApi<EF.DummyContext, IdentityUser>();

                        var app = builder.Build();
                        app.UseAuthentication();
                        app.UseAuthorization();
                        app.MapChillApi();
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
    }
}

