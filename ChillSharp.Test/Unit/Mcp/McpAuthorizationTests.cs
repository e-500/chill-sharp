using System.Security.Cryptography;
using System.Text;
using ChillSharp.Auth;
using ChillSharp.Auth.Api;
using ChillSharp.Auth.Contracts;
using ChillSharp.Auth.Model;
using ChillSharp.Auth.Services;
using ChillSharp.Dto;
using ChillSharp.EF;
using ChillSharp.Mcp;
using ChillSharp.Mcp.Contracts;
using ChillSharp.Schema;
using ChillSharp.Schema.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ChillSharp.Test;

[TestClass]
public class ChillMcpAuthorizationTests
{
    [TestMethod]
    public async Task OAuthAuthorizationCodeFlow_RegistersDedicatedMcpClientAndIssuesBearerToken()
    {
        var services = new ServiceCollection();
        services.AddDbContext<McpOAuthDbContext>(options => options.UseInMemoryDatabase($"mcp-oauth-{Guid.NewGuid():N}"));
        services.AddIdentityCore<IdentityUser>()
            .AddEntityFrameworkStores<McpOAuthDbContext>();
        services.AddChillAuthIdentityApi<McpOAuthDbContext, IdentityUser>();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<McpOAuthDbContext>();
        await context.Database.EnsureCreatedAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var identityUser = new IdentityUser("mcp.user");
        var createResult = await userManager.CreateAsync(identityUser, "Pass123$");
        Assert.IsTrue(createResult.Succeeded);

        var oauthService = scope.ServiceProvider.GetRequiredService<IChillAuthOAuthService>();
        var registration = await oauthService.RegisterClientAsync(new OAuthClientRegistrationRequest
        {
            ClientName = "MCP test client",
            RedirectUris = ["https://client.example.test/oauth/callback"]
        });

        var verifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var challenge = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var authorizeRequest = new OAuthAuthorizeRequest
        {
            ResponseType = "code",
            ClientId = registration.ClientId,
            RedirectUri = registration.RedirectUris.Single(),
            CodeChallenge = challenge,
            CodeChallengeMethod = "S256",
            Scope = "mcp"
        };

        var redirect = await oauthService.AuthorizeAsync(authorizeRequest, "mcp.user", "Pass123$");
        var code = QueryHelpers.ParseQuery(redirect.Query)["code"].ToString();
        var token = await oauthService.ExchangeCodeAsync(new OAuthTokenRequest
        {
            GrantType = "authorization_code",
            ClientId = registration.ClientId,
            RedirectUri = registration.RedirectUris.Single(),
            Code = code,
            CodeVerifier = verifier
        });

        Assert.IsFalse(string.IsNullOrWhiteSpace(token.AccessToken));
        Assert.AreEqual("Bearer", token.TokenType);
        Assert.AreEqual("mcp", token.Scope);
        Assert.IsNotNull(await context.Set<AuthOAuthClient>().SingleOrDefaultAsync(client => client.ClientId == registration.ClientId));
        Assert.AreEqual(1, await context.RefreshTokens.CountAsync());
    }

    [TestMethod]
    public async Task ToolCalls_RequireMcpPublicationAndPropagateTheAuthenticatedEnginePermissionDecision()
    {
        var schemaService = new McpSchemaService(enableMcp: true);
        var engine = new PermissionCheckingDtoEngine { CanCreate = false };
        var tools = new ChillMcpTools(
            new ChillMcpSchemaDiscoveryService(new McpContext(), schemaService),
            engine);

        var schema = await tools.GetSchemaAsync("Sales.Invoice");
        Assert.IsNotNull(schema);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => tools.Create(CreateInvoice()));

        engine.CanCreate = true;
        var created = await tools.Create(CreateInvoice());

