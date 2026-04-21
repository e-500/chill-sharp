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

using ChillSharp.Annotations;
using ChillSharp.Client;
using ChillSharp.Client.Dto;
using ChillSharp.EF;
using ChillSharp.Schema;
using ChillSharp.Schema.Model;
using ChillSharp.Tests.EF.Model;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Reflection;
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
            TestApiHost.EnsureStarted(6002);

            // Use the HTTP client wrapper against the Chill endpoints.
            var cli = new ChillSharpClient("http://localhost:6002/api/chill");

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
            Assert.HasCount(1, qRes.Results);
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
            TestApiHost.EnsureStarted(6002);

            // Ensure a seed entity exists for this step.
            if (!PostGuid.HasValue)
                Step001_AddEntity();

            Assert.IsNotNull(PostGuid);

            // Update only the Title field.
            var cli = new ChillSharpClient("http://localhost:6002/api/chill");

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
            Assert.HasCount(1, qRes.Results);
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
            TestApiHost.EnsureStarted(6002);

            // Ensure the entity exists before attempting deletion.
            if (!PostGuid.HasValue)
                Step002_UpdateEntity();

            Assert.IsNotNull(PostGuid);

            var cli = new ChillSharpClient("http://localhost:6002/api/chill");

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
            Assert.IsEmpty(qRes.Results);
        }

        /// <summary>
        /// Finds the previously created post by Guid using the direct find endpoint.
        /// </summary>
        [TestMethod]
        public void Step004_FindEntity()
        {
            TestApiHost.EnsureStarted(6002);

            // Ensure the entity exists before trying to find it.
            if (!PostGuid.HasValue)
                Step002_UpdateEntity();

            Assert.IsNotNull(PostGuid);

            var cli = new ChillSharpClient("http://localhost:6002/api/chill");

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
            TestApiHost.EnsureStarted(6002);

            var client = new ChillSharpClient("http://localhost:6002/api/chill");
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
            var initialLastUpdate = createdPost.LastUpdate;
            var systemTimeZone = ChillSharpInitOptions.GetSystemTimeZone();

            Assert.AreEqual("dummy-user", createdPost.LastUpdateUser);
            Assert.IsGreaterThan(0L, createdPost.Checksum);
            Assert.IsNotNull(createdPost.LastUpdate);
            Assert.AreEqual(
                (int)systemTimeZone.GetUtcOffset(createdPost.LastUpdate!.Value).TotalMinutes,
                createdPost.LastUpdateUtcOffset);

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
            Assert.IsTrue(updatedPost.LastUpdate.HasValue && initialLastUpdate.HasValue && updatedPost.LastUpdate.Value > initialLastUpdate.Value);
            Assert.AreEqual(
                (int)systemTimeZone.GetUtcOffset(updatedPost.LastUpdate!.Value).TotalMinutes,
                updatedPost.LastUpdateUtcOffset);
            Assert.AreNotEqual(initialChecksum, updatedPost.Checksum);
        }

        [TestMethod]
        public void Step005_DtoEntityIgnoresServerManagedAuditFields()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"chillsharp-audit-dto-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<EF.DummyContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            using var db = new EF.DummyContext(options);
            db.Database.EnsureCreated();

            var initialLastUpdate = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Unspecified);
            var post = new Post
            {
                Guid = Guid.NewGuid(),
                Title = "Original title",
                Author = "Original author",
                Checksum = 123,
                LastUpdateUser = "server-user",
                LastUpdate = initialLastUpdate,
                LastUpdateUtcOffset = 60
            };

            var dto = new ChillSharp.Dto.ChillDtoEntity
            {
                ChillType = "Model.Post",
                Guid = post.Guid,
                Properties = new Dictionary<string, object?>
                {
                    ["Title"] = "Changed title",
                    ["Checksum"] = 999L,
                    ["LastUpdateUser"] = "dto-user",
                    ["LastUpdate"] = "1999-12-31T23:59:59",
                    ["LastUpdateUtcOffset"] = -720
                }
            };

            dto.ToEntity(db, post);

            Assert.AreEqual("Changed title", post.Title);
            Assert.AreEqual(123, post.Checksum);
            Assert.AreEqual("server-user", post.LastUpdateUser);
            Assert.AreEqual(initialLastUpdate, post.LastUpdate);
            Assert.AreEqual(60, post.LastUpdateUtcOffset);
        }

        [TestMethod]
        public void Step006_ChecksumCalculationCanBeDisabledAndReEnabledPerEntityType()
        {
            TestApiHost.EnsureStarted(6002);
            var client = new ChillSharpClient("http://localhost:6002/api/chill");

            var chillType = "Model.Post";
            var chillTypeGuid = Guid.NewGuid();

            var entityOption = client.GetEntityOptions(chillType);
            entityOption.ChecksumEnabled = false;
            client.SetEntityOptions(entityOption);

            var disabledEntity = new ChillDtoEntity
            {
                ChillType = "Model.Post",
                Guid = chillTypeGuid
            };
            disabledEntity.Properties.Add("Title", "Checksum disabled");
            disabledEntity.Properties.Add("Author", "Tester");
            disabledEntity.Properties.Add("Checksum", -1L);
            disabledEntity = client.Create(disabledEntity);
            Assert.IsNotNull(disabledEntity.GetInt64("Checksum"));
            Assert.AreEqual(0L, disabledEntity.GetInt64("Checksum"));

            entityOption.ChecksumEnabled = true;
            client.SetEntityOptions(entityOption);

            var enabledEntity = new ChillDtoEntity
            {
                ChillType = "Model.Post",
                Guid = Guid.NewGuid()
            };
            enabledEntity.Properties.Add("Title", "Checksum disabled");
            enabledEntity.Properties.Add("Author", "Tester");
            enabledEntity = client.Create(enabledEntity);
            Assert.IsNotNull(enabledEntity.GetInt64("Checksum"));
            long value = enabledEntity.GetInt64("Checksum").Value;
            Assert.IsGreaterThan<long>(0L, value);
        }

        [TestMethod]
        public async Task Step007_LabelAndShortLabelCanUseConfiguredFormatStrings()
        {
            TestApiHost.EnsureStarted(6002);

            var client = new ChillSharpClient("http://localhost:6002/api/chill");
            var chillType = "Model.Post";
            var originalOptions = client.GetEntityOptions(chillType);

            try
            {
                client.SetEntityOptions(new ChillDtoEntityOptions
                {
                    ChillType = chillType,
                    ChecksumEnabled = originalOptions.ChecksumEnabled,
                    ChangeLogEnabled = originalOptions.ChangeLogEnabled,
                    EnableMCP = originalOptions.EnableMCP,
                    MCPDescription = originalOptions.MCPDescription,
                    LabelFormatString = "{Title} - {Author} - {FullTextContent}",
                    ShortLabelFormatString = "{Author}.{Title}",
                    FullTextContentFormatString = "{Author}::{Title}::{Checksum}"
                });

                var created = client.Create(new ChillDtoEntity
                {
                    ChillType = chillType,
                    Guid = Guid.NewGuid(),
                    Properties = new Dictionary<string, object?>
                    {
                        ["Title"] = "Configured title",
                        ["Author"] = "Configured author"
                    }
                });

                Assert.AreEqual("Configured title - Configured author - ", created.Label);
                Assert.AreEqual("Configured author.Configured title", created.ShortLabel);

                await using (var verificationContext = TestApiHost.CreateDbContext())
                {
                    var createdPost = await verificationContext.Post.FirstAsync(x => x.Guid == created.Guid);
                    Assert.AreEqual($"configured author::configured title::{createdPost.Checksum}", createdPost.FullTextContent);
                }

                client.SetEntityOptions(new ChillDtoEntityOptions
                {
                    ChillType = chillType,
                    ChecksumEnabled = originalOptions.ChecksumEnabled,
                    ChangeLogEnabled = originalOptions.ChangeLogEnabled,
                    EnableMCP = originalOptions.EnableMCP,
                    MCPDescription = originalOptions.MCPDescription,
                    LabelFormatString = "{Author}",
                    ShortLabelFormatString = "{Title}",
                    FullTextContentFormatString = "{Title}|{LastUpdateUser}|{FullTextContent}"
                });

                var updated = client.Update(new ChillDtoEntity
                {
                    ChillType = chillType,
                    Guid = created.Guid,
                    Properties = new Dictionary<string, object?>
                    {
                        ["Title"] = "Configured title updated"
                    }
                });

                Assert.AreEqual("Configured author", updated.Label);
                Assert.AreEqual("Configured title updated", updated.ShortLabel);

                await using var updatedVerificationContext = TestApiHost.CreateDbContext();
                var updatedPost = await updatedVerificationContext.Post.FirstAsync(x => x.Guid == created.Guid);
                Assert.AreEqual("configured title updated|dummy-user|", updatedPost.FullTextContent);

                var blog = client.Create(new ChillDtoEntity
                {
                    ChillType = "Model.Blog",
                    Guid = Guid.NewGuid(),
                    Properties = new Dictionary<string, object?>
                    {
                        ["Title"] = "Related blog",
                        ["Url"] = "https://related.example"
                    }
                });

                client.SetEntityOptions(new ChillDtoEntityOptions
                {
                    ChillType = chillType,
                    ChecksumEnabled = originalOptions.ChecksumEnabled,
                    ChangeLogEnabled = originalOptions.ChangeLogEnabled,
                    EnableMCP = originalOptions.EnableMCP,
                    MCPDescription = originalOptions.MCPDescription,
                    LabelFormatString = "{Blog.Title} - {Title}",
                    ShortLabelFormatString = "{Blog.Url}",
                    FullTextContentFormatString = "{Blog.Title} {Blog.Url} {Author}"
                });

                var related = client.Create(new ChillDtoEntity
                {
                    ChillType = chillType,
                    Guid = Guid.NewGuid(),
                    Properties = new Dictionary<string, object?>
                    {
                        ["Title"] = "Related post",
                        ["Author"] = "Related author",
                        ["Blog"] = blog.Mock()
                    }
                });

                Assert.AreEqual("Related blog - Related post", related.Label);
                Assert.AreEqual("https://related.example", related.ShortLabel);

                await using var relatedVerificationContext = TestApiHost.CreateDbContext();
                var relatedPost = await relatedVerificationContext.Post.FirstAsync(x => x.Guid == related.Guid);
                Assert.AreEqual("related blog https://related.example related author", relatedPost.FullTextContent);
            }
            finally
            {
                client.SetEntityOptions(originalOptions);
            }
        }

        [TestMethod]
        public async Task Step008_ChangeLogStoresSnapshotHistoryAndMocksLinkedEntities()
        {
            TestApiHost.EnsureStarted(6002);

            var client = new ChillSharpClient("http://localhost:6002/api/chill");
            var originalPostOptions = client.GetEntityOptions("Model.Post");
            var originalBlogOptions = client.GetEntityOptions("Model.Blog");
            var suffix = Guid.NewGuid().ToString("N");

            try
            {
                client.SetEntityOptions(new ChillDtoEntityOptions
                {
                    ChillType = "Model.Post",
                    ChecksumEnabled = originalPostOptions.ChecksumEnabled,
                    LabelFormatString = originalPostOptions.LabelFormatString,
                    ShortLabelFormatString = originalPostOptions.ShortLabelFormatString,
                    FullTextContentFormatString = originalPostOptions.FullTextContentFormatString,
                    EnableMCP = originalPostOptions.EnableMCP,
                    MCPDescription = originalPostOptions.MCPDescription,
                    ChangeLogEnabled = true
                });

                client.SetEntityOptions(new ChillDtoEntityOptions
                {
                    ChillType = "Model.Blog",
                    ChecksumEnabled = originalBlogOptions.ChecksumEnabled,
                    LabelFormatString = originalBlogOptions.LabelFormatString,
                    ShortLabelFormatString = originalBlogOptions.ShortLabelFormatString,
                    FullTextContentFormatString = originalBlogOptions.FullTextContentFormatString,
                    EnableMCP = originalBlogOptions.EnableMCP,
                    MCPDescription = originalBlogOptions.MCPDescription,
                    ChangeLogEnabled = true
                });

                var blog = client.Create(new ChillDtoEntity
                {
                    ChillType = "Model.Blog",
                    Guid = Guid.NewGuid(),
                    Properties = new Dictionary<string, object?>
                    {
                        ["Title"] = $"Tracked blog {suffix}",
                        ["Url"] = $"https://tracked.example/{suffix}"
                    }
                });

                var post = client.Create(new ChillDtoEntity
                {
                    ChillType = "Model.Post",
                    Guid = Guid.NewGuid(),
                    Properties = new Dictionary<string, object?>
                    {
                        ["Title"] = $"Tracked post {suffix}",
                        ["Author"] = "Andrea",
                        ["Blog"] = blog.Mock()
                    }
                });

                client.Update(new ChillDtoEntity
                {
                    ChillType = "Model.Post",
                    Guid = post.Guid,
                    Properties = new Dictionary<string, object?>
                    {
                        ["Title"] = $"Tracked post updated {suffix}"
                    }
                });

                client.Update(new ChillDtoEntity
                {
                    ChillType = "Model.Blog",
                    Guid = blog.Guid,
                    Properties = new Dictionary<string, object?>
                    {
                        ["Url"] = $"https://tracked.example/{suffix}/v2"
                    }
                });

                await using var context = TestApiHost.CreateDbContext();
                var persistedPost = await context.Post.Include(x => x.Blog).FirstAsync(x => x.Guid == post.Guid);
                var persistedBlog = await context.Blog.Include(x => x.Posts).FirstAsync(x => x.Guid == blog.Guid);

                var postLog = JsonDocument.Parse(persistedPost.ChangeLog);
                var postSnapshots = postLog.RootElement;
                Assert.AreEqual(2, postSnapshots.GetArrayLength());

                var latestPost = postSnapshots[1];
                Assert.AreEqual($"Tracked post updated {suffix}", latestPost.GetProperty("Properties").GetProperty("Title").GetString());
                var blogMock = latestPost.GetProperty("Properties").GetProperty("Blog");
                Assert.AreEqual(persistedBlog.Guid, blogMock.GetProperty("Guid").GetGuid());
                Assert.AreEqual(persistedBlog.Label, blogMock.GetProperty("Label").GetString());
                Assert.AreEqual(persistedBlog.ShortLabel, blogMock.GetProperty("ShortLabel").GetString());
                Assert.IsFalse(blogMock.TryGetProperty("Properties", out _));

                var blogLog = JsonDocument.Parse(persistedBlog.ChangeLog);
                var blogSnapshots = blogLog.RootElement;
                Assert.AreEqual(2, blogSnapshots.GetArrayLength());

                var latestBlog = blogSnapshots[1];
                Assert.AreEqual($"https://tracked.example/{suffix}/v2", latestBlog.GetProperty("Properties").GetProperty("Url").GetString());
                var postsMock = latestBlog.GetProperty("Properties").GetProperty("Posts");
                Assert.AreEqual(1, postsMock.GetArrayLength());
                Assert.AreEqual(persistedPost.Guid, postsMock[0].GetProperty("Guid").GetGuid());
                Assert.AreEqual(persistedPost.Label, postsMock[0].GetProperty("Label").GetString());
                Assert.AreEqual(persistedPost.ShortLabel, postsMock[0].GetProperty("ShortLabel").GetString());
                Assert.IsFalse(postsMock[0].TryGetProperty("Properties", out _));
            }
            finally
            {
                client.SetEntityOptions(originalPostOptions);
                client.SetEntityOptions(originalBlogOptions);
            }
        }

        [TestMethod]
        public void Step009_QueryFullTextSearchSplitsTokensAndUsesAndLogic()
        {
            TestApiHost.EnsureStarted(6002);

            var client = new ChillSharpClient("http://localhost:6002/api/chill");
            var tokenA = $"Perché-{Guid.NewGuid():N}";
            var tokenB = $"Beta-{Guid.NewGuid():N}";

            client.SetEntityOptions(new ChillDtoEntityOptions
            {
                ChillType = "Model.Post",
                FullTextContentFormatString = "{Title} {Author}"
            });

            var matchingPost = client.Create(new ChillDtoEntity
            {
                ChillType = "Model.Post",
                Guid = Guid.NewGuid(),
                Properties = new Dictionary<string, object?>
                {
                    ["Title"] = tokenA,
                    ["Author"] = tokenB
                }
            });

            client.Create(new ChillDtoEntity
            {
                ChillType = "Model.Post",
                Guid = Guid.NewGuid(),
                Properties = new Dictionary<string, object?>
                {
                    ["Title"] = tokenA,
                    ["Author"] = "single-token"
                }
            });

            client.Create(new ChillDtoEntity
            {
                ChillType = "Model.Post",
                Guid = Guid.NewGuid(),
                Properties = new Dictionary<string, object?>
                {
                    ["Title"] = "other-token",
                    ["Author"] = tokenB
                }
            });

            var query = new ChillDtoQuery
            {
                ChillType = "Query.PostQuery",
                ResultProperties = ChillDtoProperty.Build(["Guid", "Title", "Author"])
            };
            query.Properties.Add("FullTextSearch", $"{tokenA.Replace("é", "è", StringComparison.Ordinal)} {tokenB}");

            var result = client.Query(query);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Results);
            Assert.HasCount(1, result.Results);
            Assert.AreEqual(tokenA, result.Results[0].GetString("Title"));
            Assert.AreEqual(tokenB, result.Results[0].GetString("Author"));

            using var db = TestApiHost.CreateDbContext();
            var persistedPost = db.Post.First(x => x.Guid == matchingPost.Guid);
            Assert.IsTrue(persistedPost.FullTextContent.Contains("perche", StringComparison.Ordinal));
            Assert.IsFalse(persistedPost.FullTextContent.Contains('é'));
            Assert.IsFalse(persistedPost.FullTextContent.Contains('è'));
        }

        [TestMethod]
        public void Step010_AutocompleteEntityReturnsUpdatedFields()
        {
            TestApiHost.EnsureStarted(6002);

            var client = new ChillSharpClient("http://localhost:6002/api/chill");
            var entity = new ChillDtoEntity
            {
                ChillType = "Model.Blog",
                Guid = Guid.NewGuid()
            };
            entity.Properties["Title"] = "Autocomplete Blog";
            entity.Properties["Url"] = string.Empty;

            var result = client.Autocomplete(entity);

            Assert.IsNotNull(result);
            Assert.AreEqual("Autocomplete Blog", result.GetString("Title"));
            Assert.AreEqual("https://autocomplete.local/autocomplete-blog", result.GetString("Url"));
        }

        [TestMethod]
        public void Step011_AutocompleteQueryReturnsUpdatedFields()
        {
            TestApiHost.EnsureStarted(6002);

            var client = new ChillSharpClient("http://localhost:6002/api/chill");
            var query = new ChillDtoQuery
            {
                ChillType = "Query.BlogQuery"
            };
            query.Properties["Title"] = "  autocomplete query  ";

            var result = client.Autocomplete(query);

            Assert.IsNotNull(result);
            Assert.AreEqual("autocomplete query", result.Properties["Title"]?.ToString());
            Assert.AreEqual("autocomplete query autocomplete", result.Properties["FullTextSearch"]?.ToString());
        }

        [TestMethod]
        public async Task Step012_AutocompleteDoesNotPersistEntityChanges()
        {
            TestApiHost.EnsureStarted(6002);

            var client = new ChillSharpClient("http://localhost:6002/api/chill");
            var created = client.Create(new ChillDtoEntity
            {
                ChillType = "Model.Blog",
                Guid = Guid.NewGuid(),
                Properties = new Dictionary<string, object?>
                {
                    ["Title"] = "Persisted Blog",
                    ["Url"] = "https://persisted.local/original"
                }
            });

            var autocompleteRequest = new ChillDtoEntity
            {
                ChillType = "Model.Blog",
                Guid = created.Guid,
                Properties = new Dictionary<string, object?>
                {
                    ["Title"] = "Persisted Blog Updated",
                    ["Url"] = string.Empty
                }
            };

            var autocompleted = client.Autocomplete(autocompleteRequest);

            Assert.AreEqual("Persisted Blog Updated", autocompleted.GetString("Title"));
            Assert.AreEqual("https://autocomplete.local/persisted-blog-updated", autocompleted.GetString("Url"));

            await using var db = TestApiHost.CreateDbContext();
            var persisted = await db.Blog.FirstAsync(x => x.Guid == created.Guid);
            Assert.AreEqual("Persisted Blog", persisted.Title);
            Assert.AreEqual("https://persisted.local/original", persisted.Url);
        }

        [TestMethod]
        public void Step013_GetCollectionElementTypeSupportsDirectEnumerableTypes()
        {
            var mapperType = typeof(ChillEngine).Assembly.GetType("ChillSharp.Dto.ChillDtoObjectMapper");
            Assert.IsNotNull(mapperType);

            var method = mapperType
                .GetMethod("GetCollectionElementType", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(method);

            var enumerableElementType = (Type?)method.Invoke(null, [typeof(IEnumerable<Post>)]);
            var arrayElementType = (Type?)method.Invoke(null, [typeof(Post[])]);

            Assert.AreEqual(typeof(Post), enumerableElementType);
            Assert.AreEqual(typeof(Post), arrayElementType);
        }

        [TestMethod]
        public void Step014_ApplyPropertiesKeepsNullableDateFieldsNullWhenDtoDateIsBlank()
        {
            using var db = TestApiHost.CreateDbContext();

            var target = new NullableDateEntity();
            var sourceValues = new Dictionary<string, object?>
            {
                ["PublishedOn"] = string.Empty
            };

            var mapperType = typeof(ChillEngine).Assembly.GetType("ChillSharp.Dto.ChillDtoObjectMapper");
            Assert.IsNotNull(mapperType);

            var applyPropertiesMethod = mapperType.GetMethod(
                "ApplyProperties",
                BindingFlags.Public | BindingFlags.Static);

            Assert.IsNotNull(applyPropertiesMethod);

            applyPropertiesMethod.Invoke(null,
            [
                db,
                target,
                "Tests.NullableDateEntity",
                sourceValues,
                typeof(NullableDateEntity).GetProperties(),
                "NullableDateEntity",
                false,
                null
            ]);

            Assert.IsNull(target.PublishedOn);
        }

        [TestMethod]
        public void Step015_ApplyPropertiesUsesOnInflateForInflatedPropertiesInsteadOfDtoValues()
        {
            using var db = TestApiHost.CreateDbContext();

            var target = new InflateOnlyEntity();
            var sourceValues = new Dictionary<string, object?>
            {
                ["ComputedTitle"] = 123
            };

            var mapperType = typeof(ChillEngine).Assembly.GetType("ChillSharp.Dto.ChillDtoObjectMapper");
            Assert.IsNotNull(mapperType);

            var applyPropertiesMethod = mapperType.GetMethod(
                "ApplyProperties",
                BindingFlags.Public | BindingFlags.Static);

            Assert.IsNotNull(applyPropertiesMethod);

            applyPropertiesMethod.Invoke(null,
            [
                db,
                target,
                "Tests.InflateOnlyEntity",
                sourceValues,
                typeof(InflateOnlyEntity).GetProperties()
                    .Where(prop => prop.IsDefined(typeof(ChillPropertyAttribute), false)),
                "InflateOnlyEntity",
                false,
                (Action<string>)target.OnInflateProperty
            ]);

            Assert.AreEqual(1, target.OnInflateCalls);
            Assert.AreEqual("ComputedTitle", target.ComputedTitle);
        }

        [TestMethod]
        public void Step016_ValidateEntityReturnsValidationErrors()
        {
            TestApiHost.EnsureStarted(6002);

            var client = new ChillSharpClient("http://localhost:6002/api/chill");
            var errors = client.Validate(new ChillDtoEntity
            {
                ChillType = "Model.Blog",
                Guid = Guid.NewGuid(),
                Properties = new Dictionary<string, object?>
                {
                    ["Title"] = "invalid"
                }
            });

            Assert.HasCount(1, errors);
            Assert.AreEqual("Title", errors[0].FieldName);
            Assert.AreEqual("Blog title is invalid.", errors[0].Message);
        }

        [TestMethod]
        public void Step017_ValidateQueryReturnsValidationErrors()
        {
            TestApiHost.EnsureStarted(6002);

            var client = new ChillSharpClient("http://localhost:6002/api/chill");
            var errors = client.Validate(new ChillDtoQuery
            {
                ChillType = "Query.BlogQuery",
                Properties = new Dictionary<string, object?>
                {
                    ["Title"] = "invalid"
                }
            });

            Assert.HasCount(1, errors);
            Assert.AreEqual("Title", errors[0].FieldName);
            Assert.AreEqual("Blog query title is invalid.", errors[0].Message);
        }

        [TestMethod]
        public void Step018_ChunkSupportsValidateAndAutocompleteOperations()
        {
            TestApiHost.EnsureStarted(6002);

            var client = new ChillSharpClient("http://localhost:6002/api/chill");
            var operations = client.Chunk(
            [
                new ChillOperation
                {
                    Index = 0,
                    Verb = ChillOperationVerb.VALIDATE,
                    Entity = new ChillDtoEntity
                    {
                        ChillType = "Model.Blog",
                        Guid = Guid.NewGuid(),
                        Properties = new Dictionary<string, object?>
                        {
                            ["Title"] = "invalid"
                        }
                    }
                },
                new ChillOperation
                {
                    Index = 1,
                    Verb = ChillOperationVerb.VALIDATE,
                    Query = new ChillDtoQuery
                    {
                        ChillType = "Query.BlogQuery",
                        Properties = new Dictionary<string, object?>
                        {
                            ["Title"] = "invalid"
                        }
                    }
                },
                new ChillOperation
                {
                    Index = 2,
                    Verb = ChillOperationVerb.AUTOCOMPLETE,
                    Entity = new ChillDtoEntity
                    {
                        ChillType = "Model.Blog",
                        Guid = Guid.NewGuid(),
                        Properties = new Dictionary<string, object?>
                        {
                            ["Title"] = "Chunk Autocomplete",
                            ["Url"] = string.Empty
                        }
                    }
                }
            ]);

            Assert.HasCount(3, operations);
            Assert.HasCount(1, operations[0].ValidationErrors);
            Assert.AreEqual("Blog title is invalid.", operations[0].ValidationErrors![0].Message);
            Assert.HasCount(1, operations[1].ValidationErrors);
            Assert.AreEqual("Blog query title is invalid.", operations[1].ValidationErrors![0].Message);
            Assert.AreEqual("https://autocomplete.local/chunk-autocomplete", operations[2].Entity!.GetString("Url"));
        }

        [TestMethod]
        public void Step019_LookupPerformsGenericFullTextSearchAgainstEntityType()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"chillsharp-lookup-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<EF.DummyContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            using var db = new EF.DummyContext(options);
            db.Database.EnsureCreated();

            var tokenA = $"lookup-alpha-{Guid.NewGuid():N}";
            var tokenB = $"lookup-beta-{Guid.NewGuid():N}";
            db.Post.Add(new Post
            {
                Guid = Guid.NewGuid(),
                Title = tokenA,
                Author = tokenB,
                FullTextContent = $"{tokenA} {tokenB}",
                LastUpdateUser = string.Empty
            });
            db.Post.Add(new Post
            {
                Guid = Guid.NewGuid(),
                Title = tokenA,
                Author = "lookup-single-token",
                FullTextContent = $"{tokenA} lookup-single-token",
                LastUpdateUser = string.Empty
            });
            db.SaveChanges();

            var dtoEngine = new ChillDtoEngine(db);

            var lookup = new ChillSharp.Dto.ChillDtoQuery
            {
                ChillType = "Model.Post",
                ResultProperties = ChillSharp.Dto.ChillDtoProperty.Build(["Guid", "Title", "Author"])
            };
            lookup.Properties["FullTextSearch"] = $"{tokenA} {tokenB}";

            var result = dtoEngine.Lookup(lookup);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Results);
            Assert.HasCount(1, result.Results);
            Assert.AreEqual(tokenA, result.Results[0].Properties["Title"]?.ToString());
            Assert.AreEqual(tokenB, result.Results[0].Properties["Author"]?.ToString());
        }

        [TestMethod]
        public void Step020_BaseChillQueryOnQueryResolvesDbSetFromEntityType()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"chillsharp-autoquery-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<EF.DummyContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            using var db = new EF.DummyContext(options);
            db.Database.EnsureCreated();

            var engine = new ChillEngine(db);
            var token = $"auto-query-{Guid.NewGuid():N}";

            db.Post.Add(new Post
            {
                Guid = Guid.NewGuid(),
                Title = token,
                Author = "default-query",
                FullTextContent = token,
                LastUpdateUser = string.Empty
            });
            db.SaveChanges();

            var results = engine.Query(new AutoPostQuery
            {
                FullTextSearch = token
            });

            Assert.HasCount(1, results);
            Assert.AreEqual(token, ((Post)results[0]).Title);
        }

        [TestMethod]
        public async Task Step021_ToEntityAcceptsCamelCaseDtoPropertiesAndNestedEntities()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"chillsharp-camelcase-dto-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<EF.DummyContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            await using var db = new EF.DummyContext(options);
            await db.Database.EnsureCreatedAsync();

            var blog = new Blog
            {
                Guid = Guid.NewGuid(),
                Title = "Mapped blog",
                Url = "https://mapped.local"
            };
            db.Blog.Add(blog);
            await db.SaveChangesAsync();

            var blogPayload = JsonDocument.Parse($$"""
            {
              "guid": "{{blog.Guid:D}}",
              "chillType": "Model.Blog",
              "label": "Mapped blog",
              "shortLabel": "Mapped blog"
            }
            """);

            var dto = new ChillSharp.Dto.ChillDtoEntity
            {
                ChillType = "Model.Post",
                Guid = Guid.NewGuid(),
                Properties = new Dictionary<string, object?>
                {
                    ["title"] = "Camel title",
                    ["author"] = "Camel author",
                    ["blog"] = blogPayload.RootElement.Clone()
                }
            };

            var post = new Post();
            dto.ToEntity(db, post);

            Assert.AreEqual("Camel title", post.Title);
            Assert.AreEqual("Camel author", post.Author);
            Assert.IsNotNull(post.Blog);
            Assert.AreEqual(blog.Guid, post.Blog.Guid);
        }

        [TestMethod]
        public void Step022_ApplyPropertiesNormalizesTemporalValuesUsingConfiguredSystemTimeZone()
        {
            var originalEnvironmentValue = Environment.GetEnvironmentVariable(ChillSharpInitOptions.SystemTimeZoneEnvironmentVariableName);

            try
            {
                Environment.SetEnvironmentVariable(ChillSharpInitOptions.SystemTimeZoneEnvironmentVariableName, "Europe/Rome");
                ChillSharpInitOptions.Initialize();

                using var db = TestApiHost.CreateDbContext();
                var target = new TemporalMappingEntity();
                var sourceValues = new Dictionary<string, object?>
                {
                    ["OccurredAtUtc"] = "2024-01-10T12:30:15.000Z",
                    ["OccurredAtOffset"] = "2024-01-10T12:30:15.000+02:00",
                    ["RecordedAtOffset"] = "2024-01-10T12:30:15.000+02:00",
                    ["PublishedOn"] = "2024-01-10T23:59:58.321-05:00",
                    ["PublishedAt"] = "2024-01-10T23:59:58.321-05:00"
                };

                var mapperType = typeof(ChillEngine).Assembly.GetType("ChillSharp.Dto.ChillDtoObjectMapper");
                Assert.IsNotNull(mapperType);

                var applyPropertiesMethod = mapperType.GetMethod(
                    "ApplyProperties",
                    BindingFlags.Public | BindingFlags.Static);

                Assert.IsNotNull(applyPropertiesMethod);

                applyPropertiesMethod.Invoke(null,
                [
                    db,
                    target,
                    "Tests.TemporalMappingEntity",
                    sourceValues,
                    typeof(TemporalMappingEntity).GetProperties(),
                    "TemporalMappingEntity",
                    false,
                    null
                ]);

                Assert.AreEqual(new DateTime(2024, 1, 10, 12, 30, 15, DateTimeKind.Utc), target.OccurredAtUtc);
                Assert.AreEqual(DateTimeKind.Utc, target.OccurredAtUtc.Kind);
                Assert.AreEqual(new DateTime(2024, 1, 10, 10, 30, 15, DateTimeKind.Utc), target.OccurredAtOffset);
                Assert.AreEqual(DateTimeKind.Utc, target.OccurredAtOffset.Kind);
                Assert.AreEqual(DateTimeOffset.Parse("2024-01-10T12:30:15.000+02:00", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind), target.RecordedAtOffset);
                Assert.AreEqual(new DateOnly(2024, 1, 10), target.PublishedOn);
                Assert.AreEqual(new TimeOnly(23, 59, 58, 321), target.PublishedAt);

                var targetWithoutOffsets = new TemporalMappingEntity();
                var sourceValuesWithoutOffsets = new Dictionary<string, object?>
                {
                    ["OccurredAtUtc"] = "2024-01-10T12:30:15.000",
                    ["OccurredAtOffset"] = "2024-01-10T12:30:15.000",
                    ["RecordedAtOffset"] = "2024-01-10T12:30:15.000"
                };

                applyPropertiesMethod.Invoke(null,
                [
                    db,
                    targetWithoutOffsets,
                    "Tests.TemporalMappingEntity",
                    sourceValuesWithoutOffsets,
                    typeof(TemporalMappingEntity).GetProperties(),
                    "TemporalMappingEntity",
                    false,
                    null
                ]);

                Assert.AreEqual(new DateTime(2024, 1, 10, 11, 30, 15, DateTimeKind.Utc), targetWithoutOffsets.OccurredAtUtc);
                Assert.AreEqual(DateTimeKind.Utc, targetWithoutOffsets.OccurredAtUtc.Kind);
                Assert.AreEqual(new DateTime(2024, 1, 10, 11, 30, 15, DateTimeKind.Utc), targetWithoutOffsets.OccurredAtOffset);
                Assert.AreEqual(DateTimeKind.Utc, targetWithoutOffsets.OccurredAtOffset.Kind);
                Assert.AreEqual(new DateTimeOffset(2024, 1, 10, 12, 30, 15, TimeSpan.FromHours(1)), targetWithoutOffsets.RecordedAtOffset);
            }
            finally
            {
                Environment.SetEnvironmentVariable(ChillSharpInitOptions.SystemTimeZoneEnvironmentVariableName, originalEnvironmentValue);
                ChillSharpInitOptions.Initialize();
            }
        }

        [TestMethod]
        public void Step023_BuildPropertiesSerializesTemporalValuesWithSystemTimeZoneOnlyForDateTimeTypes()
        {
            var originalEnvironmentValue = Environment.GetEnvironmentVariable(ChillSharpInitOptions.SystemTimeZoneEnvironmentVariableName);

            try
            {
                Environment.SetEnvironmentVariable(ChillSharpInitOptions.SystemTimeZoneEnvironmentVariableName, "Europe/Rome");
                ChillSharpInitOptions.Initialize();

                using var db = TestApiHost.CreateDbContext();
                var source = new TemporalMappingEntity
                {
                    OccurredAtUtc = new DateTime(2024, 1, 10, 12, 30, 15, DateTimeKind.Utc),
                    OccurredAtOffset = new DateTime(2024, 1, 10, 12, 30, 15, DateTimeKind.Unspecified),
                    RecordedAtOffset = new DateTimeOffset(2024, 1, 10, 12, 30, 15, TimeSpan.FromHours(2)),
                    PublishedOn = new DateOnly(2024, 1, 10),
                    PublishedAt = new TimeOnly(23, 59, 58, 321)
                };

                var mapperType = typeof(ChillEngine).Assembly.GetType("ChillSharp.Dto.ChillDtoObjectMapper");
                Assert.IsNotNull(mapperType);

                var buildPropertiesMethod = mapperType.GetMethod(
                    "BuildProperties",
                    BindingFlags.Public | BindingFlags.Static);

                Assert.IsNotNull(buildPropertiesMethod);

                var properties = (Dictionary<string, object?>?)buildPropertiesMethod.Invoke(null,
                [
                    db,
                    source,
                    "Tests.TemporalMappingEntity",
                    typeof(TemporalMappingEntity).GetProperties(),
                    null,
                    null
                ]);

                Assert.IsNotNull(properties);

                var systemTimeZone = ChillSharpInitOptions.GetSystemTimeZone();
                var expectedDateTime = TimeZoneInfo.ConvertTime(new DateTimeOffset(source.OccurredAtUtc, TimeSpan.Zero), systemTimeZone)
                    .ToString("O", CultureInfo.InvariantCulture);
                var expectedUnspecifiedDateTime = new DateTimeOffset(source.OccurredAtOffset, systemTimeZone.GetUtcOffset(source.OccurredAtOffset))
                    .ToString("O", CultureInfo.InvariantCulture);
                Assert.AreEqual(expectedDateTime, properties["OccurredAtUtc"]);
                Assert.AreEqual(expectedUnspecifiedDateTime, properties["OccurredAtOffset"]);
                Assert.AreEqual(source.RecordedAtOffset.ToString("O", CultureInfo.InvariantCulture), properties["RecordedAtOffset"]);
                Assert.AreEqual(source.PublishedOn, properties["PublishedOn"]);
                Assert.AreEqual(source.PublishedAt, properties["PublishedAt"]);

                var serializedProperties = JsonSerializer.Serialize(properties);
                StringAssert.Contains(serializedProperties, "\"OccurredAtUtc\":\"2024-01-10T13:30:15.0000000\\u002B01:00\"");
                StringAssert.Contains(serializedProperties, "\"OccurredAtOffset\":\"2024-01-10T12:30:15.0000000\\u002B01:00\"");
                StringAssert.Contains(serializedProperties, "\"RecordedAtOffset\":\"2024-01-10T12:30:15.0000000\\u002B02:00\"");
                StringAssert.Contains(serializedProperties, "\"PublishedOn\":\"2024-01-10\"");
                StringAssert.Contains(serializedProperties, "\"PublishedAt\":\"23:59:58.3210000\"");
            }
            finally
            {
                Environment.SetEnvironmentVariable(ChillSharpInitOptions.SystemTimeZoneEnvironmentVariableName, originalEnvironmentValue);
                ChillSharpInitOptions.Initialize();
            }
        }

        [TestMethod]
        public void Step024_DefaultQueryOrderingUsesPosition()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"chillsharp-ordering-position-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<EF.DummyContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            using var db = new EF.DummyContext(options);
            db.Database.EnsureCreated();

            db.Post.Add(new Post
            {
                Guid = Guid.NewGuid(),
                Position = 20,
                Title = "Second",
                Author = "Author 2",
                FullTextContent = "Second",
                LastUpdateUser = string.Empty
            });
            db.Post.Add(new Post
            {
                Guid = Guid.NewGuid(),
                Position = 10,
                Title = "First",
                Author = "Author 1",
                FullTextContent = "First",
                LastUpdateUser = string.Empty
            });
            db.SaveChanges();

            var dtoEngine = new ChillDtoEngine(db);
            var result = dtoEngine.Query(new ChillSharp.Dto.ChillDtoQuery
            {
                ChillType = "Query.PostQuery",
                ResultProperties = ChillSharp.Dto.ChillDtoProperty.Build(["Guid", "Title"])
            });

            Assert.HasCount(2, result.Results);
            Assert.AreEqual("First", result.Results[0].Properties["Title"]?.ToString());
            Assert.AreEqual("Second", result.Results[1].Properties["Title"]?.ToString());
        }

        [TestMethod]
        public void Step025_QueryOrderingUsesReferenceLabelForEntityColumns()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"chillsharp-ordering-reference-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<EF.DummyContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            using var db = new EF.DummyContext(options);
            db.Database.EnsureCreated();

            var zBlog = new Blog
            {
                Guid = Guid.NewGuid(),
                Label = "Z Blog",
                Title = "Z Blog",
                Url = "https://z.local"
            };
            var aBlog = new Blog
            {
                Guid = Guid.NewGuid(),
                Label = "A Blog",
                Title = "A Blog",
                Url = "https://a.local"
            };

            db.Blog.AddRange(zBlog, aBlog);
            db.Post.Add(new Post
            {
                Guid = Guid.NewGuid(),
                Position = 0,
                Blog = zBlog,
                Title = "Post linked to Z",
                Author = "Author Z",
                FullTextContent = "Post linked to Z",
                LastUpdateUser = string.Empty
            });
            db.Post.Add(new Post
            {
                Guid = Guid.NewGuid(),
                Position = 0,
                Blog = aBlog,
                Title = "Post linked to A",
                Author = "Author A",
                FullTextContent = "Post linked to A",
                LastUpdateUser = string.Empty
            });
            db.SaveChanges();

            var dtoEngine = new ChillDtoEngine(db);
            var result = dtoEngine.Query(new ChillSharp.Dto.ChillDtoQuery
            {
                ChillType = "Query.PostQuery",
                Ordering = new ChillSharp.EF.ChillOrdering
                {
                    PropertyName = "Blog",
                    Direction = ChillSharp.EF.ChillOrdering.AscendingDirection
                },
                ResultProperties = ChillSharp.Dto.ChillDtoProperty.Build(["Guid", "Title", "Blog"])
            });

            Assert.HasCount(2, result.Results);
            Assert.AreEqual("Post linked to A", result.Results[0].Properties["Title"]?.ToString());
            Assert.AreEqual("Post linked to Z", result.Results[1].Properties["Title"]?.ToString());
        }

        [TestMethod]
        public void Step026_DtoQueryAcceptsEntityReferenceFilterOnUnmappedQueryObject()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"chillsharp-query-reference-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<EF.DummyContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            using var db = new EF.DummyContext(options);
            db.Database.EnsureCreated();

            var selectedBlog = new Blog
            {
                Guid = Guid.NewGuid(),
                Label = "Selected Blog",
                Title = "Selected Blog",
                Url = "https://selected.local"
            };
            var otherBlog = new Blog
            {
                Guid = Guid.NewGuid(),
                Label = "Other Blog",
                Title = "Other Blog",
                Url = "https://other.local"
            };

            db.Blog.AddRange(selectedBlog, otherBlog);
            db.Post.Add(new Post
            {
                Guid = Guid.NewGuid(),
                Position = 0,
                Blog = selectedBlog,
                Title = "Matching post",
                Author = "Author 1",
                FullTextContent = "Matching post",
                LastUpdateUser = string.Empty
            });
            db.Post.Add(new Post
            {
                Guid = Guid.NewGuid(),
                Position = 0,
                Blog = otherBlog,
                Title = "Other post",
                Author = "Author 2",
                FullTextContent = "Other post",
                LastUpdateUser = string.Empty
            });
            db.SaveChanges();

            var dtoEngine = new ChillDtoEngine(db);
            var result = dtoEngine.Query(new ChillSharp.Dto.ChillDtoQuery
            {
                ChillType = "Query.PostQuery",
                Properties = new Dictionary<string, object?>
                {
                    ["Blog"] = new ChillSharp.Dto.ChillDtoEntity
                    {
                        ChillType = "Model.Blog",
                        Guid = selectedBlog.Guid
                    }
                },
                ResultProperties = ChillSharp.Dto.ChillDtoProperty.Build(["Guid", "Title"])
            });

            Assert.HasCount(1, result.Results);
            Assert.AreEqual("Matching post", result.Results[0].Properties["Title"]?.ToString());
        }

        [TestMethod]
        public void Step027_DtoEntityMapsPositionDuringCreateAndUpdate()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"chillsharp-dto-position-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<EF.DummyContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            using var db = new EF.DummyContext(options);
            db.Database.EnsureCreated();

            var dtoEngine = new ChillDtoEngine(db);
            var created = dtoEngine.Create(new ChillSharp.Dto.ChillDtoEntity
            {
                ChillType = "Model.Post",
                Guid = Guid.NewGuid(),
                Position = 12,
                Properties = new Dictionary<string, object?>
                {
                    ["Title"] = "Positioned post",
                    ["Author"] = "Position author"
                }
            });

            Assert.AreEqual(12, created.Position);

            created.Position = 34;
            created.Properties["Title"] = "Positioned post updated";

            var updated = dtoEngine.Update(created);

            Assert.AreEqual(34, updated.Position);
            Assert.AreEqual("Positioned post updated", updated.Properties["Title"]?.ToString());
        }

        [TestMethod]
        public void Step028_AutocompleteSerializesEntityReferenceOnUnmappedQueryObject()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"chillsharp-query-autocomplete-reference-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<EF.DummyContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            using var db = new EF.DummyContext(options);
            db.Database.EnsureCreated();

            var selectedBlog = new Blog
            {
                Guid = Guid.NewGuid(),
                Label = "Autocomplete Blog",
                Title = "Autocomplete Blog",
                Url = "https://autocomplete-reference.local"
            };

            db.Blog.Add(selectedBlog);
            db.SaveChanges();

            var dtoEngine = new ChillDtoEngine(db);
            var result = dtoEngine.Autocomplete(new ChillSharp.Dto.ChillDtoQuery
            {
                ChillType = "Query.PostQuery",
                Properties = new Dictionary<string, object?>
                {
                    ["Blog"] = new ChillSharp.Dto.ChillDtoEntity
                    {
                        ChillType = "Model.Blog",
                        Guid = selectedBlog.Guid
                    }
                }
            });

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType<ChillSharp.Dto.ChillDtoEntity>(result.Properties["Blog"]);
            Assert.AreEqual(selectedBlog.Guid, ((ChillSharp.Dto.ChillDtoEntity)result.Properties["Blog"]!).Guid);
        }

        private sealed class NullableDateEntity
        {
            [ChillProperty]
            public DateOnly? PublishedOn { get; set; }
        }

        private sealed class TemporalMappingEntity
        {
            [ChillProperty]
            public DateTime OccurredAtUtc { get; set; }

            [ChillProperty]
            public DateTime OccurredAtOffset { get; set; }

            [ChillProperty]
            public DateTimeOffset RecordedAtOffset { get; set; }

            [ChillProperty]
            public DateOnly PublishedOn { get; set; }

            [ChillProperty]
            public TimeOnly PublishedAt { get; set; }
        }

        private sealed class InflateOnlyEntity
        {
            public int OnInflateCalls { get; private set; }

            [ChillProperty(CallOnInflate: true)]
            public string? ComputedTitle { get; set; }

            public void OnInflateProperty(string propertyName)
            {
                OnInflateCalls++;
                if (propertyName == nameof(ComputedTitle))
                {
                    ComputedTitle = propertyName;
                }
            }
        }

        private sealed class AutoPostQuery : ChillQuery, IChillQuery<Post>
        {
            IQueryable<Post> IChillQuery<Post>.OnQuery(IChillContext Context)
            {
                return OnQuery(Context).Cast<Post>();
            }

            IQueryable<Post> IChillQuery<Post>.OnOrderingBy(IChillContext Context, IQueryable<Post> Query)
            {
                return Query.OrderBy(x => x.Position).ThenBy(x => x.Guid);
            }

            IQueryable<Post> IChillQuery<Post>.OnPaginate(IChillContext Context, IQueryable<Post> Query)
            {
                if (Pagination == null)
                    return Query;

                return Query.Skip((Pagination.Page - 1) * Pagination.PageResults).Take(Pagination.PageResults);
            }
        }
    }
}
