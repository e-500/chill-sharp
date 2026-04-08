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
using ChillSharp.Annotations;
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
using System.ComponentModel.DataAnnotations;
using ChillSharp.Schema.Contracts;
using ChillSharp.Schema;

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
            Assert.IsTrue(blogSchema.EnableMCP);
            Assert.AreEqual("Blog resource exposed to MCP clients.", blogSchema.MCPDescription);
            Assert.IsTrue(blogSchema.Properties.Select(x => x.Name).ToArray().Contains("Title"),
                "Blog schema properties don't contains 'Title'");
            Assert.AreEqual("Titolo del blog", blogSchema.Properties.Single(x => x.Name == "Title").DisplayName);
            Assert.AreEqual("Blog title used to identify the resource.", blogSchema.Properties.Single(x => x.Name == "Title").MCPDescription);

            var defaultBlogSchema = defaultCultureClient.GetSchema("Model.Blog", "default");
            Assert.IsNotNull(defaultBlogSchema, "GetSchema('Model.Blog', 'default') returned null");
            Assert.AreEqual("Blog title", defaultBlogSchema.Properties.Single(x => x.Name == "Title").DisplayName);

            var postSchema = cli.GetSchema("Model.Post", "default");
            Assert.IsNotNull(postSchema, "GetSchema('Model.Post', 'default') returned null");
            var authorProperty = postSchema.Properties.Where(x => x.Name == "Author").FirstOrDefault();
            var blogProperty = postSchema.Properties.Where(x => x.Name == "Blog").FirstOrDefault();
            Assert.IsNotNull(authorProperty, "Post schema properties don't contains 'Author' property");
            Assert.IsNotNull(blogProperty, "Post schema properties don't contains 'Blog' property");
            Assert.AreEqual("Model.Blog", blogProperty.ReferenceChillType);
            Assert.AreEqual("Query.BlogQuery", blogProperty.ReferenceChillTypeQuery);
            authorProperty.DisplayName = "Post author";
            postSchema.EnableMCP = true;
            postSchema.MCPDescription = "Post resource published through schema overrides.";
            postSchema.Metadata["schema-level"] = "enabled";
            authorProperty.Metadata["property-level"] = "visible";
            authorProperty.MCPDescription = "Author field exposed to MCP clients.";
            cli.SetSchema(postSchema);

            postSchema = cli.GetSchema("Model.Post", "default");
            Assert.IsNotNull(postSchema, "GetSchema('Model.Post', 'default') returned null");
            authorProperty = postSchema.Properties.Where(x => x.Name == "Author").FirstOrDefault();
            Assert.IsNotNull(authorProperty, "Post schema properties no more contains 'Author' property");
            Assert.AreEqual("Post author", authorProperty.DisplayName, "Persistance not working");
            Assert.IsTrue(postSchema.EnableMCP);
            Assert.AreEqual("Post resource published through schema overrides.", postSchema.MCPDescription);
            Assert.IsTrue(postSchema.Metadata.ContainsKey("schema-level"));
            Assert.AreEqual("enabled", postSchema.Metadata["schema-level"]);
            Assert.IsTrue(authorProperty.Metadata.ContainsKey("property-level"));
            Assert.AreEqual("visible", authorProperty.Metadata["property-level"]);
            Assert.AreEqual("Author field exposed to MCP clients.", authorProperty.MCPDescription);
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
            var resolverType = typeof(IChillContext).Assembly.GetType("ChillSharp.Dto.ChillTypeResolver");
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

            Assert.IsGreaterThanOrEqualTo(italianItems.Count, 4, "Expected at least Blog/Post entities and BlogQuery/PostQuery queries.");

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
            var dtoEngine = new ChillDtoEngine(context); //, schemaService);

            var defaultOptions = schemaService.GetEntityOptions("Model.Post");
            Assert.AreEqual("Model.Post", defaultOptions.ChillType);
            Assert.IsTrue(defaultOptions.ChecksumEnabled);
            Assert.IsFalse(defaultOptions.ChangeLogEnabled);
            Assert.IsFalse(defaultOptions.EnableMCP);
            Assert.AreEqual("Post resource exposed to MCP clients.", defaultOptions.MCPDescription);
            Assert.AreEqual("{Title} - {Author}", defaultOptions.LabelFormatString);
            Assert.AreEqual("{Title}", defaultOptions.ShortLabelFormatString);
            Assert.AreEqual("{Title} {Author}", defaultOptions.FullTextContentFormatString);

            var updatedOptions = schemaService.SetEntityOptionsAsync(new ChillDtoEntityOptions
            {
                ChillType = "Model.Post",
                ChecksumEnabled = false,
                LabelFormatString = "{Title} - {Author}",
                ShortLabelFormatString = "{Author}.{Title}",
                FullTextContentFormatString = "{Title}::{Author}",
                EnableMCP = true,
                MCPDescription = "Post MCP runtime description.",
                ChangeLogEnabled = true
            }).GetAwaiter().GetResult();

            Assert.AreEqual("Model.Post", updatedOptions.ChillType);
            Assert.IsFalse(updatedOptions.ChecksumEnabled);
            Assert.IsTrue(updatedOptions.ChangeLogEnabled);
            Assert.IsTrue(updatedOptions.EnableMCP);
            Assert.AreEqual("Post MCP runtime description.", updatedOptions.MCPDescription);
            Assert.AreEqual("{Title} - {Author}", updatedOptions.LabelFormatString);
            Assert.AreEqual("{Author}.{Title}", updatedOptions.ShortLabelFormatString);
            Assert.AreEqual("{Title}::{Author}", updatedOptions.FullTextContentFormatString);

            var persistedOptions = schemaService.GetEntityOptions("Model.Post");
            Assert.IsFalse(persistedOptions.ChecksumEnabled);
            Assert.IsTrue(persistedOptions.ChangeLogEnabled);
            Assert.IsTrue(persistedOptions.EnableMCP);
            Assert.AreEqual("Post MCP runtime description.", persistedOptions.MCPDescription);
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
        public async Task Step010_SchemaControllerEndpointsRespond()
        {
            var controller = await CreateSchemaController();

            Assert.IsInstanceOfType<OkObjectResult>(await controller.GetSchema("Model.Post", "default"));
            Assert.IsInstanceOfType<OkObjectResult>(controller.GetSchemaList());
            Assert.IsInstanceOfType<OkObjectResult>(await controller.SetSchema(new ChillDtoSchema { ChillType = "Model.Post", ChillViewCode = "default" }));
            Assert.IsInstanceOfType<OkObjectResult>(await controller.GetEntityOptions("Model.Post"));
            Assert.IsInstanceOfType<OkObjectResult>(await controller.SetEntityOptions(new ChillDtoEntityOptions { ChillType = "Model.Post" }));
            Assert.IsInstanceOfType<OkObjectResult>(await controller.GetMenu(cancellationToken: CancellationToken.None));
            var guid = Guid.NewGuid();
            Assert.IsInstanceOfType<OkObjectResult>(await controller.SetMenu(new ChillDtoMenuItem { Guid = guid, Title = "Menu", ComponentName = "CRUD", MenuHierarchy = "TEST" }, CancellationToken.None));
            Assert.IsInstanceOfType<NoContentResult>(await controller.DeleteMenu(guid, CancellationToken.None));
        }

        [TestMethod]
        public void Step011_PropertySchemaInfersReferenceQueryTypeOnlyWhenMatchingQueryExists()
        {
            var inferredProperty = typeof(FallbackReferenceHolder).GetProperty(nameof(FallbackReferenceHolder.InferredTarget));
            var blankProperty = typeof(FallbackReferenceHolder).GetProperty(nameof(FallbackReferenceHolder.BlogWithoutQuery));

            Assert.IsNotNull(inferredProperty);
            Assert.IsNotNull(blankProperty);

            var inferredSchema = ChillDtoPropertySchema.FromPropertyInfo(inferredProperty!, "ChillSharp.Tests");
            var blankSchema = ChillDtoPropertySchema.FromPropertyInfo(blankProperty!, "ChillSharp.Tests");

            Assert.AreEqual("Schemas+FallbackLookupTarget", inferredSchema.ReferenceChillType);
            Assert.AreEqual("Schemas+FallbackLookupTargetQuery", inferredSchema.ReferenceChillTypeQuery);
            Assert.AreEqual("EF.Model.Blog", blankSchema.ReferenceChillType);
            Assert.AreEqual(string.Empty, blankSchema.ReferenceChillTypeQuery);
        }

        [TestMethod]
        public void Step012_PropertySchemaMarksJsonFormattedStringsAsJsonType()
        {
            var property = typeof(JsonPayloadHolder).GetProperty(nameof(JsonPayloadHolder.Payload));

            Assert.IsNotNull(property);

            var schema = ChillDtoPropertySchema.FromPropertyInfo(property!, "ChillSharp.Tests");

            Assert.AreEqual(ChillDtoPropertyType.Json, schema.PropertyType);
            Assert.AreEqual("json", schema.CustomFormat);
        }


        [TestMethod]
        public async Task Step013_MenuEndpointsFilterByUserAndRoleHierarchy()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"chillsharp-schema-menu-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<EF.DummyContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            await using var context = new EF.DummyContext(options);
            await context.Database.EnsureCreatedAsync();

            var cache = new ChillSharp.Schema.ChillSchemaCache();
            var schemaService = new ChillSharp.Schema.ChillSchemaService(context, context, cache);
            var authService = new ChillAuthService(context, context, new ChillAuthManagementAccessCache());

            var sectionA = await schemaService.SetMenuAsync(new ChillDtoMenuItem
            {
                Title = "Section A",
                ComponentName = "CRUD",
                MenuHierarchy = "SECTION-A"
            });
            var sectionB = await schemaService.SetMenuAsync(new ChillDtoMenuItem
            {
                Title = "Section B",
                ComponentName = "CRUD",
                MenuHierarchy = "SECTION-B"
            });
            var sectionAChild = await schemaService.SetMenuAsync(new ChillDtoMenuItem
            {
                Title = "Section A Child",
                ComponentName = "CRUD",
                MenuHierarchy = "SECTION-A.CHILD",
                Parent = new ChillDtoMenuItem { Guid = sectionA.Guid }
            });

            var role = await authService.CreateRoleAsync(new ChillSharp.Auth.Contracts.CreateAuthRoleRequest
            {
                Name = $"menu-role-{Guid.NewGuid():N}",
                Description = "Menu role",
                MenuHierarchy = "SECTION-A"
            });

            var user = await authService.CreateUserAsync(new ChillSharp.Auth.Contracts.CreateAuthUserRequest
            {
                ExternalId = "menu-user",
                UserName = "menu-user",
                DisplayName = "Menu User",
                MenuHierarchy = string.Empty
            });

            await authService.AssignRoleAsync(user.Guid, role.Guid);

            var controller = new ChillSchemaController(
                context,
                schemaService,
                authService,
                new StubIdentityResolver());
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, "menu-user")
                    ], authenticationType: "Test"))
                }
            };

            var rootResult = (OkObjectResult)await controller.GetMenu(cancellationToken: CancellationToken.None);
            var rootItems = (IReadOnlyList<ChillDtoMenuItem>)rootResult.Value!;
            Assert.HasCount(1, rootItems);
            Assert.AreEqual(sectionA.Guid, rootItems[0].Guid);

            var childResult = (OkObjectResult)await controller.GetMenu(sectionA.Guid, CancellationToken.None);
            var childItems = (IReadOnlyList<ChillDtoMenuItem>)childResult.Value!;
            Assert.HasCount(1, childItems);
            Assert.AreEqual(sectionAChild.Guid, childItems[0].Guid);

            Assert.AreNotEqual(sectionB.Guid, rootItems[0].Guid);
        }

        [TestMethod]
        public async Task Step014_DeleteMenuRemovesDescendants()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"chillsharp-schema-delete-menu-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<EF.DummyContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            await using var context = new EF.DummyContext(options);
            await context.Database.EnsureCreatedAsync();

            var cache = new ChillSharp.Schema.ChillSchemaCache();
            var schemaService = new ChillSharp.Schema.ChillSchemaService(context, context, cache);

            var root = await schemaService.SetMenuAsync(new ChillDtoMenuItem
            {
                Title = "Root",
                ComponentName = "CRUD",
                MenuHierarchy = "ROOT"
            });
            var child = await schemaService.SetMenuAsync(new ChillDtoMenuItem
            {
                Title = "Child",
                ComponentName = "CRUD",
                MenuHierarchy = "ROOT.CHILD",
                Parent = new ChillDtoMenuItem { Guid = root.Guid }
            });
            await schemaService.SetMenuAsync(new ChillDtoMenuItem
            {
                Title = "Grandchild",
                ComponentName = "CRUD",
                MenuHierarchy = "ROOT.CHILD.GRANDCHILD",
                Parent = new ChillDtoMenuItem { Guid = child.Guid }
            });
            var sibling = await schemaService.SetMenuAsync(new ChillDtoMenuItem
            {
                Title = "Sibling",
                ComponentName = "CRUD",
                MenuHierarchy = "SIBLING"
            });

            await schemaService.DeleteMenuAsync(root.Guid, CancellationToken.None);

            var remainingRootItems = await schemaService.GetMenuAsync(cancellationToken: CancellationToken.None);
            Assert.HasCount(1, remainingRootItems);
            Assert.AreEqual(sibling.Guid, remainingRootItems[0].Guid);

            var deletedChildren = await schemaService.GetMenuAsync(root.Guid, CancellationToken.None);
            Assert.IsEmpty(deletedChildren);
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

        private async static Task<ChillSchemaController> CreateSchemaController()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"chillsharp-schemas-{Guid.NewGuid():N}.db");

            var options = new DbContextOptionsBuilder<EF.DummyContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            var context = new EF.DummyContext(options);
            await context.Database.EnsureCreatedAsync();
            var cache = new ChillSharp.Schema.ChillSchemaCache();

            var schemaService = new ChillSharp.Schema.ChillSchemaService(context, context, cache);

            var controller = new ChillSchemaController(context, schemaService);
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
            var controller = await CreateSchemaController();
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

        private sealed class StubSchemaService : IChillSchemaService
        {
            private readonly List<ChillDtoMenuItem> _menuItems = [];

            public Task<IChillDtoSchema?> GetSchemaAsync(string chillType, string chillViewCode, string? cultureName = null, CancellationToken cancellationToken = default)
                => Task.FromResult<IChillDtoSchema?>(new ChillDtoSchema { ChillType = chillType, ChillViewCode = chillViewCode });

            public Task<ChillDtoSchema> SetSchemaAsync(ChillDtoSchema schema, CancellationToken cancellationToken = default)
                => Task.FromResult(schema);

            public Task<ChillDtoEntityOptions> GetEntityOptionsAsync(string chillType, CancellationToken cancellationToken = default)
                => Task.FromResult(new ChillDtoEntityOptions { ChillType = chillType });

            public Task<ChillDtoEntityOptions> SetEntityOptionsAsync(ChillDtoEntityOptions entityOptions, CancellationToken cancellationToken = default)
                => Task.FromResult(entityOptions);

            public Task<IReadOnlyList<ChillDtoMenuItem>> GetMenuAsync(Guid? parentGuid = null, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<ChillDtoMenuItem>>(_menuItems.Where(x => x.Parent?.Guid == parentGuid).ToList());

            public Task<ChillDtoMenuItem> SetMenuAsync(ChillDtoMenuItem menuItem, CancellationToken cancellationToken = default)
            {
                if (menuItem.Guid == Guid.Empty)
                    menuItem.Guid = Guid.NewGuid();
                _menuItems.RemoveAll(x => x.Guid == menuItem.Guid);
                _menuItems.Add(menuItem);
                return Task.FromResult(menuItem);
            }
            public Task DeleteMenuAsync(Guid menuItemGuid, CancellationToken cancellationToken = default)
            {
                var pending = new Stack<Guid>();
                pending.Push(menuItemGuid);
                while (pending.Count > 0)
                {
                    var currentGuid = pending.Pop();
                    var childGuids = _menuItems.Where(x => x.Parent?.Guid == currentGuid).Select(x => x.Guid).ToList();
                    _menuItems.RemoveAll(x => x.Guid == currentGuid);
                    foreach (var childGuid in childGuids)
                    {
                        pending.Push(childGuid);
                    }
                }
                return Task.CompletedTask;
            }

            Task<ChillDtoSchema?> IChillSchemaService.GetSchemaAsync(string chillType, string chillViewCode, string? cultureName, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }
        private sealed class StubDtoEngine : IChillDtoEngine
        {
            public void BeginTransaction() => throw new NotSupportedException();
            public void CommitTransaction() => throw new NotSupportedException();
            public void RollbackTransaction() => throw new NotSupportedException();
            public ChillDtoQuery Query(ChillDtoQuery DtoQuery) => throw new NotSupportedException();
            public ChillDtoQuery Lookup(ChillDtoQuery DtoQuery) => throw new NotSupportedException();
            public ChillDtoEntity? Find(ChillDtoEntity DtoEntity) => throw new NotSupportedException();
            public ChillDtoEntity Create(ChillDtoEntity DtoEntity) => throw new NotSupportedException();
            public ChillDtoEntity Update(ChillDtoEntity DtoEntity) => throw new NotSupportedException();
            public void Delete(ChillDtoEntity DtoEntity) => throw new NotSupportedException();
            public ChillDtoEntity Autocomplete(ChillDtoEntity DtoEntity) => throw new NotSupportedException();
            public ChillDtoQuery Autocomplete(ChillDtoQuery DtoQuery) => throw new NotSupportedException();
            public IEnumerable<ChillValidationError> Validate(ChillDtoEntity DtoEntity) => throw new NotSupportedException();
            public IEnumerable<ChillValidationError> Validate(ChillDtoQuery DtoQuery) => throw new NotSupportedException();
            public ChillDtoSchema? GetSchema(string ChillType, string ChillViewCode, string? CultureName = null) => new() { ChillType = ChillType, ChillViewCode = ChillViewCode };
            public ChillDtoSchema SetSchema(ChillDtoSchema Schema) => Schema;
            public ChillDtoEntityOptions GetEntityOptions(string ChillType) => new() { ChillType = ChillType };
            public ChillDtoEntityOptions SetEntityOptions(ChillDtoEntityOptions EntityOptions) => EntityOptions;
        }

        private sealed class JsonPayloadHolder
        {
            [ChillProperty(CustomFormat = "json")]
            public string Payload { get; set; } = "{}";
        }

        public sealed class OpenGenericBlogQuery<Blog> : ChillQuery
        {
            public override IQueryable<IChillEntity> OnQuery(IChillContext Context)
            {
                return Array.Empty<IChillEntity>().AsQueryable();
            }
        }

        private sealed class FallbackLookupTarget : ChillEntity
        {
            [Key]
            public override Guid Guid { get; set; }
        }

        private sealed class FallbackLookupTargetQuery : ChillQuery
        {
            public override IQueryable<IChillEntity> OnQuery(IChillContext Context)
            {
                return Array.Empty<IChillEntity>().AsQueryable();
            }
        }

        private sealed class FallbackReferenceHolder
        {
            [ChillProperty]
            public FallbackLookupTarget? InferredTarget { get; set; }

            [ChillProperty]
            public Blog? BlogWithoutQuery { get; set; }
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







