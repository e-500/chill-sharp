using ChillSharp.EF;

namespace ChillSharp.Test;

[TestClass]
public class ChillFullTextSearchNormalizerTests
{
    [TestMethod]
    public void Normalize_RemovesDiacriticsAndFoldsSpecialCharacters()
    {
        var result = ChillFullTextSearchNormalizer.Normalize("Crème Ægir Œuvre ß Ð Þ Ł Ø");

        Assert.AreEqual("creme aegir oeuvre ss d th l o", result);
    }

    [TestMethod]
    public void NormalizeSearchTerms_SplitsDeduplicatesAndNormalizesTokens()
    {
        var terms = ChillFullTextSearchNormalizer.NormalizeSearchTerms(" Café  café*TEA % tea ");

        CollectionAssert.AreEqual(
            new[]
            {
                new ChillFullTextSearchTerm("cafe", false, false),
                new ChillFullTextSearchTerm("tea", false, false)
            },
            terms);
    }

[TestMethod]
    [DataRow("\"coffee shop\"", "coffee shop", true, true)]
    [DataRow("\"*coffee\"", "coffee", false, true)]
    [DataRow("\"coffee*\"", "coffee", true, false)]
    [DataRow("\"*coffee*\"", "coffee", false, false)]
    public void NormalizeSearchTerms_ParsesQuotedPhraseBoundaries(string search, string expectedValue, bool expectedStartBoundary, bool expectedEndBoundary)
    {
        var term = ChillFullTextSearchNormalizer.NormalizeSearchTerms(search).Single();

        Assert.AreEqual(expectedValue, term.Value);
        Assert.AreEqual(expectedStartBoundary, term.MatchStartBoundary);
        Assert.AreEqual(expectedEndBoundary, term.MatchEndBoundary);
    }

[TestMethod]
    [DataRow(null, false)]
    [DataRow("coffee and tea", true)]
    [DataRow("coffee OR tea", true)]
    [DataRow("(coffee tea)", true)]
    [DataRow("\"coffee and tea\"", false)]
    [DataRow("candy", false)]
    public void HasAdvancedSelectors_DetectsOnlyOperatorsAndGroupingOutsideQuotes(string? search, bool expected)
    {
        Assert.AreEqual(expected, ChillFullTextSearchNormalizer.HasAdvancedSelectors(search));
    }

    [TestMethod]
    public void ApplySearch_UsesContainsForPlainTerms()
    {
        var results = Search("coffee");

        CollectionAssert.AreEqual(new[] { "coffee shop", "coffeemaker", "shop coffee" }, results);
    }

    [TestMethod]
    public void ApplySearch_UsesWholeWordMatchingForQuotedTerms()
    {
        var results = Search("\"coffee\"");

        CollectionAssert.AreEqual(new[] { "coffee shop", "shop coffee" }, results);
    }

    [TestMethod]
    public void ApplySearch_HonorsGroupingAndOperatorPrecedence()
    {
        var results = Search("(coffee AND shop) OR tea");

        CollectionAssert.AreEqual(new[] { "coffee shop", "shop coffee", "tea time" }, results);
    }

    private static string[] Search(string query)
    {
        var entities = new IChillEntity[]
        {
            new SearchEntity { FullTextContent = "coffee shop" },
            new SearchEntity { FullTextContent = "coffeemaker" },
            new SearchEntity { FullTextContent = "shop coffee" },
            new SearchEntity { FullTextContent = "tea time" },
            new SearchEntity { FullTextContent = string.Empty }
        };

        return ChillFullTextSearchNormalizer.ApplySearch(entities.AsQueryable(), query)
            .Select(entity => entity.FullTextContent)
            .ToArray();
    }

    private sealed class SearchEntity : ChillEntity;
}
