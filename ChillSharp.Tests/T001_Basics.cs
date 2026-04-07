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
            Assert.IsNotNull(createdPost.LastUpdateUtc);

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
            Assert.IsTrue(updatedPost.LastUpdateUtc.HasValue && initialLastUpdateUtc.HasValue && updatedPost.LastUpdateUtc.Value > initialLastUpdateUtc.Value);
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

            await schemaService.SetEntityOptionsAsync(new ChillSharp.Schema.Contracts.ChillDtoEntityOptions
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

            await schemaService.SetEntityOptionsAsync(new ChillSharp.Schema.Contracts.ChillDtoEntityOptions
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

        [TestMethod]
        public void Step009_QueryFullTextSearchSplitsTokensAndUsesAndLogic()
        {
            TestApiHost.EnsureStarted();

            var client = new ChillSharpClient("http://localhost:5000/api/chill");
            var tokenA = $"alpha-{Guid.NewGuid():N}";
            var tokenB = $"beta-{Guid.NewGuid():N}";

            client.SetEntityOptions(new ChillDtoEntityOptions
            {
                ChillType = "Model.Post",
                FullTextContentFormatString = "{Title} {Author}"
            });

            client.Create(new ChillDtoEntity
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
            query.Properties.Add("FullTextSearch", $"{tokenA} {tokenB}");

            var result = client.Query(query);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Results);
            Assert.AreEqual(1, result.Results.Count);
            Assert.AreEqual(tokenA, result.Results[0].GetString("Title"));
            Assert.AreEqual(tokenB, result.Results[0].GetString("Author"));
        }

        [TestMethod]
        public void Step010_AutocompleteEntityReturnsUpdatedFields()
        {
            TestApiHost.EnsureStarted();

            var client = new ChillSharpClient("http://localhost:5000/api/chill");
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
            TestApiHost.EnsureStarted();

            var client = new ChillSharpClient("http://localhost:5000/api/chill");
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
            TestApiHost.EnsureStarted();

            var client = new ChillSharpClient("http://localhost:5000/api/chill");
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
            TestApiHost.EnsureStarted();

            var client = new ChillSharpClient("http://localhost:5000/api/chill");
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
            TestApiHost.EnsureStarted();

            var client = new ChillSharpClient("http://localhost:5000/api/chill");
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
            TestApiHost.EnsureStarted();

            var client = new ChillSharpClient("http://localhost:5000/api/chill");
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
            Assert.AreEqual(1, result.Results.Count);
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

            Assert.AreEqual(1, results.Count);
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

        private sealed class NullableDateEntity
        {
            [ChillProperty]
            public DateOnly? PublishedOn { get; set; }
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

            IQueryable<Post> IChillQuery<Post>.OnSort(IChillContext Context, IQueryable<Post> Query)
            {
                return Query.OrderBy(x => x.Guid);
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
