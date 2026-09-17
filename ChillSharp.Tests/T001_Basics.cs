using ChillSharp.Api;
using ChillSharp.Client;
using ChillSharp.Client.Dto;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ChillSharp.Tests
{
    [TestClass]
    public sealed class Basics
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
        public void Step001_AddEntity()
        {
            if (!_ApiServiceUpAndRunning)
                StartApiService();

            // Init client
            var cli = new ChillSharpClient("http://localhost:5000/api/chill");

            // Create a new entity
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

            // Check if newly created entity has been created
            var q = new ChillDtoQuery();
            q.ChillType = "Query.PostQuery";
            q.Properties.Add("Guid", cRes.Guid);
            q.ResultProperties = ChillSharp.Client.Dto.ChillDtoProperty.FromStrings(new string[] { "Guid", "Title", "Author" });
            var qRes = cli.Query(q);
            Assert.IsNotNull(qRes);
            Assert.IsNotNull(qRes.Results);
            Assert.AreEqual(1, qRes.Results.Count);
            var qEntity = qRes.Results[0];
            Assert.IsNotNull(qEntity);
            Assert.IsTrue(qEntity.HasValue("Title"));
            Assert.AreEqual("New Title", qEntity.GetString("Title"));
            Assert.AreEqual("William Shakespeare", cRes.GetString("Author"));

            // Save data for the upcoming tests
            PostGuid = qEntity.Guid;
        }

        private Guid? PostGuid = null;

        [TestMethod]
        public void Step002_UpdateEntity()
        {
            if (!_ApiServiceUpAndRunning)
                StartApiService();

            if (!PostGuid.HasValue)
                Step001_AddEntity();

            // Test initial state for the test
            Assert.IsNotNull(PostGuid);

            // Init client
            var cli = new ChillSharpClient("http://localhost:5000/api/chill");

            // Create an empty mock with a Guid of the entity to delete
            var e = new ChillDtoEntity();
            e.ChillType = "Model.Post";
            e.Guid = PostGuid.Value;
            e.Properties.Add("Title", "Title changed");
            // Update entity
            cli.Update(e);

            // Check if entity has been updated and "Author" fields remained unchanged
            var q = new ChillDtoQuery();
            q.ChillType = "Query.PostQuery";
            q.Properties.Add("Guid", PostGuid.Value);
            q.ResultProperties = ChillSharp.Client.Dto.ChillDtoProperty.FromStrings(new string[] { "Guid", "Title", "Author" });
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

        [TestMethod]
        public void Step003_DeleteEntity()
        {
            if (!_ApiServiceUpAndRunning)
                StartApiService();

            if (!PostGuid.HasValue)
                Step002_UpdateEntity();

            // Test initial state for the test
            Assert.IsNotNull(PostGuid);

            // Init client
            var cli = new ChillSharpClient("http://localhost:5000/api/chill");
            
            // Create an empty mock with a Guid of the entity to delete
            var e = new ChillDtoEntity();
            e.ChillType = "Model.Post";
            e.Guid = PostGuid.Value;
            // Delete entity
            cli.Delete(e);

            // Check if entity has been deleted
            var q = new ChillDtoQuery();
            q.ChillType = "Query.PostQuery";
            q.Properties.Add("Guid", PostGuid.Value);
            var qRes = cli.Query(q);
            Assert.IsNotNull(qRes);
            Assert.IsNotNull(qRes.Results);
            Assert.AreEqual(0, qRes.Results.Count);
        }

        [TestMethod]
        public void Step004_FindEntity()
        {
            if (!_ApiServiceUpAndRunning)
                StartApiService();

            if (!PostGuid.HasValue)
                Step002_UpdateEntity();

            // Test initial state for the test
            Assert.IsNotNull(PostGuid);

            // Init client
            var cli = new ChillSharpClient("http://localhost:5000/api/chill");

            // Create an empty mock with a Guid of the entity to find
            var e = new ChillDtoEntity();
            e.ChillType = "Model.Post";
            e.Guid = PostGuid.Value;
            // find entity
            var entity = cli.Find(e);
            Assert.IsNotNull(entity);
        }
    }
}
