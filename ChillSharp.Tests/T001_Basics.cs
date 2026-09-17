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
    }
}
