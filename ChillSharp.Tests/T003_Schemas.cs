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
using ChillSharp.Dto;
using ChillSharp.EF;
using ChillSharp.Auth.Api;
using ChillSharp.Auth.Services;
using ChillSharp.Schema.Api;
using ChillSharp.Schema.Api.Controllers;
using ChillSharp.Tests.EF.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using System.Reflection;

namespace ChillSharp.Tests
{
    [TestClass]
    public sealed class Schemas
    {
        [TestMethod]
        public void Step001_TestSchema()
        {
            TestApiHost.EnsureStarted();

            var cli = new ChillSharpClient("http://localhost:5000/api/chill", CultureName: "it-IT");
            var defaultCultureClient = new ChillSharpClient("http://localhost:5000/api/chill");

            var blogSchema = cli.GetSchema("Model.Blog", "default");
            Assert.IsNotNull(blogSchema, "GetSchema('Model.Blog', 'default') returned null");
            Assert.IsTrue(blogSchema.Properties.Select(x => x.Name).ToArray().Contains("Title"),
                "Blog schema properties don't contains 'Title'");
            Assert.AreEqual("Titolo del blog", blogSchema.Properties.Single(x => x.Name == "Title").DisplayName);

            var defaultBlogSchema = defaultCultureClient.GetSchema("Model.Blog", "default");
            Assert.IsNotNull(defaultBlogSchema, "GetSchema('Model.Blog', 'default') returned null");
            Assert.AreEqual("Blog title", defaultBlogSchema.Properties.Single(x => x.Name == "Title").DisplayName);

            var postSchema = cli.GetSchema("Model.Post", "default");
            Assert.IsNotNull(postSchema, "GetSchema('Model.Post', 'default') returned null");
            var authorProperty = postSchema.Properties.Where(x => x.Name == "Author").FirstOrDefault();
            Assert.IsNotNull(authorProperty, "Post schema properties don't contains 'Author' property");
            authorProperty.DisplayName = "Post author";
            postSchema.Metadata["schema-level"] = "enabled";
            authorProperty.Metadata["property-level"] = "visible";
            cli.SetSchema(postSchema);

            postSchema = cli.GetSchema("Model.Post", "default");
            Assert.IsNotNull(postSchema, "GetSchema('Model.Post', 'default') returned null");
            authorProperty = postSchema.Properties.Where(x => x.Name == "Author").FirstOrDefault();
            Assert.IsNotNull(authorProperty, "Post schema properties no more contains 'Author' property");
            Assert.AreEqual("Post author", authorProperty.DisplayName, "Persistance not working");
            Assert.IsTrue(postSchema.Metadata.ContainsKey("schema-level"));
            Assert.AreEqual("enabled", postSchema.Metadata["schema-level"]);
            Assert.IsTrue(authorProperty.Metadata.ContainsKey("property-level"));
            Assert.AreEqual("visible", authorProperty.Metadata["property-level"]);
        }

        [TestMethod]
        public void Step002_ContextCulturesDrivePrimaryAndSecondaryLabelResolution()
        {
            var italianAwareContext = new TestChillContext("ChillSharp.Tests.EF", "en-GB", "it-IT", "it-IT");
            var defaultOnlyContext = new TestChillContext("ChillSharp.Tests.EF", "en-GB", "de-DE", "en-GB");

            var italianSchema = ChillDtoSchema.FromIChillEntity(new Blog(), "default", italianAwareContext.GetChillTypePrefix(), italianAwareContext);
            var defaultSchema = ChillDtoSchema.FromIChillEntity(new Blog(), "default", defaultOnlyContext.GetChillTypePrefix(), defaultOnlyContext);
            var explicitPrimarySchema = ChillDtoSchema.FromIChillEntity(new Blog(), "default", italianAwareContext.GetChillTypePrefix(), italianAwareContext, "en-GB");

            var italianTitle = italianSchema.Properties.Single(x => x.Name == "Title");
            var defaultTitle = defaultSchema.Properties.Single(x => x.Name == "Title");
            var explicitPrimaryTitle = explicitPrimarySchema.Properties.Single(x => x.Name == "Title");

            Assert.AreEqual("Titolo del blog", italianTitle.DisplayName);
            Assert.AreEqual("Blog title", defaultTitle.DisplayName);
            Assert.AreEqual("Blog title", explicitPrimaryTitle.DisplayName);
        }

