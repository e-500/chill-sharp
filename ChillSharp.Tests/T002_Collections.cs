using ChillSharp.Api;
using ChillSharp.Client;
using ChillSharp.Client.Dto;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ChillSharp.Tests
{
    [TestClass]
    public sealed class Collections
    {
        private void StartApiService()
        {
            var apiServer = Task.Run(() =>
            {
                var ctx = new EF.DummyContext();
                ctx.Database.Migrate();
                var builder = WebApplication.CreateBuilder(new string[0]);
                builder.Services.AddDbContext<EF.DummyContext>(options =>
                        options.UseSqlite($"Data Source={ctx.DbPath}"));
                builder.Services.AddChillApi<EF.DummyContext>();
                var app = builder.Build();
                app.MapChillApi();
                app.Run();
            });
            apiServer.Wait(5000);
            _ApiServiceUpAndRunning = true;
        }

        private bool _ApiServiceUpAndRunning = false;

        [TestMethod]
        public void Step001_CreateEntityWithAnEmptyCollection()
        {
            if (!_ApiServiceUpAndRunning)
                StartApiService();

            // Init client
            var cli = new ChillSharpClient("http://localhost:5000/api/chill");

            // Create an entity with an empty collection
            var e = new ChillDtoEntity();
            e.ChillType = "Model.Blog";
            e.Guid = Guid.NewGuid();
            e.Properties.Add("Title", "The BLOG!");
            e.Properties.Add("Url", "https://the-blog.com/");
            var cRes = cli.Create(e);
            Assert.IsNotNull(cRes);
            Assert.IsTrue(cRes.HasValue("Title"));
            Assert.AreEqual("The BLOG!", cRes.GetString("Title"));

            // Check if that newly created entity has been created
            var q = new ChillDtoQuery();
            q.ChillType = "Query.BlogQuery";
            q.Properties.Add("Guid", cRes.Guid);
            q.ResultProperties = ChillDtoProperty.Build([
                "Guid",
                "Title",
                "Url",
                // Request also the Posts collection property with sub properties
                ChillDtoProperty.With("Posts", ["Guid", "Title"])
                ]);
            var qRes = cli.Query(q);
            Assert.IsNotNull(qRes);
            Assert.IsNotNull(qRes.Results);
            Assert.AreEqual(1, qRes.Results.Count);
            var qEntity = qRes.Results[0];
            Assert.IsNotNull(qEntity);
            Assert.IsTrue(qEntity.HasValue("Title"));
            Assert.AreEqual("The BLOG!", qEntity.GetString("Title"));
            // Check if collection is empty
            var posts = qEntity.GetCollection("Posts");
            Assert.IsNotNull(posts);
            Assert.AreEqual(0, posts.Count());

            // Save data for the upcoming tests
            BlogGuid = qEntity.Guid;
        }

        private Guid? BlogGuid = null;

        [TestMethod]
        public void Step002_AddEntityToCollection()
        {
            if (!_ApiServiceUpAndRunning)
                StartApiService();

            if (!BlogGuid.HasValue)
                Step001_CreateEntityWithAnEmptyCollection();

            // Test initial state for the test
            Assert.IsNotNull(BlogGuid);

            // Init client
            var cli = new ChillSharpClient("http://localhost:5000/api/chill");
            
            // Get FK reference
            var q = new ChillDtoQuery();
            q.ChillType = "Query.BlogQuery";
            q.Properties.Add("Guid", BlogGuid.Value);
            var qBlog = cli.Query(q);
            Assert.IsNotNull(qBlog);
            Assert.IsNotNull(qBlog.Results);
            Assert.AreEqual(1, qBlog.Results.Count);
            var blog = qBlog.Results[0];

            // Create entity specifying directly the FK to link it to collection directly on create
            var e = new ChillDtoEntity();
            e.ChillType = "Model.Post";
            e.Guid = Guid.NewGuid();
            e.Properties.Add("Title", "Post title");
            e.Properties.Add("Blog", blog);
            var cRes = cli.Create(e);
            Assert.IsNotNull(cRes);
            Assert.IsTrue(cRes.HasValue("Title"));
            Assert.AreEqual("Post title", cRes.GetString("Title"));

            // Get the entity with the collection to test if the newly create entity has be linked to the collection
            q = new ChillDtoQuery();
            q.ChillType = "Query.BlogQuery";
            q.Properties.Add("Guid", BlogGuid.Value);
            q.ResultProperties = ChillDtoProperty.Build([
                "Guid",
                "Title",
                "Url",
                // Request also the Posts collection property with sub properties
                ChillDtoProperty.With("Posts", ["Guid", "Title"])
                ]);
            qBlog = cli.Query(q);
            Assert.IsNotNull(qBlog);
            Assert.IsNotNull(qBlog.Results);
            Assert.AreEqual(1, qBlog.Results.Count);
            blog = qBlog.Results[0];
            Assert.IsNotNull(blog);
            Assert.IsTrue(blog.HasValue("Title"));
            Assert.AreEqual("The BLOG!", blog.GetString("Title"));
            // Check collection count
            var posts = blog.GetCollection("Posts");
            Assert.AreEqual(1, posts.Count());
            foreach (var post in posts)
            {
                // Check if linked entity is the correct one
                Assert.IsNotNull(post); 
                Assert.IsTrue(post.HasValue("Title"));
                Assert.AreEqual("Post title", post.GetString("Title"));
            }

            // Save data for the upcoming tests
            BlogDtoWithCollection = blog;
        }

        ChillDtoEntity? BlogDtoWithCollection = null;

        [TestMethod]
        public void Step003_RemoveEntityFromCollection()
        {
            if (!_ApiServiceUpAndRunning)
                StartApiService();

            if (!BlogGuid.HasValue)
                Step002_AddEntityToCollection();

            // Test initial state for the test
            Assert.IsNotNull(BlogDtoWithCollection);
            Assert.AreEqual(1, BlogDtoWithCollection.GetCollection("Posts").Count());

            // Init client
            var cli = new ChillSharpClient("http://localhost:5000/api/chill");

            // Get and test collection element
            var post = BlogDtoWithCollection.GetCollection("Posts").First();
            Assert.IsNotNull(post);
            Assert.IsTrue(post.HasValue("Title"));
            Assert.AreEqual("Post title", post.GetString("Title"));

            // Replace collection with an empty one (aka remove element)
            BlogDtoWithCollection.Properties["Posts"] = new List<ChillDtoEntity>();
            // Update the entity with the empty collection
            var blogDtoWithEmptyCollection = cli.Update(BlogDtoWithCollection);
            Assert.IsNotNull(blogDtoWithEmptyCollection);
            // Check if collection is empty after save
            Assert.AreEqual(0, blogDtoWithEmptyCollection.GetCollection("Posts").Count());

            // Select removed entity to check if is still present with null FK
            var q = new ChillDtoQuery();
            q.ChillType = "Query.PostQuery";
            q.Properties.Add("Guid", post.Guid);
            q.ResultProperties = ChillDtoProperty.Build(["Guid", "Blog", "Title"]);
            var qr = cli.Query(q);
            Assert.IsNotNull(qr);
            Assert.IsNotNull(qr.Results);
            Assert.AreEqual(1, qr.Results.Count);
            var qPost = qr.Results[0];

            Assert.IsNotNull(qPost);
            Assert.IsFalse(qPost.HasValue("Blog")); // Null FK

            // Remove entity unlinked from Posts collection
            cli.Delete(qPost);

            // Test if entity previously linked in the collection has been removed successfully
            q = new ChillDtoQuery();
            q.ChillType = "Query.PostQuery";
            q.Properties.Add("Guid", post.Guid);
            q.ResultProperties = ChillDtoProperty.Build(["Guid", "Blog", "Title"]);
            qr = cli.Query(q);
            Assert.IsNotNull(qr);
            Assert.IsNotNull(qr.Results);
            Assert.AreEqual(0, qr.Results.Count);

            // No entity, No errors => ok
        }
    }
}
