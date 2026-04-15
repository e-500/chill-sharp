using ChillSharp;
using ChillSharp.Dto;
using ChillSharp.Mcp;
using ChillSharp.Schema.Contracts;
using ChillSharp.Tests.EF.Model;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace ChillSharp.Tests;

[TestClass]
public sealed class McpApi
{
    [TestMethod]
    public void Step001_ToolsDescribeSchemaDiscoveryQueriesAndAuthentication()
    {
        var toolTypeAttribute = typeof(ChillMcpTools).GetCustomAttributes(typeof(McpServerToolTypeAttribute), inherit: false).SingleOrDefault();
        Assert.IsNotNull(toolTypeAttribute);

        var getSchemaList = GetToolMethod(nameof(ChillMcpTools.GetSchemaList));
        var getSchema = GetToolMethod(nameof(ChillMcpTools.GetSchemaAsync));
        var query = GetToolMethod(nameof(ChillMcpTools.Query));

        AssertToolMetadata(getSchemaList, "ChillSharp get-schema-list", "MCP-enabled", "bearer token");
        AssertToolMetadata(getSchema, "ChillSharp get-schema", "MCP-enabled", "properties");
        AssertToolMetadata(query, "ChillSharp query", "MCP-enabled", "ChillDtoQuery");
    }

    [TestMethod]
    public async Task Step002_SchemaDiscoveryReturnsOnlyMcpEnabledSchemas()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"chillsharp-mcp-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<EF.DummyContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        await using var context = new EF.DummyContext(options);
        await context.Database.EnsureCreatedAsync();

        var schemaService = CreateSchemaService(context);
        var discoveryService = new ChillMcpSchemaDiscoveryService(context, schemaService);

        var schemaList = await discoveryService.GetSchemaListAsync("it-IT");
        Assert.IsTrue(schemaList.Any(x => x.ChillType == "Model.Blog"));
        Assert.IsFalse(schemaList.Any(x => x.ChillType == "Model.Post"));
        Assert.IsFalse(schemaList.Any(x => x.ChillType == "Query.PostQuery"));

        var blogSchema = await discoveryService.GetSchemaAsync("Model.Blog", cancellationToken: CancellationToken.None);
        Assert.IsNotNull(blogSchema);
        Assert.AreEqual("Model.Blog", blogSchema.ChillType);

        var postQuerySchema = await discoveryService.GetSchemaAsync("Query.PostQuery", cancellationToken: CancellationToken.None);
        Assert.IsNull(postQuerySchema);
    }

    [TestMethod]
    public async Task Step003_RuntimeEntityOptionsCanEnableMcpSchemasAndQueryExecution()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"chillsharp-mcp-query-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<EF.DummyContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        await using var context = new EF.DummyContext(options);
        await context.Database.EnsureCreatedAsync();

        var blog = new Blog
        {
            Guid = Guid.NewGuid(),
            Title = "Engineering",
            Url = "https://example.test/engineering"
        };

        var post = new Post
        {
            Guid = Guid.NewGuid(),
            Title = "Hello MCP",
            Author = "Ada",
            Blog = blog
        };

        context.Blog.Add(blog);
        context.Post.Add(post);
        await context.SaveChangesAsync();

        var schemaService = CreateSchemaService(context);
        ((IChillContext)context).RegisterSchemaService(schemaService);

        var tools = new ChillMcpTools(
            new ChillMcpSchemaDiscoveryService(context, schemaService),
            new ChillDtoEngine(context));

        try
        {
            await tools.Query(new ChillDtoQuery
            {
                ChillType = "Query.PostQuery"
            });
            Assert.Fail("Expected non MCP-enabled query execution to throw.");
        }
        catch (InvalidOperationException)
        {
        }

        await schemaService.SetEntityOptionsAsync(new ChillSharp.Schema.Contracts.ChillDtoEntityOptions
        {
            ChillType = "Query.PostQuery",
            EnableMCP = true
        });

        var schema = await tools.GetSchemaAsync("Query.PostQuery");
        Assert.IsNotNull(schema);
        Assert.AreEqual("Model.Post", schema.QueryRelatedChillType);

        var result = await tools.Query(new ChillDtoQuery
        {
            ChillType = "Query.PostQuery",
            ResultProperties =
            [
                new ChillDtoProperty("Title"),
                new ChillDtoProperty("Author")
            ]
        });

        Assert.IsNotNull(result);
        Assert.HasCount(1, result.Results);
        Assert.AreEqual("Hello MCP", result.Results[0].Properties["Title"]?.ToString());
        Assert.AreEqual("Ada", result.Results[0].Properties["Author"]?.ToString());
    }

    private static ChillSharp.Schema.ChillSchemaService CreateSchemaService(EF.DummyContext context)
    {
        return new ChillSharp.Schema.ChillSchemaService(
            context,
            new ChillSharp.Schema.ChillContextSchemaRuntimeContext(context),
            new ChillSharp.Schema.ChillSchemaCache());
    }

    private static System.Reflection.MethodInfo GetToolMethod(string methodName)
    {
        return typeof(ChillMcpTools).GetMethod(methodName) ?? throw new AssertFailedException($"Method {methodName} was not found.");
    }

    private static void AssertToolMetadata(System.Reflection.MethodInfo method, string expectedName, params string[] expectedDescriptionSnippets)
    {
        var toolAttribute = method.GetCustomAttributes(typeof(McpServerToolAttribute), inherit: false)
            .Cast<McpServerToolAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(toolAttribute);
        Assert.AreEqual(expectedName, toolAttribute.Name);

        var descriptionAttribute = method.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), inherit: false)
            .Cast<System.ComponentModel.DescriptionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(descriptionAttribute);

        foreach (var snippet in expectedDescriptionSnippets)
        {
            Assert.IsTrue(
                descriptionAttribute.Description.Contains(snippet, StringComparison.OrdinalIgnoreCase),
                $"Description for {method.Name} does not contain '{snippet}'.");
        }
    }
}
