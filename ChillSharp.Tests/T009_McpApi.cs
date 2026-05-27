using ChillSharp;
using ChillSharp.Dto;
using ChillSharp.Mcp;
using ChillSharp.Mcp.Contracts;
using ChillSharp.Schema.Contracts;
using ChillSharp.Tests.EF.Model;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using System.Reflection;
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
        var toolMethods = new[]
        {
            getSchemaList,
            getSchema,
            getDtoExamples,
            query,
            lookup,
            find,
            create,
            update,
            delete,
            autocompleteEntity,
            autocompleteQuery,
            validateEntity,
            validateQuery,
            chunk
        };

        AssertToolMetadata(getSchemaList, "ChillSharp.get-schema-list", "MCP-enabled", "bearer token");
        AssertToolMetadata(getSchema, "ChillSharp.get-schema", "MCP-enabled", "properties", "SimplePropertyType");
        AssertToolMetadata(getDtoExamples, "ChillSharp.get-dto-examples", "MCP query", "entity payload", "Pagination with Page and PageResults", "Ordering with PropertyName and Direction");
        AssertToolMetadata(query, "ChillSharp.query", "MCP-enabled", "MCP query", "schema property names", "simplePropertyType", "FullTextSearch");
        AssertToolMetadata(lookup, "ChillSharp.lookup", "MCP-enabled", "full-text");
        AssertToolMetadata(find, "ChillSharp.find", "MCP-enabled", "Guid");
        AssertToolMetadata(create, "ChillSharp.create", "MCP-enabled", "MCP entity", "schema");
        AssertToolMetadata(update, "ChillSharp.update", "MCP-enabled", "Guid");
        AssertToolMetadata(delete, "ChillSharp.delete", "MCP-enabled", "mutating");
        AssertToolMetadata(autocompleteEntity, "ChillSharp.autocomplete-entity", "MCP-enabled", "entity DTO");
        AssertToolMetadata(autocompleteQuery, "ChillSharp.autocomplete-query", "MCP-enabled", "query DTO");
        AssertToolMetadata(validateEntity, "ChillSharp.validate-entity", "MCP-enabled", "validation errors");
        AssertToolMetadata(validateQuery, "ChillSharp.validate-query", "MCP-enabled", "validation errors");
        AssertToolMetadata(chunk, "ChillSharp.chunk", "MCP-enabled", "operation");

        foreach (var method in toolMethods)
        {
            AssertMcpContractType(method.ReturnType, method.Name);
            foreach (var parameter in method.GetParameters().Where(x => x.ParameterType != typeof(CancellationToken)))
            {
                AssertMcpContractType(parameter.ParameterType, method.Name);
            }
        }
    }

    [TestMethod]
    public void Step002_ToolsAdvertiseOutputSchemas()
    {
        var options = new DbContextOptionsBuilder<EF.DummyContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new EF.DummyContext(options);
        var schemaService = CreateSchemaService(context);
        var tools = new ChillMcpTools(
            new ChillMcpSchemaDiscoveryService(context, schemaService),
            new ChillDtoEngine(context));

        var toolMethods = typeof(ChillMcpTools)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(x => x.GetCustomAttribute<McpServerToolAttribute>() is not null)
            .ToArray();

        Assert.IsNotEmpty(toolMethods);

        foreach (var method in toolMethods)
        {
            var serverTool = McpServerTool.Create(method, tools);
            var outputSchema = serverTool.ProtocolTool.OutputSchema;

            Assert.IsNotNull(outputSchema, $"Tool {method.Name} should expose ProtocolTool.OutputSchema.");
            Assert.IsTrue(
                JsonSerializer.Serialize(outputSchema).Contains("\"type\"", StringComparison.OrdinalIgnoreCase),
                $"Tool {method.Name} output schema should be a JSON schema.");
        }
    }

    [TestMethod]
    public void Step003_StaticDtoExamplesReturnSerializedPayloadStructures()
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

        Assert.IsTrue(root.TryGetProperty("ChillMcpQuery", out var queryExample));
        Assert.AreEqual("Query.PostQuery", queryExample.GetProperty("ChillType").GetString());
        Assert.AreEqual(1, queryExample.GetProperty("Pagination").GetProperty("Page").GetInt32());
        Assert.AreEqual(20, queryExample.GetProperty("Pagination").GetProperty("PageResults").GetInt32());
        Assert.AreEqual("Title", queryExample.GetProperty("Ordering").GetProperty("PropertyName").GetString());
        Assert.AreEqual("ASC", queryExample.GetProperty("Ordering").GetProperty("Direction").GetString());

        var resultProperty = queryExample.GetProperty("ResultProperties")[2];
        Assert.AreEqual("Blog", resultProperty.GetProperty("PropertyName").GetString());
        Assert.AreEqual("Guid", resultProperty.GetProperty("SubProperties")[0].GetProperty("PropertyName").GetString());

        Assert.IsTrue(root.TryGetProperty("ChillMcpEntity", out var entityExample));
        Assert.AreEqual("Model.Post", entityExample.GetProperty("ChillType").GetString());
        Assert.AreEqual("Example post", entityExample.GetProperty("Properties").GetProperty("Title").GetString());
        Assert.AreEqual("Model.Blog", entityExample.GetProperty("Properties").GetProperty("Blog").GetProperty("ChillType").GetString());
    }

    [TestMethod]
    public async Task Step004_SchemaDiscoveryReturnsOnlyMcpEnabledSchemas()
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
        Assert.AreEqual(
            "Blog resource exposed to MCP clients.",
            schemaList.Single(x => x.ChillType == "Model.Blog").Description);

        var blogSchema = await discoveryService.GetSchemaAsync("Model.Blog", cancellationToken: CancellationToken.None);
        Assert.IsNotNull(blogSchema);
        Assert.AreEqual("Model.Blog", blogSchema.ChillType);
        Assert.AreEqual("Blog resource exposed to MCP clients.", blogSchema.Description);
        Assert.AreEqual(
            "Blog title used to identify the resource.",
            blogSchema.Properties.Single(x => x.Name == nameof(Blog.Title)).Description);

        var postQuerySchema = await discoveryService.GetSchemaAsync("Query.PostQuery", cancellationToken: CancellationToken.None);
        Assert.IsNull(postQuerySchema);
    }

    [TestMethod]
    public async Task Step005_RuntimeEntityOptionsCanEnableMcpSchemasAndQueryExecution()
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
            await tools.Query(new ChillMcpQuery
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
        await AssertInvalidOperationAsync(() => tools.Query(new ChillMcpQuery
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

        var result = await tools.Query(new ChillMcpQuery
        {
            ChillType = "Query.PostQuery",
            ResultProperties =
            [
                new ChillMcpProperty { PropertyName = "Title" },
                new ChillMcpProperty { PropertyName = "Author" }
            ]
        });

        Assert.IsNotNull(result);
        Assert.HasCount(1, result.Results);
        Assert.AreEqual("Hello MCP", result.Results[0].Properties["Title"]?.ToString());
        Assert.AreEqual("Ada", result.Results[0].Properties["Author"]?.ToString());
    }

    [TestMethod]
    public async Task Step006_McpCrudAndChunkOperateOnlyOnEnabledSchemas()
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

        var created = await tools.Create(new ChillMcpEntity
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

        var found = await tools.Find(new ChillMcpEntity
        {
            ChillType = "Model.Blog",
            Guid = created.Guid
        });

        Assert.IsNotNull(found);
        Assert.AreEqual("https://example.test/mcp-crud", found.Properties[nameof(Blog.Url)]?.ToString());

        var updated = await tools.Update(new ChillMcpEntity
        {
            ChillType = "Model.Blog",
            Guid = created.Guid,
            Properties =
            {
                [nameof(Blog.Title)] = "MCP CRUD Updated"
            }
        });

        Assert.AreEqual("MCP CRUD Updated", updated.Properties[nameof(Blog.Title)]?.ToString());

        var autocomplete = await tools.AutocompleteEntity(new ChillMcpEntity
        {
            ChillType = "Model.Blog",
            Properties =
            {
                [nameof(Blog.Title)] = "Needs Url"
            }
        });

        Assert.AreEqual("https://autocomplete.local/needs-url", autocomplete.Properties[nameof(Blog.Url)]?.ToString());

        var validationErrors = await tools.ValidateEntity(new ChillMcpEntity
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
            new ChillMcpOperation
            {
                Index = 0,
                Verb = ChillOperationVerb.CREATE,
                Entity = new ChillMcpEntity
                {
                    ChillType = "Model.Blog",
                    Properties =
                    {
                        [nameof(Blog.Title)] = "Chunk Blog",
                        [nameof(Blog.Url)] = "https://example.test/chunk"
                    }
                }
            },
            new ChillMcpOperation
            {
                Index = 1,
                Verb = ChillOperationVerb.FIND,
                Entity = new ChillMcpEntity
                {
                    ChillType = "Model.Blog",
                    Guid = created.Guid
                }
            }
        ]);

        Assert.AreEqual("Chunk Blog", chunk[0].Entity?.Properties[nameof(Blog.Title)]?.ToString());
        Assert.AreEqual("MCP CRUD Updated", chunk[1].Entity?.Properties[nameof(Blog.Title)]?.ToString());

        await tools.Delete(new ChillMcpEntity
        {
            ChillType = "Model.Blog",
            Guid = created.Guid
        });

        var deleted = await tools.Find(new ChillMcpEntity
        {
            ChillType = "Model.Blog",
            Guid = created.Guid
        });

        Assert.IsNull(deleted);

        await AssertInvalidOperationAsync(() => tools.Create(new ChillMcpEntity
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
            new ChillMcpOperation
            {
                Index = 0,
                Verb = ChillOperationVerb.CREATE,
                Entity = new ChillMcpEntity
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
        Assert.IsTrue(toolAttribute.UseStructuredContent, $"Tool {method.Name} should advertise an output schema.");

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

    private static void AssertMcpContractType(Type type, string toolName)
    {
        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
            {
                AssertMcpContractType(argument, toolName);
            }
        }

        if (type.IsArray)
        {
            AssertMcpContractType(type.GetElementType()!, toolName);
        }

        if (type.Namespace is "ChillSharp.Dto" or "ChillSharp.Schema.Contracts")
        {
            Assert.Fail($"Tool {toolName} exposes shared DTO contract type {type.FullName}.");
        }
    }
}
