using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using ChillSharp.Annotations;
using ChillSharp.Dto;
using ChillSharp.EF;
using ChillSharp.Schema;
using ChillSharp.Schema.Contracts;
using ChillSharp.Schema.Model;
using ChillSharp.Test.SchemaFixtures;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Test
{

[TestClass]
public class ChillSchemaServiceTests
{
    private const string ChillType = "SchemaFixtures.SchemaFixtureEntity";
    private const string ViewCode = "grid";

    [TestMethod]
    public async Task GetSchemaAsync_GeneratesLocalizedSchemaFromTheInMemoryDbContextModel()
    {
        await using var fixture = SchemaFixture.Create();

        var englishSchema = await fixture.Service.GetSchemaAsync(ChillType, ViewCode, "en-US");
        var italianSchema = await fixture.Service.GetSchemaAsync(ChillType, ViewCode, "it-IT");

        Assert.IsNotNull(englishSchema);
        Assert.IsNotNull(italianSchema);
        Assert.AreEqual("Products", englishSchema.DisplayName);
        Assert.AreEqual("Prodotti", italianSchema.DisplayName);
        Assert.AreEqual("Product name", Property(englishSchema, "Name").DisplayName);
        Assert.AreEqual("Nome prodotto", Property(italianSchema, "Name").DisplayName);
        Assert.AreEqual("Price", Property(englishSchema, "Price").DisplayName);
        Assert.AreEqual(ChillDtoPropertyType.Decimal, Property(englishSchema, "Price").PropertyType);
        Assert.IsGreaterThanOrEqualTo(2, englishSchema.Properties.Count);
    }

    [TestMethod]
    public async Task SetSchemaAsync_InvalidatesCachedSchemaAndPreservesConfiguredLabelsAndColumnOrderDuringRefresh()
    {
        await using var fixture = SchemaFixture.Create();
        var initialSchema = await fixture.Service.GetSchemaAsync(ChillType, ViewCode, "en-US");

        Assert.IsNotNull(initialSchema);
        Assert.IsTrue(fixture.Cache.TryGet(ChillType, ViewCode, "en-US", out var cachedInitialSchema));
        Assert.AreSame(initialSchema, cachedInitialSchema);

        var configuredSchema = Clone(initialSchema);
        configuredSchema.DisplayName = "Catalog editor";
        configuredSchema.Properties = configuredSchema.Properties
            .OrderByDescending(property => property.Name)
            .ToList();
        Property(configuredSchema, "Name").DisplayName = "Editor name";
        Property(configuredSchema, "Price").DisplayName = "Editor price";

        await fixture.Service.SetSchemaAsync(configuredSchema);

        Assert.IsTrue(fixture.Cache.TryGet(ChillType, ViewCode, "en-US", out var cachedConfiguredSchema));
        var configuredSchemaFromCache = cachedConfiguredSchema!;
        Assert.AreSame(configuredSchema, configuredSchemaFromCache);
        Assert.AreNotSame(cachedInitialSchema, configuredSchemaFromCache);
        CollectionAssert.AreEqual(
            new[] { "Price", "Name" },
            configuredSchemaFromCache.Properties.Where(property => property.Name is "Name" or "Price").Select(property => property.Name).ToArray());

        var refreshedSchema = await fixture.Service.GetSchemaAsync(ChillType, ViewCode, "en-US", update: true);

        Assert.IsNotNull(refreshedSchema);
        Assert.AreEqual("Catalog editor", refreshedSchema.DisplayName);
        Assert.AreEqual("Editor name", Property(refreshedSchema, "Name").DisplayName);
        Assert.AreEqual("Editor price", Property(refreshedSchema, "Price").DisplayName);
        CollectionAssert.AreEqual(
            new[] { "Name", "Price" },
            refreshedSchema.Properties.Where(property => property.Name is "Name" or "Price").Select(property => property.Name).ToArray());
        Assert.AreEqual(1, await fixture.Context.SchemaEntries.CountAsync());
    }

    [TestMethod]
    public async Task SetEntityOptionsAsync_InvalidatesSchemaAndEntityOptionCaches()
    {
        await using var fixture = SchemaFixture.Create();
        var initialSchema = await fixture.Service.GetSchemaAsync(ChillType, ViewCode, "en-US");
        var defaultOptions = await fixture.Service.GetEntityOptionsAsync(ChillType);

        var configuredOptions = await fixture.Service.SetEntityOptionsAsync(new ChillDtoEntityOptions
        {
            ChillType = ChillType,
            ChecksumEnabled = false,
            HandleAttachments = true,
            LabelFormatString = " {Name} ",
            ShortLabelFormatString = " {Name} ",
            FullTextContentFormatString = " {Name} {Price} ",
            EnableMCP = true,
            MCPDescription = " Product catalog entry ",
            ChangeLogEnabled = true
        });

        var cachedOptions = await fixture.Service.GetEntityOptionsAsync(ChillType);
        var schemaWithOptions = await fixture.Service.GetSchemaAsync(ChillType, ViewCode, "en-US");

        Assert.AreNotSame(defaultOptions, configuredOptions);
        Assert.AreSame(configuredOptions, cachedOptions);
        Assert.IsNotNull(initialSchema);
        Assert.IsNotNull(schemaWithOptions);
        Assert.AreNotSame(initialSchema, schemaWithOptions);
        Assert.IsTrue(schemaWithOptions.HandleAttachments);
        Assert.IsTrue(schemaWithOptions.EnableMCP);
        Assert.AreEqual("Product catalog entry", schemaWithOptions.MCPDescription);
        Assert.IsFalse(cachedOptions.ChecksumEnabled);
        Assert.AreEqual("{Name}", cachedOptions.LabelFormatString);
        Assert.AreEqual("{Name} {Price}", cachedOptions.FullTextContentFormatString);
        Assert.IsTrue(cachedOptions.ChangeLogEnabled);
    }

    [TestMethod]
    public async Task SetMenuAsync_PersistsLabelsAndReordersItemsWhenTheirPositionChanges()
    {
        await using var fixture = SchemaFixture.Create();

        var later = await fixture.Service.SetMenuAsync(new ChillDtoMenuItem
        {
            PositionNo = 20,
            Title = " Later ",
            MenuHierarchy = "root"
        });
        await fixture.Service.SetMenuAsync(new ChillDtoMenuItem
        {
            PositionNo = 10,
            Title = "Earlier",
            MenuHierarchy = "root"
        });

        var initiallyOrdered = await fixture.Service.GetMenuAsync();

        CollectionAssert.AreEqual(new[] { "Earlier", "Later" }, initiallyOrdered.Select(item => item.Title).ToArray());

        later.PositionNo = 5;
        later.Title = "First";
        await fixture.Service.SetMenuAsync(later);

        var reordered = await fixture.Service.GetMenuAsync();

        CollectionAssert.AreEqual(new[] { "First", "Earlier" }, reordered.Select(item => item.Title).ToArray());
        Assert.AreEqual(5, reordered[0].PositionNo);
    }

    private static ChillDtoPropertySchema Property(ChillDtoSchema schema, string name)
    {
        return schema.Properties.Single(property => property.Name == name);
    }

    private static ChillDtoSchema Clone(ChillDtoSchema schema)
    {
        return JsonSerializer.Deserialize<ChillDtoSchema>(JsonSerializer.Serialize(schema))!;
    }

    private sealed class SchemaFixture : IAsyncDisposable
    {
        private SchemaFixture(SchemaTestDbContext context, ChillSchemaCache cache, ChillSchemaService service)
        {
            Context = context;
            Cache = cache;
            Service = service;
        }

        public SchemaTestDbContext Context { get; }
        public ChillSchemaCache Cache { get; }
        public ChillSchemaService Service { get; }

        public static SchemaFixture Create()
        {
            var options = new DbContextOptionsBuilder<SchemaTestDbContext>()
                .UseInMemoryDatabase($"chill-schema-test-{Guid.NewGuid():N}")
                .Options;
            var context = new SchemaTestDbContext(options);
            context.Database.EnsureCreated();

            var cache = new ChillSchemaCache();
            var runtimeContext = new SchemaTestRuntimeContext(context);
            return new SchemaFixture(context, cache, new ChillSchemaService(context, runtimeContext, cache));
        }

        public ValueTask DisposeAsync() => Context.DisposeAsync();
    }
}

}

