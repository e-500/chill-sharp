extern alias ChillSchemaAssembly;

using ChillSharp.Annotations;
using ChillSharp.EF;
using ChillSharp.Schema;
using ChillSharp.Schema.Contracts;
using SchemaLabelFormatter = ChillSchemaAssembly::ChillSharp.Schema.ChillEntityLabelFormatter;

namespace ChillSharp.Test;

[TestClass]
public sealed class ChillEntityLabelFormatterTests
{
    [TestMethod]
    public void ResolveConfiguredFormats_FormatsNestedValuesAndNormalizesFullText()
    {
        var context = CreateContext(new ChillDtoEntityOptions
        {
            LabelFormatString = "{Name} ({Amount}) {Child}",
            ShortLabelFormatString = "{Child.Name}",
            FullTextContentFormatString = "{Name} {Tags} {Children.Name}"
        });
        var child = new FormatterEntity { Guid = Guid.NewGuid(), Name = "Child name" };
        var entity = new FormatterEntity
        {
            Name = "Crème",
            Amount = 12.5m,
            Child = child,
            Tags = ["alpha", "beta"],
            Children = [new FormatterEntity { Name = "One" }, new FormatterEntity { Name = "Two" }]
        };

        using var culture = ChillSharpThreadContext.Begin(
            System.Globalization.CultureInfo.GetCultureInfo("fr-FR"),
            TimeZoneInfo.Utc);

        Assert.AreEqual($"Crème (12.5) {child.Guid:D}", SchemaLabelFormatter.ResolveLabel(entity, context));
        Assert.AreEqual("Child name", SchemaLabelFormatter.ResolveShortLabel(entity, context));
        Assert.AreEqual("creme alpha, beta one, two", SchemaLabelFormatter.ResolveFullTextContent(entity, context));
    }

    [TestMethod]
    public void ResolveConfiguredLabel_InvokesInflateAndIgnoresMissingOrUnmarkedProperties()
    {
        var context = CreateContext(new ChillDtoEntityOptions
        {
            LabelFormatString = "{Name}|{Missing}|{Unmarked}|{Child.Name}|{Children.Name}|{CustomValue}"
        });
        var entity = new FormatterEntity
        {
            Name = "Root",
            Child = null,
            Children = [new FormatterEntity { Name = "" }, new FormatterEntity { Name = "Second" }],
            CustomValue = new FormatterPlainValue()
        };

        var configured = SchemaLabelFormatter.TryResolveConfiguredLabel(entity, context, useShortLabel: false);

        Assert.AreEqual("Root||||Second|plain", configured);
        Assert.IsTrue(entity.InflatedProperties.Contains(nameof(FormatterEntity.Child)));
    }

    [TestMethod]
    public void ResolveConfiguredFormats_HandlesNullCollectionsAndEntityLabelsOrGuids()
    {
        var context = CreateContext(new ChillDtoEntityOptions
        {
            LabelFormatString = "{Child}|{Children.Name}|{Amount}"
        });
        var labeledChild = new FormatterEntity { Guid = Guid.NewGuid(), Label = "Existing label" };
        var entity = new FormatterEntity
        {
            Child = labeledChild,
            Children = null!,
            Amount = 1.25m
        };

        Assert.AreEqual("Existing label||1.25", SchemaLabelFormatter.ResolveLabel(entity, context));

        entity.Child = null;
        Assert.AreEqual("||1.25", SchemaLabelFormatter.ResolveLabel(entity, context));
    }

    [TestMethod]
    public void ResolveMethods_FallBackWhenNoFormatsExistAndHonorCustomFullText()
    {
        var context = CreateContext(new ChillDtoEntityOptions());
        var entity = new FormatterEntity { Guid = Guid.NewGuid() };

        Assert.AreEqual($"ChillEntity Guid = {entity.Guid}", SchemaLabelFormatter.ResolveLabel(entity, context));
        Assert.AreEqual("Chill(ChillEnt", SchemaLabelFormatter.ResolveShortLabel(entity, context));
        Assert.AreEqual($"chillentity guid = {entity.Guid}".ToLowerInvariant(), SchemaLabelFormatter.ResolveFullTextContent(entity, context));

        var customEntity = new CustomFullTextFormatterEntity();
        Assert.AreEqual("creme custom full text", SchemaLabelFormatter.ResolveFullTextContent(customEntity, context));
    }

    [TestMethod]
    public void ResolveConfiguredFormats_LoadReferenceSafelyForDbContext()
    {
        using var context = new ChillSharp.Tests.EF.DummyContext();
        ((IChillContext)context).RegisterSchemaService(new FormatterSchemaService(new ChillDtoEntityOptions
        {
            LabelFormatString = "{Child.Name}"
        }));
        var entity = new FormatterEntity { Child = new FormatterEntity { Name = "Loaded safely" } };

        Assert.AreEqual("Loaded safely", SchemaLabelFormatter.ResolveLabel(entity, context));
    }

    private static FormatterContext CreateContext(ChillDtoEntityOptions options)
    {
        var context = new FormatterContext();
        ((IChillContext)context).RegisterSchemaService(new FormatterSchemaService(options));
        return context;
    }

    private sealed class FormatterContext : IChillContext
    {
        public string GetChillTypePrefix() => "ChillSharp.Test";
    }

    private sealed class FormatterSchemaService(IChillDtoEntityOptions options) : IChillSchemaResolverService
    {
        public IChillDtoSchema? ResolveSchema(string chillType, string chillViewCode, string? cultureName = null) => null;
        public IChillDtoEntityOptions GetEntityOptions(string chillType) => options;
    }

    private class FormatterEntity : ChillEntity
    {
        [ChillProperty]
        public string Name { get; set; } = string.Empty;

        [ChillProperty]
        public decimal Amount { get; set; }

        [ChillProperty(null, CallOnInflate: true)]
        public FormatterEntity? Child { get; set; }

        [ChillProperty]
        public List<FormatterEntity>? Children { get; set; } = [];

        [ChillProperty]
        public string[] Tags { get; set; } = [];

        [ChillProperty]
        public object CustomValue { get; set; } = string.Empty;

        public string Unmarked { get; set; } = "must not leak";

        public List<string> InflatedProperties { get; } = [];

        public override void OnInflate(IChillContext context, string propertyName)
        {
            InflatedProperties.Add(propertyName);
        }
    }

    private sealed class CustomFullTextFormatterEntity : FormatterEntity
    {
        public override string GetFullTextContent(IChillContext context) => "Crème Custom Full Text";
    }

    private sealed class FormatterPlainValue
    {
        public override string ToString() => "plain";
    }
}
