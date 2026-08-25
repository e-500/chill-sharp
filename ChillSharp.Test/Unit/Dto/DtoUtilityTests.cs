using ChillSharp.Dto;
using ChillSharp.EF;

namespace ChillSharp.Test;

[TestClass]
public class DtoUtilityTests
{
[TestMethod]
    [DataRow(typeof(Guid), ChillDtoPropertyType.Guid)]
    [DataRow(typeof(int?), ChillDtoPropertyType.Integer)]
    [DataRow(typeof(decimal), ChillDtoPropertyType.Decimal)]
    [DataRow(typeof(DateOnly), ChillDtoPropertyType.Date)]
    [DataRow(typeof(TimeOnly), ChillDtoPropertyType.Time)]
    [DataRow(typeof(DateTimeOffset), ChillDtoPropertyType.DateTime)]
    [DataRow(typeof(TimeSpan), ChillDtoPropertyType.Duration)]
    [DataRow(typeof(bool), ChillDtoPropertyType.Boolean)]
    [DataRow(typeof(string), ChillDtoPropertyType.String)]
    [DataRow(typeof(Uri), ChillDtoPropertyType.Unknown)]
    public void Map_MapsScalarTypes(Type type, ChillDtoPropertyType expected)
    {
        Assert.AreEqual(expected, ChillDtoPropertyMapper.Map(type));
    }

    [TestMethod]
    public void Map_MapsChillEntitiesAndEntityCollections()
    {
        Assert.AreEqual(ChillDtoPropertyType.ChillEntity, ChillDtoPropertyMapper.Map(typeof(TestEntity)));
        Assert.AreEqual(ChillDtoPropertyType.ChillEntityCollection, ChillDtoPropertyMapper.Map(typeof(List<TestEntity>)));
        Assert.AreEqual(ChillDtoPropertyType.ChillEntityCollection, ChillDtoPropertyMapper.Map(typeof(TestEntity[])));
    }

    [TestMethod]
    public void NormalizeChillType_StripsPrefixAndGenericArity()
    {
        var result = ChillTypeResolver.NormalizeChillType(typeof(Dictionary<string, int>), "System.Collections.Generic");

        Assert.AreEqual("Dictionary", result);
    }

    [TestMethod]
    public void PrepareFullChillType_HandlesWhitespaceAndExistingPrefix()
    {
        Assert.AreEqual("ChillSharp.Test.TestEntity", ChillTypeResolver.PrepareFullChillType(" .TestEntity. ", " ChillSharp.Test. "));
        Assert.AreEqual("ChillSharp.Test.TestEntity", ChillTypeResolver.PrepareFullChillType("ChillSharp.Test.TestEntity", "ChillSharp.Test"));
    }

    [TestMethod]
    public void PrepareFullChillType_RejectsAnEmptyType()
    {
        var exception = Assert.Throws<ChillException>(() => ChillTypeResolver.PrepareFullChillType(" . ", "ChillSharp.Test"));

        StringAssert.Contains(exception.Message, "ChillType is required");
    }

    [TestMethod]
    public void ResolveType_ResolvesTypeFromRootAssembly()
    {
        var resolved = ChillTypeResolver.ResolveType(typeof(TestEntity).Assembly, "DtoUtilityTests+TestEntity", "ChillSharp.Test");

        Assert.AreEqual(typeof(TestEntity), resolved);
    }

    [TestMethod]
    public void GetCandidateAssemblies_IncludesRootAssemblyOnlyOnce()
    {
        var rootAssembly = typeof(TestEntity).Assembly;
        var candidates = ChillAssemblyDiscovery.GetCandidateAssemblies(rootAssembly);

        Assert.IsTrue(candidates.Contains(rootAssembly));
        Assert.AreEqual(candidates.Count, candidates.Distinct().Count());
    }

[TestMethod]
    [DataRow("desc", true)]
    [DataRow("DESC", true)]
    [DataRow("asc", false)]
    [DataRow(null, false)]
    public void IsDescending_IsCaseInsensitiveAndOnlyMatchesDesc(string? direction, bool expected)
    {
        var ordering = new ChillOrdering { Direction = direction! };

        Assert.AreEqual(expected, ordering.IsDescending());
    }

    public sealed class TestEntity : ChillEntity;
}
