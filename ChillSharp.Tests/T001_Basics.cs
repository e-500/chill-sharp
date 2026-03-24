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

using ChillSharp.Client;
using ChillSharp.Client.Dto;
using ChillSharp.EF;
using ChillSharp.Schema;
using ChillSharp.Schema.Model;
using ChillSharp.Tests.EF.Model;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ChillSharp.Tests
{
    [TestClass]
    public sealed class Basics
    {
        /// <summary>
        /// Creates a post through the Chill API and verifies that it can be queried back.
        /// </summary>
        [TestMethod]
        public void Step001_AddEntity()
        {
            // Start the shared API host only once for the whole test run.
            TestApiHost.EnsureStarted();

            // Use the HTTP client wrapper against the Chill endpoints.
            var cli = new ChillSharpClient("http://localhost:5000/api/chill");

            // Create a new Post entity.
            var e = new ChillDtoEntity();
            e.ChillType = "Model.Post";
            e.Guid = Guid.NewGuid();
            e.Properties.Add("Title", "New Title");
            e.Properties.Add("Author", "William Shakespeare");
            var cRes = cli.Create(e);
            Assert.IsNotNull(cRes);
            Assert.IsTrue(cRes.HasValue("Title"));
            Assert.AreEqual("New Title", cRes.GetString("Title"));
            Assert.AreEqual("William Shakespeare", cRes.GetString("Author"));

            // Query the created entity to verify persistence.
            var q = new ChillDtoQuery();
            q.ChillType = "Query.PostQuery";
            q.Properties.Add("Guid", cRes.Guid);
            q.ResultProperties = ChillDtoProperty.Build(["Guid", "Title", "Author"]);
            var qRes = cli.Query(q);
            Assert.IsNotNull(qRes);
            Assert.IsNotNull(qRes.Results);
            Assert.AreEqual(1, qRes.Results.Count);
            var qEntity = qRes.Results[0];
            Assert.IsNotNull(qEntity);
            Assert.IsTrue(qEntity.HasValue("Title"));
            Assert.AreEqual("New Title", qEntity.GetString("Title"));
            Assert.AreEqual("William Shakespeare", cRes.GetString("Author"));

            // Persist the Guid for the following step-based tests.
            PostGuid = qEntity.Guid;
        }

        private Guid? PostGuid = null;

        /// <summary>
        /// Updates the previously created post and verifies that unchanged fields are preserved.
        /// </summary>
        [TestMethod]
        public void Step002_UpdateEntity()
        {
            TestApiHost.EnsureStarted();

            // Ensure a seed entity exists for this step.
            if (!PostGuid.HasValue)
                Step001_AddEntity();

            Assert.IsNotNull(PostGuid);

            // Update only the Title field.
            var cli = new ChillSharpClient("http://localhost:5000/api/chill");

            var e = new ChillDtoEntity();
            e.ChillType = "Model.Post";
            e.Guid = PostGuid.Value;
            e.Properties.Add("Title", "Title changed");
            cli.Update(e);

            // Query again and verify partial update behavior.
            var q = new ChillDtoQuery();
            q.ChillType = "Query.PostQuery";
            q.Properties.Add("Guid", PostGuid.Value);
            q.ResultProperties = ChillDtoProperty.Build(["Guid", "Title", "Author"]);
            var qRes = cli.Query(q);
            Assert.IsNotNull(qRes);
            Assert.IsNotNull(qRes.Results);
            Assert.AreEqual(1, qRes.Results.Count);
            var qEntity = qRes.Results[0];
            Assert.IsNotNull(qEntity);
            Assert.IsTrue(qEntity.HasValue("Title"));
            Assert.AreEqual("Title changed", qEntity.GetString("Title"));
            Assert.IsTrue(qEntity.HasValue("Author"));
            Assert.AreEqual("William Shakespeare", qEntity.GetString("Author"));
        }

        /// <summary>
        /// Deletes the previously created post and verifies that it is no longer returned by queries.
        /// </summary>
        [TestMethod]
        public void Step003_DeleteEntity()
        {
            TestApiHost.EnsureStarted();

            // Ensure the entity exists before attempting deletion.
            if (!PostGuid.HasValue)
                Step002_UpdateEntity();

            Assert.IsNotNull(PostGuid);

            var cli = new ChillSharpClient("http://localhost:5000/api/chill");

            var e = new ChillDtoEntity();
            e.ChillType = "Model.Post";
            e.Guid = PostGuid.Value;
            cli.Delete(e);

            // Query the deleted entity and verify that nothing is returned.
            var q = new ChillDtoQuery();
            q.ChillType = "Query.PostQuery";
            q.Properties.Add("Guid", PostGuid.Value);
            var qRes = cli.Query(q);
            Assert.IsNotNull(qRes);
            Assert.IsNotNull(qRes.Results);
            Assert.AreEqual(0, qRes.Results.Count);
        }

        /// <summary>
        /// Finds the previously created post by Guid using the direct find endpoint.
        /// </summary>
        [TestMethod]
        public void Step004_FindEntity()
        {
            TestApiHost.EnsureStarted();

            // Ensure the entity exists before trying to find it.
            if (!PostGuid.HasValue)
                Step002_UpdateEntity();

            Assert.IsNotNull(PostGuid);

            var cli = new ChillSharpClient("http://localhost:5000/api/chill");

            var e = new ChillDtoEntity();
            e.ChillType = "Model.Post";
            e.Guid = PostGuid.Value;

            // Call the find endpoint and verify that the entity is returned.
            var entity = cli.Find(e);
            Assert.IsNotNull(entity);
        }

        /// <summary>
        /// Verifies that the base entity stores checksum, modifying user, and last-update timestamp.
        /// </summary>
        [TestMethod]
        public async Task Step005_BaseEntityStoresAuditFields()
        {
            TestApiHost.EnsureStarted();

            var client = new ChillSharpClient("http://localhost:5000/api/chill");
            var postGuid = Guid.NewGuid();

            var createEntity = new ChillDtoEntity
            {
                ChillType = "Model.Post",
                Guid = postGuid
            };
            createEntity.Properties.Add("Title", "Audit Title");
            createEntity.Properties.Add("Author", "Audit Author");
            var createdEntity = client.Create(createEntity);
            postGuid = createdEntity.Guid;

            await using var initialContext = TestApiHost.CreateDbContext();
            var createdPost = await initialContext.Post.FirstAsync(x => x.Guid == postGuid);
            var initialChecksum = createdPost.Checksum;
            var initialLastUpdateUtc = createdPost.LastUpdateUtc;

            Assert.AreEqual("dummy-user", createdPost.LastUpdateUser);
            Assert.IsTrue(createdPost.Checksum > 0);
            Assert.AreNotEqual(default, createdPost.LastUpdateUtc);

            await Task.Delay(20);

            var updateEntity = new ChillDtoEntity
            {
                ChillType = "Model.Post",
                Guid = postGuid
            };
            updateEntity.Properties.Add("Title", "Audit Title Changed");
            client.Update(updateEntity);

            await using var updatedContext = TestApiHost.CreateDbContext();
            var updatedPost = await updatedContext.Post.FirstAsync(x => x.Guid == postGuid);

            Assert.AreEqual("dummy-user", updatedPost.LastUpdateUser);
            Assert.IsTrue(updatedPost.LastUpdateUtc > initialLastUpdateUtc);
            Assert.AreNotEqual(initialChecksum, updatedPost.Checksum);
        }

        [TestMethod]
        public void Step006_ChecksumCalculationCanBeDisabledAndReEnabledPerEntityType()
        {
            var chillType = "Model.Post";
            using (var disabledContext = new EF.DummyContext())
            {
                disabledContext.EntityOptionsEntries.Add(new ChillEntityOptionsEntry
                {
                    Guid = Guid.NewGuid(),
                    ChillType = chillType,
                    ChecksumEnabled = false
                });

                var disabledEntity = new Post
                {
                    Title = "Checksum disabled",
                    Author = "Tester"
                };

                ((IChillEntity)disabledEntity).OnAfterUpdate(disabledContext);

                Assert.AreEqual("dummy-user", disabledEntity.LastUpdateUser);
                Assert.AreEqual(0L, disabledEntity.Checksum);
            }

            using (var enabledContext = new EF.DummyContext())
            {
                ChillEntityOptionsRuntimeCache.Invalidate(enabledContext, chillType);
                enabledContext.EntityOptionsEntries.Add(new ChillEntityOptionsEntry
                {
                    Guid = Guid.NewGuid(),
                    ChillType = chillType,
                    ChecksumEnabled = true
                });

                var enabledEntity = new Post
                {
                    Title = "Checksum enabled",
                    Author = "Tester"
                };

                ((IChillEntity)enabledEntity).OnAfterUpdate(enabledContext);

                Assert.AreEqual("dummy-user", enabledEntity.LastUpdateUser);
                Assert.IsTrue(enabledEntity.Checksum > 0);
            }
        }

        [TestMethod]
        public async Task Step007_LabelAndShortLabelCanUseConfiguredFormatStrings()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"chillsharp-entity-options-{Guid.NewGuid():N}.db");

            var options = new DbContextOptionsBuilder<EF.DummyContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            await using var context = new EF.DummyContext(options);
            await context.Database.EnsureCreatedAsync();
            var cache = new ChillSharp.Schema.ChillSchemaCache();
            var schemaService = new ChillSharp.Schema.ChillSchemaService(context, context, cache);
            var chillType = "Model.Post";

            await schemaService.SetEntityOptionsAsync(new ChillSharp.Dto.ChillDtoEntityOptions
            {
                ChillType = chillType,
                LabelFormatString = "{Title} - {Author} - {FullTextContent}",
                ShortLabelFormatString = "{Author}.{Title}",
                FullTextContentFormatString = "{Author}::{Title}::{Checksum}"
            });

            var post = new Post
            {
                Title = "Configured title",
                Author = "Configured author"
            };

            Assert.AreEqual("Configured title - Configured author - ", post.GetLabel(context));
            Assert.AreEqual("Configured author.Configured title", post.GetShortLabel(context));
            Assert.AreEqual("Configured author::Configured title::0", post.GetFullTextContent(context));

            await schemaService.SetEntityOptionsAsync(new ChillSharp.Dto.ChillDtoEntityOptions
            {
                ChillType = chillType,
                LabelFormatString = "{Author}",
                ShortLabelFormatString = "{Title}",
                FullTextContentFormatString = "{Title}|{LastUpdateUser}|{FullTextContent}"
            });

            Assert.AreEqual("Configured author", post.GetLabel(context));
            Assert.AreEqual("Configured title", post.GetShortLabel(context));
            Assert.AreEqual("Configured title||", post.GetFullTextContent(context));
        }

        [TestMethod]
        public async Task Step008_ChangeLogStoresSnapshotHistoryAndMocksLinkedEntities()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"chillsharp-changelog-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<EF.DummyContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            await using var context = new EF.DummyContext(options);
            await context.Database.EnsureCreatedAsync();

            context.EntityOptionsEntries.AddRange(
                new ChillEntityOptionsEntry
                {
                    Guid = Guid.NewGuid(),
                    ChillType = "Model.Post",
                    ChangeLogEnabled = true
                },
                new ChillEntityOptionsEntry
                {
                    Guid = Guid.NewGuid(),
                    ChillType = "Model.Blog",
                    ChangeLogEnabled = true
                });
            await context.SaveChangesAsync();

            var engine = new ChillEngine(context);

            var blog = (Blog)engine.Create(new Blog
            {
                Title = "Tracked blog",
                Url = "https://tracked.example"
            });

            var post = (Post)engine.Create(new Post
            {
                Title = "Tracked post",
                Author = "Andrea",
                Blog = blog
            });

            post.Title = "Tracked post updated";
            engine.Update(post);

            await context.Entry(blog).Collection(x => x.Posts!).LoadAsync();
            blog.Url = "https://tracked.example/v2";
            engine.Update(blog);

            var postLog = JsonDocument.Parse(post.ChangeLog);
            var postSnapshots = postLog.RootElement;
            Assert.AreEqual(2, postSnapshots.GetArrayLength());

            var latestPost = postSnapshots[1];
            Assert.AreEqual("Tracked post updated", latestPost.GetProperty("Properties").GetProperty("Title").GetString());
            var blogMock = latestPost.GetProperty("Properties").GetProperty("Blog");
            Assert.AreEqual(blog.Guid, blogMock.GetProperty("Guid").GetGuid());
            Assert.AreEqual(blog.Label, blogMock.GetProperty("Label").GetString());
            Assert.AreEqual(blog.ShortLabel, blogMock.GetProperty("ShortLabel").GetString());
            Assert.IsFalse(blogMock.TryGetProperty("Properties", out _));

            var blogLog = JsonDocument.Parse(blog.ChangeLog);
            var blogSnapshots = blogLog.RootElement;
            Assert.AreEqual(2, blogSnapshots.GetArrayLength());

            var latestBlog = blogSnapshots[1];
            Assert.AreEqual("https://tracked.example/v2", latestBlog.GetProperty("Properties").GetProperty("Url").GetString());
            var postsMock = latestBlog.GetProperty("Properties").GetProperty("Posts");
            Assert.AreEqual(1, postsMock.GetArrayLength());
            Assert.AreEqual(post.Guid, postsMock[0].GetProperty("Guid").GetGuid());
            Assert.AreEqual(post.Label, postsMock[0].GetProperty("Label").GetString());
            Assert.AreEqual(post.ShortLabel, postsMock[0].GetProperty("ShortLabel").GetString());
            Assert.IsFalse(postsMock[0].TryGetProperty("Properties", out _));
        }
    }
}