        [TestMethod]
        public void Step003_QuerySchemaExposesConcreteRelatedChillType()
        {
            var context = new TestChillContext("ChillSharp.Tests.EF", "en-GB", "it-IT", "en-GB");
            IChillQuery<IChillEntity> query = new TypedBlogQuery();

            var schema = ChillDtoSchema.FromIChillQuery(query, "default", context.GetChillTypePrefix(), context);

            Assert.AreEqual("Model.Blog", schema.QueryRelatedChillType);
        }

        [TestMethod]
        public void Step004_OpenGenericQueryDefinitionInfersConcreteRelatedChillType()
        {
            var method = typeof(ChillDtoSchemaListItem).GetMethod("FromQueryType", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "Unable to locate ChillDtoSchemaListItem.FromQueryType");

            var item = (ChillDtoSchemaListItem?)method.Invoke(null, new object?[] { typeof(OpenGenericBlogQuery<>), "ChillSharp.Tests.EF", null, null });

            Assert.IsNotNull(item);
            Assert.AreEqual("Model.Blog", item.RelatedChillType);
        }

        [TestMethod]
        public void Step005_OpenGenericQueryDefinitionCanBeActivated()
        {
            var resolverType = typeof(ChillDtoSchema).Assembly.GetType("ChillSharp.Dto.ChillTypeResolver");
            Assert.IsNotNull(resolverType, "Unable to locate ChillTypeResolver");

            var method = resolverType.GetMethod("ActivateType", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "Unable to locate ChillTypeResolver.ActivateType");

            var activated = method.Invoke(null, new object?[] { typeof(Schemas).Assembly, "ChillSharp.Tests.Schemas+OpenGenericBlogQuery", "ChillSharp.Tests" });

            Assert.IsNotNull(activated);
            Assert.IsInstanceOfType<IChillQuery<IChillEntity>>(activated);
            Assert.AreEqual(typeof(OpenGenericBlogQuery<ChillSharp.Tests.EF.Model.Blog>), activated.GetType());
        }

        [TestMethod]
        public void Step006_GetSchemaListReturnsRegisteredEntitiesAndQueries()
        {
            TestApiHost.EnsureStarted();

            var italianClient = new ChillSharpClient("http://localhost:5000/api/chill", CultureName: "it-IT");
            var defaultClient = new ChillSharpClient("http://localhost:5000/api/chill");

            var italianItems = italianClient.GetSchemaList();
            var defaultItems = defaultClient.GetSchemaList();

            Assert.IsTrue(italianItems.Count >= 4, "Expected at least Blog/Post entities and BlogQuery/PostQuery queries.");

            var blogEntity = italianItems.Single(x => x.Type == "entity" && x.ChillType == "Model.Blog");
            Assert.AreEqual("Blog", blogEntity.Name);
            Assert.AreEqual("Model.Blog", blogEntity.RelatedChillType);

            var blogQuery = italianItems.Single(x => x.Type == "query" && x.ChillType == "Query.BlogQuery");
            Assert.AreEqual("Ricerca Blog", blogQuery.Name);
            Assert.AreEqual("Model.Blog", blogQuery.RelatedChillType);

            var defaultBlogQuery = defaultItems.Single(x => x.Type == "query" && x.ChillType == "Query.BlogQuery");
            Assert.AreEqual("Blog query", defaultBlogQuery.Name);
        }

        [TestMethod]
        public async Task Step007_EntityOptionsCanBeReadAndPersisted()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"chillsharp-schema-options-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<EF.DummyContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            await using var context = new EF.DummyContext(options);
            await context.Database.EnsureCreatedAsync();
            var cache = new ChillSharp.Schema.ChillSchemaCache();
            var schemaService = new ChillSharp.Schema.ChillSchemaService(context, context, cache);
            var dtoEngine = new ChillDtoEngine(context, schemaService);

