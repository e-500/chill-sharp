using ChillSharp.Api;
using ChillSharp.Api.Controllers;
using ChillSharp.Dto;
using ChillSharp.EF;
using ChillSharp.Tests.EF;
using ChillSharp.Tests.EF.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace ChillSharp.Test;

[TestClass]
public class AutomaticQueryTests
{
    [TestMethod]
    public void ApplyTo_FiltersPlainClrPropertiesAndScalarCollections()
    {
        var source = new[]
        {
            new PlainRecord("Alpha", 12, 12, RecordState.Published, ["news", "featured"]),
            new PlainRecord("Beta", 7, null, RecordState.Draft, ["news"]),
            new PlainRecord("Alphabet", 30, 30, RecordState.Published, ["archive"]),
            new PlainRecord(null, 12, 12, RecordState.Published, ["featured"])
        }.AsQueryable();
        var query = new AutomaticQuery
        {
            Filter = new AutomaticQueryGroup
            {
                Filters =
                {
                    new() { PropertyName = nameof(PlainRecord.Name), Operator = AutomaticQueryOperator.Contains, Value = "ALPHA", IgnoreCase = true },
                    new() { PropertyName = nameof(PlainRecord.Score), Operator = AutomaticQueryOperator.Between, Value = "10", SecondValue = 20 },
                    new() { PropertyName = nameof(PlainRecord.OptionalScore), Operator = AutomaticQueryOperator.Equal, Value = "12" },
                    new() { PropertyName = nameof(PlainRecord.State), Operator = AutomaticQueryOperator.In, Value = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>("[\"Published\"]") },
                    new() { PropertyName = nameof(PlainRecord.Tags), Operator = AutomaticQueryOperator.Contains, Value = "featured" }
                }
            }
        };

        var results = query.ApplyTo(source).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("Alpha", results[0].Name);
    }

    [TestMethod]
    public void ApplyTo_UsesChillEntityGuidForReferenceEqualityAndSupportsNestedPaths()
    {
        var selectedBlog = NewBlog("Selected");
        var otherBlog = NewBlog("Other");
        var source = new[]
        {
            NewPost("match", selectedBlog),
            NewPost("other", otherBlog),
            NewPost("no-blog", null)
        }.AsQueryable();
        var query = new AutomaticQuery
        {
            Filter = new AutomaticQueryGroup
            {
                Filters =
                {
                    new() { PropertyName = nameof(Post.Blog), Operator = AutomaticQueryOperator.Equal, Value = selectedBlog.Guid },
                    new() { PropertyName = $"{nameof(Post.Blog)}.{nameof(Blog.Title)}", Operator = AutomaticQueryOperator.StartsWith, Value = "sel", IgnoreCase = true }
                }
            }
        };

        var results = query.ApplyTo(source).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("match", results[0].Title);

        var nullNavigationQuery = new AutomaticQuery
        {
            Filter = new AutomaticQueryGroup
            {
                Filters =
                {
                    new() { PropertyName = $"{nameof(Post.Blog)}.{nameof(Blog.Title)}", Operator = AutomaticQueryOperator.IsNull }
                }
            }
        };

        var nullNavigationResults = nullNavigationQuery.ApplyTo(source).ToList();
        Assert.HasCount(1, nullNavigationResults);
        Assert.AreEqual("no-blog", nullNavigationResults[0].Title);
    }

    [TestMethod]
    public void ApplyTo_FiltersEntityCollectionsWithAny()
    {
        var matching = NewBlog("Matching");
        matching.Posts = [NewPost("Release notes", matching), NewPost("Roadmap", matching)];
        var notMatching = NewBlog("Not matching");
        notMatching.Posts = [NewPost("Welcome", notMatching)];
        var empty = NewBlog("Empty");
        empty.Posts = [];

        var query = new AutomaticQuery
        {
            Filter = new AutomaticQueryGroup
            {
                Filters =
                {
                    new()
                    {
                        PropertyName = nameof(Blog.Posts),
                        Operator = AutomaticQueryOperator.Any,
                        ItemFilter = new AutomaticQueryGroup
                        {
                            Filters =
                            {
                                new() { PropertyName = nameof(Post.Title), Operator = AutomaticQueryOperator.Contains, Value = "release", IgnoreCase = true }
                            }
                        }
                    }
                }
            }
        };

        var results = query.ApplyTo(new[] { matching, notMatching, empty }.AsQueryable()).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual(matching.Guid, results[0].Guid);
    }