        Assert.AreNotEqual(Guid.Empty, created.Guid);
        Assert.AreEqual("MCP invoice", created.Properties["Name"]);
        Assert.AreEqual(2, engine.CreateCalls);
    }

    [TestMethod]
    public async Task ToolCalls_RejectUnpublishedSchemasBeforeReachingTheAuthenticatedEngine()
    {
        var engine = new PermissionCheckingDtoEngine { CanCreate = true };
        var tools = new ChillMcpTools(
            new ChillMcpSchemaDiscoveryService(new McpContext(), new McpSchemaService(enableMcp: false)),
            engine);

        await Assert.ThrowsAsync<InvalidOperationException>(() => tools.Create(CreateInvoice()));

        Assert.AreEqual(0, engine.CreateCalls);
    }

    private static ChillMcpEntity CreateInvoice() => new()
    {
        ChillType = "Sales.Invoice",
        Properties = { ["Name"] = "MCP invoice" }
    };

    private sealed class McpContext : IChillContext
    {
        public string GetChillTypePrefix() => "ChillSharp.Test";
    }

    private sealed class McpSchemaService(bool enableMcp) : IChillSchemaService
    {
        public Task<ChillDtoSchema?> GetSchemaAsync(string chillType, string chillViewCode, string? cultureName = null, CancellationToken cancellationToken = default, bool update = false)
        {
            return Task.FromResult<ChillDtoSchema?>(chillType == "Sales.Invoice"
                ? new ChillDtoSchema { ChillType = chillType, ChillViewCode = chillViewCode, EnableMCP = enableMcp }
                : null);
        }

        public Task<ChillDtoSchema> SetSchemaAsync(ChillDtoSchema schema, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ChillDtoEntityOptions> GetEntityOptionsAsync(string chillType, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ChillDtoEntityOptions> SetEntityOptionsAsync(ChillDtoEntityOptions entityOptions, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ChillDtoMenuItem>> GetMenuAsync(Guid? parentGuid = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ChillDtoMenuItem> SetMenuAsync(ChillDtoMenuItem menuItem, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteMenuAsync(Guid menuItemGuid, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class PermissionCheckingDtoEngine : IChillDtoEngine
    {
        public bool CanCreate { get; set; }
        public int CreateCalls { get; private set; }

        public ChillDtoEntity Create(ChillDtoEntity entity)
        {
            CreateCalls++;
            if (!CanCreate)
                throw new UnauthorizedAccessException("The OAuth-authenticated user cannot create Sales.Invoice.");

            entity.Guid = Guid.NewGuid();
            return entity;
        }

        public void BeginTransaction() { }
        public void CommitTransaction() { }
        public void RollbackTransaction() { }
        public ChillDtoQuery Query(ChillDtoQuery query) => query;
        public ChillDtoQuery Lookup(ChillDtoQuery query) => query;
        public ChillDtoEntity? Find(ChillDtoEntity entity) => entity;
        public ChillDtoEntity Update(ChillDtoEntity entity) => entity;
        public void Delete(ChillDtoEntity entity) { }
        public ChillDtoEntity Autocomplete(ChillDtoEntity entity) => entity;
        public ChillDtoQuery Autocomplete(ChillDtoQuery query) => query;
        public IEnumerable<ChillValidationError> Validate(ChillDtoEntity entity) => [];
        public IEnumerable<ChillValidationError> Validate(ChillDtoQuery query) => [];
    }

    private sealed class McpOAuthDbContext(DbContextOptions<McpOAuthDbContext> options) : IdentityDbContext<IdentityUser>(options), IChillAuthDbContext, IChillContext
    {
        DbSet<AuthUser> IChillAuthDbContext.Users => Set<AuthUser>();
        DbSet<AuthRole> IChillAuthDbContext.Roles => Set<AuthRole>();
        DbSet<AuthUserRole> IChillAuthDbContext.UserRoles => Set<AuthUserRole>();
        public DbSet<AuthPermissionRule> PermissionRules => Set<AuthPermissionRule>();
        public DbSet<AuthRefreshToken> RefreshTokens => Set<AuthRefreshToken>();

        public string GetChillTypePrefix() => "ChillSharp.Test";

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.AddChillAuthModel();
        }
    }
}