            var defaultOptions = dtoEngine.GetEntityOptions("Model.Post");
            Assert.AreEqual("Model.Post", defaultOptions.ChillType);
            Assert.IsTrue(defaultOptions.ChecksumEnabled);
            Assert.IsFalse(defaultOptions.ChangeLogEnabled);
            Assert.IsNull(defaultOptions.LabelFormatString);
            Assert.IsNull(defaultOptions.ShortLabelFormatString);
            Assert.IsNull(defaultOptions.FullTextContentFormatString);

            var updatedOptions = dtoEngine.SetEntityOptions(new ChillDtoEntityOptions
            {
                ChillType = "Model.Post",
                ChecksumEnabled = false,
                LabelFormatString = "{Title} - {Author}",
                ShortLabelFormatString = "{Author}.{Title}",
                FullTextContentFormatString = "{Title}::{Author}",
                ChangeLogEnabled = true
            });

            Assert.AreEqual("Model.Post", updatedOptions.ChillType);
            Assert.IsFalse(updatedOptions.ChecksumEnabled);
            Assert.IsTrue(updatedOptions.ChangeLogEnabled);
            Assert.AreEqual("{Title} - {Author}", updatedOptions.LabelFormatString);
            Assert.AreEqual("{Author}.{Title}", updatedOptions.ShortLabelFormatString);
            Assert.AreEqual("{Title}::{Author}", updatedOptions.FullTextContentFormatString);

            var persistedOptions = dtoEngine.GetEntityOptions("Model.Post");
            Assert.IsFalse(persistedOptions.ChecksumEnabled);
            Assert.IsTrue(persistedOptions.ChangeLogEnabled);
            Assert.AreEqual("{Title} - {Author}", persistedOptions.LabelFormatString);
            Assert.AreEqual("{Author}.{Title}", persistedOptions.ShortLabelFormatString);
            Assert.AreEqual("{Title}::{Author}", persistedOptions.FullTextContentFormatString);
        }

        [TestMethod]
        public async Task Step008_SchemaManagementEndpointsRequireCanManageSchemaPermission()
        {
            var result = await ExecuteSchemaAccessFilterAsync(false);
            Assert.IsInstanceOfType<ForbidResult>(result);
        }

