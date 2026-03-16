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
using ChillSharp.Tests.EF.Model;

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
