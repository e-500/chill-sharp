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

namespace ChillSharp.Tests
{
    [TestClass]
    public sealed class Collections
    {
        /// <summary>
        /// Creates a blog without posts and verifies that the returned collection is empty.
        /// </summary>
        [TestMethod]
        public void Step001_CreateEntityWithAnEmptyCollection()
        {
            // Start the shared API host only once for the whole test run.
            TestApiHost.EnsureStarted(6002);

            // Use the HTTP client wrapper against the Chill endpoints.
            var cli = new ChillSharpClient("http://localhost:6002/api/chill");

            // Create a Blog entity without any Posts children.
            var e = new ChillDtoEntity();
            e.ChillType = "Model.Blog";
            e.Guid = Guid.NewGuid();
            e.Properties.Add("Title", "The BLOG!");
            e.Properties.Add("Url", "https://the-blog.com/");
            var cRes = cli.Create(e);
            Assert.IsNotNull(cRes);
            Assert.IsTrue(cRes.HasValue("Title"));
            Assert.AreEqual("The BLOG!", cRes.GetString("Title"));

            // Query the blog including its Posts collection and verify that it is empty.
            var q = new ChillDtoQuery();
            q.ChillType = "Query.BlogQuery";
            q.Properties.Add("Guid", cRes.Guid);
            q.ResultProperties = ChillDtoProperty.Build([
                "Guid",
                "Title",
                "Url",
                ChillDtoProperty.With("Posts", ["Guid", "Title"])
            ]);
            var qRes = cli.Query(q);
            Assert.IsNotNull(qRes);
            Assert.IsNotNull(qRes.Results);
            Assert.HasCount(1, qRes.Results);
            var qEntity = qRes.Results[0];
            Assert.IsNotNull(qEntity);
            Assert.IsTrue(qEntity.HasValue("Title"));
            Assert.AreEqual("The BLOG!", qEntity.GetString("Title"));
            var posts = qEntity.GetCollection("Posts");
            Assert.IsNotNull(posts);
            Assert.AreEqual(0, posts.Count());

            // Persist the Guid for the following collection steps.
            BlogGuid = qEntity.Guid;
        }

        private Guid? BlogGuid = null;

        /// <summary>
        /// Adds a post to the blog collection by setting the Blog reference during create.
        /// </summary>
        [TestMethod]
        public void Step002_AddEntityToCollection()
        {
            TestApiHost.EnsureStarted(6002);

            // Ensure a blog exists before linking a post to it.
            if (!BlogGuid.HasValue)
                Step001_CreateEntityWithAnEmptyCollection();

            Assert.IsNotNull(BlogGuid);

            // Load the parent Blog DTO so it can be used as the foreign-key reference.
            var cli = new ChillSharpClient("http://localhost:6002/api/chill");

            var q = new ChillDtoQuery();
            q.ChillType = "Query.BlogQuery";
            q.Properties.Add("Guid", BlogGuid.Value);
            var qBlog = cli.Query(q);
            Assert.IsNotNull(qBlog);
            Assert.IsNotNull(qBlog.Results);
            Assert.HasCount(1, qBlog.Results);
            var blog = qBlog.Results[0];

            // Create a Post and attach it to the Blog by sending the Blog DTO as reference.
            var e = new ChillDtoEntity();
            e.ChillType = "Model.Post";
            e.Guid = Guid.NewGuid();
            e.Properties.Add("Title", "Post title");
            e.Properties.Add("Blog", blog);
            var cRes = cli.Create(e);
            Assert.IsNotNull(cRes);
            Assert.IsTrue(cRes.HasValue("Title"));
            Assert.AreEqual("Post title", cRes.GetString("Title"));

            // Query the Blog with its Posts collection and verify that the child entity is linked.
            q = new ChillDtoQuery();
            q.ChillType = "Query.BlogQuery";
            q.Properties.Add("Guid", BlogGuid.Value);
            q.ResultProperties = ChillDtoProperty.Build([
                "Guid",
                "Title",
                "Url",
                ChillDtoProperty.With("Posts", ["Guid", "Title"])
            ]);
            qBlog = cli.Query(q);
            Assert.IsNotNull(qBlog);
            Assert.IsNotNull(qBlog.Results);
            Assert.HasCount(1, qBlog.Results);
            blog = qBlog.Results[0];
            Assert.IsNotNull(blog);
            Assert.IsTrue(blog.HasValue("Title"));
            Assert.AreEqual("The BLOG!", blog.GetString("Title"));
            var posts = blog.GetCollection("Posts");
            Assert.AreEqual(1, posts.Count());
            foreach (var post in posts)
            {
                Assert.IsNotNull(post);
                Assert.IsTrue(post.HasValue("Title"));
                Assert.AreEqual("Post title", post.GetString("Title"));
            }

            // Store the DTO with collection data for the remove step.
            BlogDtoWithCollection = blog;
        }

        ChillDtoEntity? BlogDtoWithCollection = null;

    }
}
