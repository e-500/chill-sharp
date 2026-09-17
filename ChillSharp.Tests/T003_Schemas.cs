using ChillSharp.Api;
using ChillSharp.Client;
using ChillSharp.Client.Dto;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ChillSharp.Tests
{
    [TestClass]
    public sealed class Schemas
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
        public void Step001_TestSchema()
        {
            if (!_ApiServiceUpAndRunning)
                StartApiService();

            // Init client
            var cli = new ChillSharpClient("http://localhost:5000/api/chill");

            var blogSchema = cli.GetSchema("Model.Blog", "default");
            Assert.IsNotNull(blogSchema, "GetSchema('Model.Blog', 'default') returned null");
            Assert.IsTrue(blogSchema.Properties.Select(x => x.Name).ToArray().Contains("Title"),
                "Blog schema properties don't contains 'Title'");

            var postSchema = cli.GetSchema("Model.Post", "default");
            Assert.IsNotNull(postSchema, "GetSchema('Model.Post', 'default') returned null");
            // Check persistance
            var authorProperty = postSchema.Properties.Where(x => x.Name == "Author").FirstOrDefault();
            Assert.IsNotNull(authorProperty, "Post schema properties don't contains 'Author' property");
            authorProperty.DisplayName = "Post author";
            cli.SetSchema(postSchema);
            // Check result
            postSchema = cli.GetSchema("Model.Post", "default");
            Assert.IsNotNull(postSchema, "GetSchema('Model.Post', 'default') returned null");
            authorProperty = postSchema.Properties.Where(x => x.Name == "Author").FirstOrDefault();
            Assert.IsNotNull(authorProperty, "Post schema properties no more contains 'Author' property");
            Assert.AreEqual("Post author", authorProperty.DisplayName, "Persistance not working");

        }

    }
}
