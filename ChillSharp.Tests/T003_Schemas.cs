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
using ChillSharp.Tests.EF.Model;
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
            cli.SetSchema(postSchema);

            postSchema = cli.GetSchema("Model.Post", "default");
            Assert.IsNotNull(postSchema, "GetSchema('Model.Post', 'default') returned null");
            authorProperty = postSchema.Properties.Where(x => x.Name == "Author").FirstOrDefault();
            Assert.IsNotNull(authorProperty, "Post schema properties no more contains 'Author' property");
            Assert.AreEqual("Post author", authorProperty.DisplayName, "Persistance not working");
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
            Assert.IsNull(blogQuery.RelatedChillType);

            var defaultBlogQuery = defaultItems.Single(x => x.Type == "query" && x.ChillType == "Query.BlogQuery");
            Assert.AreEqual("Blog query", defaultBlogQuery.Name);
        }

        private sealed class TypedBlogQuery : IChillQuery<IChillEntity>, IChillQuery<Blog>
        {
            public Guid? Guid { get; set; }

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