    [TestMethod]
    public void ChillEngineQuery_AcceptsAutomaticQueryWithoutChangingStandardQueryMethod()
    {
        var options = new DbContextOptionsBuilder<DummyContext>()
            .UseInMemoryDatabase($"automatic-query-{Guid.NewGuid():N}")
            .Options;
        using var context = new DummyContext(options);
        context.Post.AddRange(NewPost("Wanted", null), NewPost("Ignored", null));
        context.SaveChanges();

        var automaticQuery = new AutomaticQuery<Post>
        {
            Definition = new AutomaticQuery
            {
                Filter = new AutomaticQueryGroup
                {
                    Filters =
                    {
                        new() { PropertyName = nameof(Post.Title), Operator = AutomaticQueryOperator.Equal, Value = "Wanted" }
                    }
                }
            }
        };

        var results = new ChillEngine(context).Query(automaticQuery);

        Assert.HasCount(1, results);
        Assert.AreEqual("Wanted", ((Post)results[0]).Title);
    }

    [TestMethod]
    public void ChillDtoEngineQuery_UsesEntityChillTypeForAutomaticQuery()
    {
        using var context = CreateContext();
        context.Post.AddRange(NewPost("Wanted", null), NewPost("Ignored", null));
        context.SaveChanges();
        var dtoQuery = new ChillDtoQuery
        {
            ChillType = "Model.Post",
            AutomaticQuery = TitleEquals("Wanted"),
            ResultProperties = ChillDtoProperty.Build(["Guid", "Title"])
        };

        var result = new ChillDtoEngine(context).Query(dtoQuery);

        Assert.HasCount(1, result.Results);
        Assert.AreEqual("Model.Post", result.Results[0].ChillType);
        Assert.AreEqual("Wanted", result.Results[0].Properties[nameof(Post.Title)]?.ToString());
    }

