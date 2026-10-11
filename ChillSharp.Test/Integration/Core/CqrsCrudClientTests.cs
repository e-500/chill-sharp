using ChillSharp.Client;
using ChillSharp.Client.Dto;

namespace ChillSharp.Tests;

[TestClass]
public sealed class CqrsCrudClientTests
{
    [TestMethod]
    public void Client_CqrsAndCrudEndpoints_PersistAndReturnExpectedValues()
    {
        TestApiHost.EnsureStarted(6002);
        var client = new ChillSharpClient("http://localhost:6002/api/chill");
        var itemGuid = Guid.NewGuid();

        var created = new ChillDtoEntity
        {
            ChillType = "Model.CqrsCrudItem",
            Guid = itemGuid
        };
        created.Properties.Add("Name", "created");

        var createResult = client.Create(created);
        Assert.AreEqual("created", createResult.GetString("Name"));

        var query = new ChillDtoQuery
        {
            ChillType = "Query.CqrsCrudItemQuery",
            ResultProperties = ChillDtoProperty.Build(["Guid", "Name"])
        };
        query.Properties.Add("Guid", itemGuid);
        var queryResult = client.Query(query);
        Assert.IsNotNull(queryResult.Results);
        Assert.HasCount(1, queryResult.Results);
        Assert.AreEqual("created", queryResult.Results[0].GetString("Name"));

        var itemReference = new ChillDtoEntity
        {
            ChillType = "Model.CqrsCrudItem",
            Guid = itemGuid
        };
        Assert.AreEqual("created", client.Find(itemReference)?.GetString("Name"));

        var update = new ChillDtoEntity
        {
            ChillType = "Model.CqrsCrudItem",
            Guid = itemGuid
        };
        update.Properties.Add("Name", "updated");
        client.Update(update);
        Assert.AreEqual("updated", client.Find(itemReference)?.GetString("Name"));

        var command = new ChillDtoCommand
        {
            ChillType = "Commands.RenameCqrsCrudItemCommand"
        };
        command.Parameters.Add("ItemGuid", itemGuid);
        command.Parameters.Add("Name", "renamed by command");
        var commandResult = client.Command(command);
        Assert.AreEqual("renamed by command", commandResult.Parameters[nameof(ChillSharp.Tests.EF.Commands.RenameCqrsCrudItemCommand.Result)]?.ToString());
        Assert.AreEqual("renamed by command", client.Find(itemReference)?.GetString("Name"));

        client.Delete(itemReference);
        Assert.IsNull(client.Find(itemReference));

        var deletedQuery = client.Query(query);
        Assert.IsNotNull(deletedQuery.Results);
        Assert.IsEmpty(deletedQuery.Results);
    }
}