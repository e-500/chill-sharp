using ChillSharp.Annotations;
using ChillSharp.Api;
using ChillSharp.Api.Controllers;
using ChillSharp.Dto;
using ChillSharp.EF;
using ChillSharp.Tests.EF;
using ChillSharp.Tests.EF.Commands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace ChillSharp.Test
{
    [TestClass]
    public class CommandEndpointTests
    {
        [TestMethod]
        public async Task CommandEndpoint_MapsParametersExecutesCommandAndReturnsUpdatedParameters()
        {
            const string payload = """
            {
              "chillType": "Commands.EchoCommand",
              "parameters": { "Input": "hello" }
            }
            """;
            var dtoCommand = JsonSerializer.Deserialize<ChillDtoCommand>(payload, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            Assert.IsNotNull(dtoCommand);

            var options = new DbContextOptionsBuilder<DummyContext>()
                .UseInMemoryDatabase($"command-endpoint-{Guid.NewGuid():N}")
                .Options;
            using var context = new DummyContext(options);
            var controller = new ChillController(new ChillDtoEngine(context), context);

            var action = await controller.Command(dtoCommand, CancellationToken.None);

            var ok = action as OkObjectResult;
            Assert.IsNotNull(ok);
            var result = ok.Value as ChillDtoCommand;
            Assert.IsNotNull(result);
            Assert.AreEqual("HELLO", result.Parameters[nameof(EchoCommand.Output)]?.ToString());
        }

        [TestMethod]
        public async Task ChunkCommandOperation_ExecutesCommandAndReturnsUpdatedParameters()
        {
            var operation = new ChillOperation
            {
                Verb = ChillOperationVerb.COMMAND,
                Command = new ChillDtoCommand
                {
                    ChillType = "Commands.EchoCommand",
                    Parameters = new Dictionary<string, object?> { [nameof(EchoCommand.Input)] = "hello" }
                }
            };
            var options = new DbContextOptionsBuilder<DummyContext>()
                .UseInMemoryDatabase($"chunk-command-{Guid.NewGuid():N}")
                .Options;
            using var context = new DummyContext(options);
            var controller = new ChillController(new ChillDtoEngine(context), context);

            var action = await controller.Chunk([operation], CancellationToken.None);

            var ok = action as OkObjectResult;
            Assert.IsNotNull(ok);
            var result = ok.Value as List<ChillOperation>;
            Assert.IsNotNull(result);
            Assert.AreEqual("HELLO", result[0].Command?.Parameters[nameof(EchoCommand.Output)]?.ToString());
        }

        [TestMethod]
        public async Task ChunkCommandOperation_RequiresCommandPermission()
        {
            var operation = new ChillOperation
            {
                Verb = ChillOperationVerb.COMMAND,
                Command = new ChillDtoCommand
                {
                    ChillType = "Commands.EchoCommand",
                    Parameters = new Dictionary<string, object?> { [nameof(EchoCommand.Input)] = "hello" }
                }
            };
            var options = new DbContextOptionsBuilder<DummyContext>()
                .UseInMemoryDatabase($"chunk-command-denied-{Guid.NewGuid():N}")
                .Options;
            using var context = new DummyContext(options);
            var acl = new RecordingAclService { IsAllowed = false };
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

            var action = await controller.Chunk([operation], CancellationToken.None);

            Assert.IsInstanceOfType<ForbidResult>(action);
            Assert.AreEqual("Commands", acl.Module);
            Assert.AreEqual("EchoCommand", acl.EntityName);
            Assert.AreEqual(ChillEntityAclAction.Command, acl.Action);
        }

        [TestMethod]
        public async Task CommandEndpoint_RequiresCommandPermissionForCommandType()
        {
            const string payload = """
            {
              "chillType": "Commands.EchoCommand",
              "parameters": { "Input": "hello" }
            }
            """;
            var dtoCommand = JsonSerializer.Deserialize<ChillDtoCommand>(payload, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            Assert.IsNotNull(dtoCommand);

            var options = new DbContextOptionsBuilder<DummyContext>()
                .UseInMemoryDatabase($"command-denied-{Guid.NewGuid():N}")
                .Options;
            using var context = new DummyContext(options);
            var acl = new RecordingAclService { IsAllowed = false };
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

            var action = await controller.Command(dtoCommand, CancellationToken.None);

            Assert.IsInstanceOfType<ForbidResult>(action);
            Assert.AreEqual("Commands", acl.Module);
            Assert.AreEqual("EchoCommand", acl.EntityName);
            Assert.AreEqual(ChillEntityAclAction.Command, acl.Action);
        }

        private sealed class RecordingAclService : IChillEntityAclService
        {
            public bool IsAllowed { get; init; }
            public string? Module { get; private set; }
            public string? EntityName { get; private set; }
            public ChillEntityAclAction? Action { get; private set; }

            public Task<bool> AuthorizeAsync(ClaimsPrincipal principal, string module, string entityName, ChillEntityAclAction action, CancellationToken cancellationToken = default)
            {
                Module = module;
                EntityName = entityName;
                Action = action;
                return Task.FromResult(IsAllowed);
            }
        }

        [TestMethod]
        public void ContextMetadataCatalog_IncludesCommandsInPermissionLookups()
        {
            using var context = new DummyContext(new DbContextOptionsBuilder<DummyContext>()
                .UseInMemoryDatabase($"command-metadata-{Guid.NewGuid():N}")
                .Options);
            var chillContext = (IChillContext)context;

            Assert.Contains("Commands", chillContext.GetModuleList());
            Assert.Contains("EchoCommand", chillContext.GetCommands("Commands"));
        }
    }
}

namespace ChillSharp.Tests.EF.Commands
{
    public sealed class EchoCommand : IChillCommand
    {
        [ChillProperty]
        public string Input { get; set; } = string.Empty;

        [ChillProperty]
        public string Output { get; set; } = string.Empty;

        public void OnCommand(IChillContext Context)
        {
            Output = Input.ToUpperInvariant();
        }
    }
}