    [TestMethod]
    public async Task QueryEndpoint_DeserializesAutomaticQueryAndAuthorizesTheEntityResource()
    {
        const string payload = """
        {
          "chillType": "Model.Post",
          "automaticQuery": {
            "filter": {
              "logicalOperator": "And",
              "filters": [
                {
                  "propertyName": "Title",
                  "operator": "Equal",
                  "value": "Wanted"
                }
              ]
            }
          },
          "resultProperties": [
            { "propertyName": "Guid" },
            { "propertyName": "Title" }
          ]
        }
        """;
        var dtoQuery = JsonSerializer.Deserialize<ChillDtoQuery>(payload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        Assert.IsNotNull(dtoQuery);

        using var context = CreateContext();
        context.Post.AddRange(NewPost("Wanted", null), NewPost("Ignored", null));
        context.SaveChanges();
        var acl = new RecordingAclService();
        var controller = new ChillController(new ChillDtoEngine(context), context, acl)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "tester")], "test"))
                }
            }
        };

        var action = await controller.Query(dtoQuery, CancellationToken.None);

        var ok = action as OkObjectResult;
        Assert.IsNotNull(ok);
        var result = ok.Value as ChillDtoQuery;
        Assert.IsNotNull(result);
        Assert.HasCount(1, result.Results);
        Assert.AreEqual("Wanted", result.Results[0].Properties[nameof(Post.Title)]?.ToString());
        Assert.AreEqual("Model", acl.Module);
        Assert.AreEqual("Post", acl.EntityName);
        Assert.AreEqual(ChillEntityAclAction.Query, acl.Action);
    }

    [TestMethod]
    public void DotNetClientDto_SerializesACompatibleAutomaticQueryPayload()
    {
        var clientQuery = new ChillSharp.Client.Dto.ChillDtoQuery
        {
            ChillType = "Model.Post",
            AutomaticQuery = new ChillSharp.Client.Dto.AutomaticQuery
            {
                Filter = new ChillSharp.Client.Dto.AutomaticQueryGroup
                {
                    Filters =
                    {
                        new ChillSharp.Client.Dto.AutomaticQueryFilter
                        {
                            PropertyName = nameof(Post.Title),
                            Operator = ChillSharp.Client.Dto.AutomaticQueryOperator.Contains,
                            Value = "release"
                        }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(clientQuery);
        var serverQuery = JsonSerializer.Deserialize<ChillDtoQuery>(json);

        Assert.IsNotNull(serverQuery?.AutomaticQuery);
        var filter = serverQuery.AutomaticQuery.Filter.Filters.Single();
        Assert.AreEqual(AutomaticQueryOperator.Contains, filter.Operator);
        Assert.AreEqual(nameof(Post.Title), filter.PropertyName);
    }

    [TestMethod]
    public void ApplyTo_ReportsInvalidPropertyPaths()
    {
        var query = new AutomaticQuery
        {
            Filter = new AutomaticQueryGroup
            {
                Filters = { new() { PropertyName = "Missing", Value = "value" } }
            }
        };

        var exception = Assert.Throws<ChillException>(() => query.ApplyTo(Array.Empty<PlainRecord>().AsQueryable()));

        StringAssert.Contains(exception.Message, "Missing");
        StringAssert.Contains(exception.Message, nameof(PlainRecord));
    }

    [TestMethod]
    public void ApplyTo_RejectsCyclicGroupsBeforeBuildingExpressions()
    {
        var group = new AutomaticQueryGroup();
        group.Groups.Add(group);
        var query = new AutomaticQuery { Filter = group };

        var exception = Assert.Throws<ChillException>(() => query.ApplyTo(Array.Empty<PlainRecord>().AsQueryable()));

        StringAssert.Contains(exception.Message, "cycles");
    }

    private static Blog NewBlog(string title) => new()
    {
        Guid = Guid.NewGuid(),
        Title = title,
        Url = $"https://{title.ToLowerInvariant().Replace(' ', '-')}.test"
    };

    private static Post NewPost(string title, Blog? blog) => new()
    {
        Guid = Guid.NewGuid(),
        Title = title,
        Author = "automatic-query-test",
        Blog = blog,
        LastUpdateUser = string.Empty
    };

    private static DummyContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DummyContext>()
            .UseInMemoryDatabase($"automatic-query-dto-{Guid.NewGuid():N}")
            .Options;
        return new DummyContext(options);
    }

    private static AutomaticQuery TitleEquals(string title) => new()
    {
        Filter = new AutomaticQueryGroup
        {
            Filters =
            {
                new()
                {
                    PropertyName = nameof(Post.Title),
                    Operator = AutomaticQueryOperator.Equal,
                    Value = title
                }
            }
        }
    };

    private sealed record PlainRecord(string? Name, int Score, int? OptionalScore, RecordState State, IReadOnlyList<string> Tags);

    private enum RecordState
    {
        Draft,
        Published
    }

    private sealed class RecordingAclService : IChillEntityAclService
    {
        public string? Module { get; private set; }
        public string? EntityName { get; private set; }
        public ChillEntityAclAction? Action { get; private set; }

        public Task<bool> AuthorizeAsync(
            ClaimsPrincipal principal,
            string module,
            string entityName,
            ChillEntityAclAction action,
            CancellationToken cancellationToken = default)
        {
            Module = module;
            EntityName = entityName;
            Action = action;
            return Task.FromResult(true);
        }
    }
}
