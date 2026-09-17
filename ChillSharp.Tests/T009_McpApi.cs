using ChillSharp;
using ChillSharp.Dto;
using ChillSharp.Mcp;
using ChillSharp.Schema.Contracts;
using ChillSharp.Tests.EF.Model;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using System.Text.Json;

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
        var getDtoExamples = GetToolMethod(nameof(ChillMcpTools.GetDtoExamples));
        var query = GetToolMethod(nameof(ChillMcpTools.Query));
        var lookup = GetToolMethod(nameof(ChillMcpTools.Lookup));
        var find = GetToolMethod(nameof(ChillMcpTools.Find));
        var create = GetToolMethod(nameof(ChillMcpTools.Create));
        var update = GetToolMethod(nameof(ChillMcpTools.Update));
        var delete = GetToolMethod(nameof(ChillMcpTools.Delete));
        var autocompleteEntity = GetToolMethod(nameof(ChillMcpTools.AutocompleteEntity));
        var autocompleteQuery = GetToolMethod(nameof(ChillMcpTools.AutocompleteQuery));
        var validateEntity = GetToolMethod(nameof(ChillMcpTools.ValidateEntity));
        var validateQuery = GetToolMethod(nameof(ChillMcpTools.ValidateQuery));
        var chunk = GetToolMethod(nameof(ChillMcpTools.Chunk));

        AssertToolMetadata(getSchemaList, "ChillSharp get-schema-list", "MCP-enabled", "bearer token");
        AssertToolMetadata(getSchema, "ChillSharp get-schema", "MCP-enabled", "properties", "SimplePropertyType");
        AssertToolMetadata(getDtoExamples, "ChillSharp get-dto-examples", "ChillDtoQuery", "ChillDtoEntity", "Pagination with Page and PageResults", "Ordering with PropertyName and Direction");
        AssertToolMetadata(query, "ChillSharp query", "MCP-enabled", "ChillDtoQuery", "Do not invent request objects", "simplePropertyType", "FullTextSearch", "exact-match equals", "Pagination contains Page and PageResults", "Ordering contains PropertyName and Direction", "ChillSharp get-dto-examples");
        AssertToolMetadata(lookup, "ChillSharp lookup", "MCP-enabled", "full-text");
        AssertToolMetadata(find, "ChillSharp find", "MCP-enabled", "Guid");
        AssertToolMetadata(create, "ChillSharp create", "MCP-enabled", "ChillDtoEntity", "exact schema property names", "Guid, Position, ChillType, Label, ShortLabel, and Properties");
        AssertToolMetadata(update, "ChillSharp update", "MCP-enabled", "Guid");
        AssertToolMetadata(delete, "ChillSharp delete", "MCP-enabled", "mutating");
        AssertToolMetadata(autocompleteEntity, "ChillSharp autocomplete-entity", "MCP-enabled", "entity DTO");
        AssertToolMetadata(autocompleteQuery, "ChillSharp autocomplete-query", "MCP-enabled", "query DTO");
        AssertToolMetadata(validateEntity, "ChillSharp validate-entity", "MCP-enabled", "validation errors");
        AssertToolMetadata(validateQuery, "ChillSharp validate-query", "MCP-enabled", "validation errors");
        AssertToolMetadata(chunk, "ChillSharp chunk", "MCP-enabled", "ChillOperation");
    }

    [TestMethod]
    public void Step002_StaticDtoExamplesReturnSerializedPayloadStructures()
    {
        var options = new DbContextOptionsBuilder<EF.DummyContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new EF.DummyContext(options);
        var schemaService = CreateSchemaService(context);
        var tools = new ChillMcpTools(
            new ChillMcpSchemaDiscoveryService(context, schemaService),
            new ChillDtoEngine(context));

        using var document = JsonDocument.Parse(tools.GetDtoExamples());
        var root = document.RootElement;

        Assert.IsTrue(root.TryGetProperty("ChillDtoQuery", out var queryExample));
        Assert.AreEqual("Query.PostQuery", queryExample.GetProperty("ChillType").GetString());
        Assert.AreEqual(1, queryExample.GetProperty("Pagination").GetProperty("Page").GetInt32());
        Assert.AreEqual(20, queryExample.GetProperty("Pagination").GetProperty("PageResults").GetInt32());
        Assert.AreEqual("Title", queryExample.GetProperty("Ordering").GetProperty("PropertyName").GetString());
        Assert.AreEqual("ASC", queryExample.GetProperty("Ordering").GetProperty("Direction").GetString());

        var resultProperty = queryExample.GetProperty("ResultProperties")[2];
        Assert.AreEqual("Blog", resultProperty.GetProperty("PropertyName").GetString());
        Assert.AreEqual("Guid", resultProperty.GetProperty("SubProperties")[0].GetProperty("PropertyName").GetString());

        Assert.IsTrue(root.TryGetProperty("ChillDtoEntity", out var entityExample));
        Assert.AreEqual("Model.Post", entityExample.GetProperty("ChillType").GetString());
        Assert.AreEqual("Example post", entityExample.GetProperty("Properties").GetProperty("Title").GetString());
        Assert.AreEqual("Model.Blog", entityExample.GetProperty("Properties").GetProperty("Blog").GetProperty("ChillType").GetString());
    }

    [TestMethod]
    public async Task Step003_SchemaDiscoveryReturnsOnlyMcpEnabledSchemas()
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
        Assert.IsTrue(schemaList.Any(x => x.ChillType == "Query.BlogQuery"));
        Assert.IsFalse(schemaList.Any(x => x.ChillType == "Model.Post"));
        Assert.IsFalse(schemaList.Any(x => x.ChillType == "Query.PostQuery"));

        var blogSchema = await discoveryService.GetSchemaAsync("Model.Blog", cancellationToken: CancellationToken.None);
        Assert.IsNotNull(blogSchema);
        Assert.AreEqual("Model.Blog", blogSchema.ChillType);

        var postQuerySchema = await discoveryService.GetSchemaAsync("Query.PostQuery", cancellationToken: CancellationToken.None);
        Assert.IsNull(postQuerySchema);
    }

    [TestMethod]
    public async Task Step004_RuntimeEntityOptionsCanEnableMcpSchemasAndQueryExecution()
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

        Assert.IsNull(await tools.GetSchemaAsync("Query.PostQuery"));
        await AssertInvalidOperationAsync(() => tools.Query(new ChillDtoQuery
        {
            ChillType = "Query.PostQuery"
        }));

        await schemaService.SetEntityOptionsAsync(new ChillSharp.Schema.Contracts.ChillDtoEntityOptions
        {
            ChillType = "Model.Post",
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

    [TestMethod]
    public async Task Step005_McpCrudAndChunkOperateOnlyOnEnabledSchemas()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"chillsharp-mcp-crud-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<EF.DummyContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        await using var context = new EF.DummyContext(options);
        await context.Database.EnsureCreatedAsync();

        var schemaService = CreateSchemaService(context);
        ((IChillContext)context).RegisterSchemaService(schemaService);

        var tools = new ChillMcpTools(
            new ChillMcpSchemaDiscoveryService(context, schemaService),
            new ChillDtoEngine(context));

        var created = await tools.Create(new ChillDtoEntity
        {
            ChillType = "Model.Blog",
            Properties =
            {
                [nameof(Blog.Title)] = "MCP CRUD",
                [nameof(Blog.Url)] = "https://example.test/mcp-crud"
            }
        });

        Assert.AreNotEqual(Guid.Empty, created.Guid);
        Assert.AreEqual("MCP CRUD", created.Properties[nameof(Blog.Title)]?.ToString());

        var found = await tools.Find(new ChillDtoEntity
        {
            ChillType = "Model.Blog",
            Guid = created.Guid
        });

        Assert.IsNotNull(found);
        Assert.AreEqual("https://example.test/mcp-crud", found.Properties[nameof(Blog.Url)]?.ToString());

        var updated = await tools.Update(new ChillDtoEntity
        {
            ChillType = "Model.Blog",
            Guid = created.Guid,
            Properties =
            {
                [nameof(Blog.Title)] = "MCP CRUD Updated"
            }
        });

        Assert.AreEqual("MCP CRUD Updated", updated.Properties[nameof(Blog.Title)]?.ToString());

        var autocomplete = await tools.AutocompleteEntity(new ChillDtoEntity
        {
            ChillType = "Model.Blog",
            Properties =
            {
                [nameof(Blog.Title)] = "Needs Url"
            }
        });

        Assert.AreEqual("https://autocomplete.local/needs-url", autocomplete.Properties[nameof(Blog.Url)]?.ToString());

        var validationErrors = await tools.ValidateEntity(new ChillDtoEntity
        {
            ChillType = "Model.Blog",
            Properties =
            {
                [nameof(Blog.Title)] = "invalid"
            }
        });

        Assert.IsTrue(validationErrors.Any(x => x.FieldName == nameof(Blog.Title)));

        context.ChangeTracker.Clear();

        var chunk = await tools.Chunk(
        [
            new ChillOperation
            {
                Index = 0,
                Verb = ChillOperationVerb.CREATE,
                Entity = new ChillDtoEntity
                {
                    ChillType = "Model.Blog",
                    Properties =
                    {
                        [nameof(Blog.Title)] = "Chunk Blog",
                        [nameof(Blog.Url)] = "https://example.test/chunk"
                    }
                }
            },
            new ChillOperation
            {
                Index = 1,
                Verb = ChillOperationVerb.FIND,
                Entity = new ChillDtoEntity
                {
                    ChillType = "Model.Blog",
                    Guid = created.Guid
                }
            }
        ]);

        Assert.AreEqual("Chunk Blog", chunk[0].Entity?.Properties[nameof(Blog.Title)]?.ToString());
        Assert.AreEqual("MCP CRUD Updated", chunk[1].Entity?.Properties[nameof(Blog.Title)]?.ToString());

        await tools.Delete(new ChillDtoEntity
        {
            ChillType = "Model.Blog",
            Guid = created.Guid
        });

        var deleted = await tools.Find(new ChillDtoEntity
        {
            ChillType = "Model.Blog",
            Guid = created.Guid
        });

        Assert.IsNull(deleted);

        await AssertInvalidOperationAsync(() => tools.Create(new ChillDtoEntity
        {
            ChillType = "Model.Post",
            Properties =
            {
                [nameof(Post.Title)] = "Hidden",
                [nameof(Post.Author)] = "Ada"
            }
        }));

        await AssertInvalidOperationAsync(() => tools.Chunk(
        [
            new ChillOperation
            {
                Index = 0,
                Verb = ChillOperationVerb.CREATE,
                Entity = new ChillDtoEntity
                {
                    ChillType = "Model.Post",
                    Properties =
                    {
                        [nameof(Post.Title)] = "Hidden",
                        [nameof(Post.Author)] = "Ada"
                    }
                }
            }
        ]));
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

    private static async Task AssertInvalidOperationAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        Assert.Fail("Expected InvalidOperationException.");
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
