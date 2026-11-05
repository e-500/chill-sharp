using ChillSharp.Dto;
using ChillSharp.EF;

namespace ChillSharp.Test;

[TestClass]
public sealed class ChillOperationTests
{
    [TestMethod]
    public void Execute_DelegatesSupportedVerbsAndStoresReturnedDtos()
    {
        var engine = new RecordingDtoEngine();
        var command = new ChillDtoCommand();
        var query = new ChillDtoQuery();
        var entity = new ChillDtoEntity();

        new ChillOperation { Verb = ChillOperationVerb.TRANSACTION }.Execute(engine);
        var commandOperation = new ChillOperation { Verb = ChillOperationVerb.COMMAND, Command = command };
        commandOperation.Execute(engine);
        new ChillOperation { Verb = ChillOperationVerb.QUERY, Query = query }.Execute(engine);
        var findOperation = new ChillOperation { Verb = ChillOperationVerb.FIND, Entity = entity };
        findOperation.Execute(engine);
        var createOperation = new ChillOperation { Verb = ChillOperationVerb.CREATE, Entity = entity };
        createOperation.Execute(engine);
        var updateOperation = new ChillOperation { Verb = ChillOperationVerb.UPDATE, Entity = entity };
        updateOperation.Execute(engine);
        new ChillOperation { Verb = ChillOperationVerb.DELETE, Entity = entity }.Execute(engine);
        var queryAutocomplete = new ChillOperation { Verb = ChillOperationVerb.AUTOCOMPLETE, Query = query };
        queryAutocomplete.Execute(engine);
        var entityAutocomplete = new ChillOperation { Verb = ChillOperationVerb.AUTOCOMPLETE, Entity = entity };
        entityAutocomplete.Execute(engine);
        var queryValidation = new ChillOperation { Verb = ChillOperationVerb.VALIDATE, Query = query };
        queryValidation.Execute(engine);
        var entityValidation = new ChillOperation { Verb = ChillOperationVerb.VALIDATE, Entity = entity };
        entityValidation.Execute(engine);
        new ChillOperation { Verb = ChillOperationVerb.COMMIT }.Execute(engine);
        new ChillOperation { Verb = "unknown" }.Execute(engine);

        Assert.AreSame(command, commandOperation.Command);
        Assert.AreSame(query, queryAutocomplete.Query);
        Assert.AreSame(entity, findOperation.Entity);
        Assert.AreSame(entity, createOperation.Entity);
        Assert.AreSame(entity, updateOperation.Entity);
        Assert.AreSame(entity, entityAutocomplete.Entity);
        Assert.IsNotNull(queryValidation.ValidationErrors);
        Assert.IsNotNull(entityValidation.ValidationErrors);
        Assert.HasCount(0, queryValidation.ValidationErrors);
        Assert.HasCount(0, entityValidation.ValidationErrors);
        Assert.AreEqual(1, engine.BeginCount);
        Assert.AreEqual(1, engine.CommitCount);
        Assert.AreEqual(1, engine.CommandCount);
        Assert.AreEqual(1, engine.QueryCount);
        Assert.AreEqual(1, engine.FindCount);
        Assert.AreEqual(1, engine.CreateCount);
        Assert.AreEqual(1, engine.UpdateCount);
        Assert.AreEqual(1, engine.DeleteCount);
        Assert.AreEqual(2, engine.AutocompleteCount);
        Assert.AreEqual(2, engine.ValidateCount);
    }

    [TestMethod]
    public void Execute_SkipsVerbsMissingRequiredPayload()
    {
        var engine = new RecordingDtoEngine();

        foreach (var verb in new[]
        {
            ChillOperationVerb.COMMAND,
            ChillOperationVerb.QUERY,
            ChillOperationVerb.FIND,
            ChillOperationVerb.CREATE,
            ChillOperationVerb.UPDATE,
            ChillOperationVerb.DELETE,
            ChillOperationVerb.AUTOCOMPLETE,
            ChillOperationVerb.VALIDATE
        })
        {
            new ChillOperation { Verb = verb }.Execute(engine);
        }

        Assert.AreEqual(0, engine.CommandCount);
        Assert.AreEqual(0, engine.QueryCount);
        Assert.AreEqual(0, engine.FindCount);
        Assert.AreEqual(0, engine.CreateCount);
        Assert.AreEqual(0, engine.UpdateCount);
        Assert.AreEqual(0, engine.DeleteCount);
        Assert.AreEqual(0, engine.AutocompleteCount);
        Assert.AreEqual(0, engine.ValidateCount);
    }

    private sealed class RecordingDtoEngine : IChillDtoEngine
    {
        public int BeginCount { get; private set; }
        public int CommitCount { get; private set; }
        public int CommandCount { get; private set; }
        public int QueryCount { get; private set; }
        public int FindCount { get; private set; }
        public int CreateCount { get; private set; }
        public int UpdateCount { get; private set; }
        public int DeleteCount { get; private set; }
        public int AutocompleteCount { get; private set; }
        public int ValidateCount { get; private set; }

        public void BeginTransaction() => BeginCount++;
        public void CommitTransaction() => CommitCount++;
        public void RollbackTransaction() { }
        public ChillDtoCommand Command(ChillDtoCommand dtoCommand) { CommandCount++; return dtoCommand; }
        public ChillDtoQuery Query(ChillDtoQuery dtoQuery) { QueryCount++; return dtoQuery; }
        public ChillDtoQuery Lookup(ChillDtoQuery dtoQuery) => dtoQuery;
        public ChillDtoEntity? Find(ChillDtoEntity dtoEntity) { FindCount++; return dtoEntity; }
        public ChillDtoEntity Create(ChillDtoEntity dtoEntity) { CreateCount++; return dtoEntity; }
        public ChillDtoEntity Update(ChillDtoEntity dtoEntity) { UpdateCount++; return dtoEntity; }
        public void Delete(ChillDtoEntity dtoEntity) => DeleteCount++;
        public ChillDtoEntity Autocomplete(ChillDtoEntity dtoEntity) { AutocompleteCount++; return dtoEntity; }
        public ChillDtoQuery Autocomplete(ChillDtoQuery dtoQuery) { AutocompleteCount++; return dtoQuery; }
        public IEnumerable<ChillValidationError> Validate(ChillDtoEntity dtoEntity) { ValidateCount++; return []; }
        public IEnumerable<ChillValidationError> Validate(ChillDtoQuery dtoQuery) { ValidateCount++; return []; }
    }
}
