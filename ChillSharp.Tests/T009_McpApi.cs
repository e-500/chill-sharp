using ChillSharp.Dto;
using ChillSharp.Mcp;
using ChillSharp.Mcp.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace ChillSharp.Tests;

[TestClass]
public sealed class McpApi
{
    [TestMethod]
    public async Task Step001_GetResourceListReturnsOnlyMcpEnabledResources()
    {
        TestApiHost.EnsureStarted(6002);

        using var client = new HttpClient
        {
            BaseAddress = new Uri(TestApiHost.HttpBaseUrl)
        };

        var resources = await client.GetFromJsonAsync<List<ChillMcpResource>>("api/chill-mcp/get-resource-list?cultureName=it-IT");

        Assert.IsNotNull(resources);
        Assert.IsTrue(resources.Any(x => x.ChillType == "Model.Blog"));
        Assert.IsFalse(resources.Any(x => x.ChillType == "Model.Post"));

        var blog = resources.Single(x => x.ChillType == "Model.Blog");
        Assert.AreEqual("entity", blog.ResourceType);
        Assert.AreEqual("Blog resource exposed to MCP clients.", blog.Description);
        Assert.IsTrue(blog.Properties.Any(x => x.Name == "Title" && x.Description == "Blog title used to identify the resource."));
    }

    [TestMethod]
    public async Task Step002_RuntimeEntityOptionsCanExposeAdditionalMcpResources()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"chillsharp-mcp-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<EF.DummyContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        await using var context = new EF.DummyContext(options);
        await context.Database.EnsureCreatedAsync();

        var schemaCache = new ChillSharp.Schema.ChillSchemaCache();
            var schemaService = new ChillSharp.Schema.ChillSchemaService(
                context,
                new ChillSharp.Schema.ChillContextSchemaRuntimeContext(context),
                schemaCache);
        var mcpService = new ChillMcpService(context, schemaService);

        Assert.IsNull(await mcpService.GetResourceAsync("Model.Post"));

        await schemaService.SetEntityOptionsAsync(new ChillSharp.Schema.Contracts.ChillDtoEntityOptions
        {
            ChillType = "Model.Post",
            EnableMCP = true,
            MCPDescription = "Post resource enabled at runtime."
        });

        var postResource = await mcpService.GetResourceAsync("Model.Post");

        Assert.IsNotNull(postResource);
        Assert.AreEqual("Post resource enabled at runtime.", postResource.Description);
        Assert.AreEqual("chill://entity/Model.Post", postResource.Uri);
        Assert.IsTrue(postResource.Properties.Any(x => x.Name == "Author" && x.Description == "Author of the post."));
    }
}