namespace ChillSharp.Test.SchemaFixtures
{

[ChillEntity("E25C5431-8DD5-45B6-9B9E-84D4F48FBFB2", "Products", "Prodotti", EnableMCP = true, MCPDescription = "Product catalog")]
public class SchemaFixtureEntity : ChillEntity
{
    [Key]
    public override Guid Guid { get; set; }

    [ChillProperty("DEC985FB-212B-4C7F-B829-374D43208FD2", "Product name", "Nome prodotto", MaxLength: 120)]
    public string Name { get; set; } = string.Empty;

    [ChillProperty("5D2161CF-DC47-4644-981B-253EFD7BEA88", "Price", "Prezzo", DecimalPlaces: 2, Precision: 18, Scale: 2)]
    public decimal Price { get; set; }
}

public sealed class SchemaTestDbContext(DbContextOptions<SchemaTestDbContext> options) : DbContext(options), IChillSchemaDbContext, IChillContext
{
    public DbSet<SchemaFixtureEntity> Products => Set<SchemaFixtureEntity>();
    public DbSet<ChillSchemaEntry> SchemaEntries => Set<ChillSchemaEntry>();
    public DbSet<ChillEntityOptionsEntry> EntityOptionsEntries => Set<ChillEntityOptionsEntry>();
    public DbSet<ChillMenuItemEntry> MenuItems => Set<ChillMenuItemEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddChillSchemaModel();
    }

    public string GetChillTypePrefix() => "ChillSharp.Test";
}

public sealed class SchemaTestRuntimeContext : IChillSchemaRuntimeContext
{
    private readonly SchemaTestDbContext _context;

    public SchemaTestRuntimeContext(SchemaTestDbContext context)
    {
        _context = context;
    }

    public Assembly ModelAssembly => _context.GetType().Assembly;
    public string ChillTypePrefix => "ChillSharp.Test";
    public string DefaultUserCultureName => "en-US";
    public string RuntimeContextKey => nameof(SchemaTestRuntimeContext);

    public IChillDtoSchema BuildSchema(object activatedType, string chillViewCode, string cultureName)
    {
        return ChillDtoSchema.FromIChillEntity((IChillEntity)activatedType, chillViewCode, ChillTypePrefix, _context, cultureName);
    }
}

}
