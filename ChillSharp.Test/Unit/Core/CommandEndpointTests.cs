using ChillSharp.Annotations;
using ChillSharp.Api.Controllers;
using ChillSharp.Dto;
using ChillSharp.EF;
using ChillSharp.Tests.EF;
using ChillSharp.Tests.EF.Commands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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