        [TestMethod]
        public async Task Step009_SchemaManagementEndpointsAllowCanManageSchemaPermission()
        {
            var result = await ExecuteSchemaAccessFilterAsync(true);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void Step010_SchemaControllerEndpointsRespond()
        {
            var controller = CreateSchemaController();

            Assert.IsInstanceOfType<OkObjectResult>(controller.GetSchema("Model.Post", "default"));
            Assert.IsInstanceOfType<OkObjectResult>(controller.GetSchemaList());
            Assert.IsInstanceOfType<OkObjectResult>(controller.SetSchema(new ChillDtoSchema { ChillType = "Model.Post", ChillViewCode = "default" }));
            Assert.IsInstanceOfType<OkObjectResult>(controller.GetEntityOptions("Model.Post"));
            Assert.IsInstanceOfType<OkObjectResult>(controller.SetEntityOptions(new ChillDtoEntityOptions { ChillType = "Model.Post" }));
        }

        private sealed class TypedBlogQuery : IChillQuery<IChillEntity>, IChillQuery<Blog>
        {
            public Guid? Guid { get; set; }

            public string FullTextSearch { get; set; } = string.Empty;

            public ChillPagination? Pagination { get; set; }

            public IQueryable<IChillEntity> OnPaginate(IChillContext Context, IQueryable<IChillEntity> Query)
            {
                return Query;
            }

            public IQueryable<IChillEntity> OnQuery(IChillContext Context)
            {
                return Array.Empty<IChillEntity>().AsQueryable();
            }

            public IQueryable<IChillEntity> OnSort(IChillContext Context, IQueryable<IChillEntity> Query)
            {
                return Query;
            }

            IQueryable<Blog> IChillQuery<Blog>.OnPaginate(IChillContext Context, IQueryable<Blog> Query)
            {
                return Query;
            }

            IQueryable<Blog> IChillQuery<Blog>.OnQuery(IChillContext Context)
            {
                return Array.Empty<Blog>().AsQueryable();
            }

            IQueryable<Blog> IChillQuery<Blog>.OnSort(IChillContext Context, IQueryable<Blog> Query)
            {
                return Query;
            }
        }

        private static ChillSchemaController CreateSchemaController()
        {
            var controller = new ChillSchemaController(new StubDtoEngine(), new TestChillContext("ChillSharp.Tests.EF", "en-GB", "it-IT", "en-GB"));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            return controller;
        }

        private static async Task<IActionResult?> ExecuteSchemaAccessFilterAsync(bool allowSchemaManagement)
        {
            var filter = new ChillSchemaManagementAccessFilter(
                new StubManagementAccessService(allowSchemaManagement),
                new StubIdentityResolver());
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "schema-tester")
                ], authenticationType: "Test"))
            };

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var filters = new List<IFilterMetadata>();
            var actionArguments = new Dictionary<string, object?>();
            var controller = CreateSchemaController();
            var executingContext = new ActionExecutingContext(actionContext, filters, actionArguments, controller);

            await filter.OnActionExecutionAsync(executingContext, () =>
            {
                var executedContext = new ActionExecutedContext(actionContext, filters, controller);
                return Task.FromResult(executedContext);
            });

            return executingContext.Result;
        }

        private sealed class StubManagementAccessService : IChillAuthManagementAccessService
        {
            private readonly bool _allowSchemaManagement;

            public StubManagementAccessService(bool allowSchemaManagement)
            {
                _allowSchemaManagement = allowSchemaManagement;
            }

            public Task<bool> HasCapabilityAsync(string externalId, ChillAuthManagementCapability capability, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(capability == ChillAuthManagementCapability.Schema && _allowSchemaManagement);
            }

            public void Invalidate(string externalId) { }

            public void InvalidateAll() { }
        }

        private sealed class StubIdentityResolver : IChillAuthIdentityResolver
        {
            public string? ResolveExternalId(ClaimsPrincipal principal)
            {
                return principal.FindFirstValue(ClaimTypes.NameIdentifier);
            }
        }

        private sealed class StubDtoEngine : IChillDtoEngine
        {
            public void BeginTransaction() => throw new NotSupportedException();
            public void CommitTransaction() => throw new NotSupportedException();
            public void RollbackTransaction() => throw new NotSupportedException();
            public ChillDtoQuery Query(ChillDtoQuery DtoQuery) => throw new NotSupportedException();
            public ChillDtoEntity? Find(ChillDtoEntity DtoEntity) => throw new NotSupportedException();
            public ChillDtoEntity Create(ChillDtoEntity DtoEntity) => throw new NotSupportedException();
            public ChillDtoEntity Update(ChillDtoEntity DtoEntity) => throw new NotSupportedException();
            public void Delete(ChillDtoEntity DtoEntity) => throw new NotSupportedException();
            public ChillDtoSchema? GetSchema(string ChillType, string ChillViewCode, string? CultureName = null) => new() { ChillType = ChillType, ChillViewCode = ChillViewCode };
            public ChillDtoSchema SetSchema(ChillDtoSchema Schema) => Schema;
            public ChillDtoEntityOptions GetEntityOptions(string ChillType) => new() { ChillType = ChillType };
            public ChillDtoEntityOptions SetEntityOptions(ChillDtoEntityOptions EntityOptions) => EntityOptions;
        }

        public sealed class OpenGenericBlogQuery<Blog> : ChillQuery
        {
            public override IQueryable<IChillEntity> OnQuery(IChillContext Context)
            {
                return Array.Empty<IChillEntity>().AsQueryable();
            }
        }

        private sealed class TestChillContext : IChillContext
        {
            private readonly string _typePrefix;
            private readonly string _primaryCultureName;
            private readonly string _secondaryCultureName;
            private readonly string _defaultUserCultureName;

            public TestChillContext(string typePrefix, string primaryCultureName, string secondaryCultureName, string defaultUserCultureName)
            {
                _typePrefix = typePrefix;
                _primaryCultureName = primaryCultureName;
                _secondaryCultureName = secondaryCultureName;
                _defaultUserCultureName = defaultUserCultureName;
            }

            public string GetChillTypePrefix()
            {
                return _typePrefix;
            }

            public string GetPrimaryCultureName()
            {
                return _primaryCultureName;
            }

            public string GetSecondaryCultureName()
            {
                return _secondaryCultureName;
            }

            public string GetDefaultUserCultureName()
            {
                return _defaultUserCultureName;
            }
        }
    }
}


