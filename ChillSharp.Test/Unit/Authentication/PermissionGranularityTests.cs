using ChillSharp.Auth;
using ChillSharp.Auth.Contracts;
using ChillSharp.Auth.Model;
using ChillSharp.Auth.Services;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Test;

[TestClass]
public class ChillAuthPermissionGranularityTests
{
    [TestMethod]
    public async Task ModuleRule_GrantsAccessToTablesWithinTheModuleAndKeepsOtherModulesDenied()
    {
        await using var fixture = AuthFixture.Create();
        var allowRule = await fixture.AddRuleAsync(PermissionScope.Module, PermissionAction.Query, PermissionEffect.Allow, "Sales");

        var allowed = await fixture.EvaluateEntityAsync(PermissionAction.Query, "Sales.Europe", "Invoice");
        var denied = await fixture.EvaluateEntityAsync(PermissionAction.Query, "Inventory", "StockItem");

        Assert.IsTrue(allowed.IsAllowed);
        Assert.AreEqual(allowRule.Guid, allowed.RuleGuid);
        Assert.IsFalse(denied.IsAllowed);
        Assert.IsNull(denied.RuleGuid);
    }

    [TestMethod]
    public async Task TableRule_OverridesModuleRuleForTheTargetedTableOnly()
    {
        await using var fixture = AuthFixture.Create();
        await fixture.AddRuleAsync(PermissionScope.Module, PermissionAction.FullControl, PermissionEffect.Allow, "Sales");
        var denyDelete = await fixture.AddRuleAsync(PermissionScope.Entity, PermissionAction.Delete, PermissionEffect.Deny, "Sales", "Invoice");

        var invoiceDelete = await fixture.EvaluateEntityAsync(PermissionAction.Delete, "Sales", "Invoice");
        var invoiceUpdate = await fixture.EvaluateEntityAsync(PermissionAction.Update, "Sales", "Invoice");
        var orderDelete = await fixture.EvaluateEntityAsync(PermissionAction.Delete, "Sales", "Order");

        Assert.IsFalse(invoiceDelete.IsAllowed);
        Assert.AreEqual(denyDelete.Guid, invoiceDelete.RuleGuid);
        Assert.IsTrue(invoiceUpdate.IsAllowed);
        Assert.IsTrue(orderDelete.IsAllowed);
    }

    [TestMethod]
    public async Task FieldRule_OverridesRoleFieldAccessWhileEntityAccessRemainsRequired()
    {
        await using var fixture = AuthFixture.Create(withRole: true);
        await fixture.AddRuleAsync(PermissionScope.Entity, PermissionAction.Query, PermissionEffect.Allow, "Sales", "Invoice");
        await fixture.AddRuleAsync(PermissionScope.Entity, PermissionAction.Update, PermissionEffect.Allow, "Sales", "Invoice");
        await fixture.AddRuleAsync(PermissionScope.Property, PermissionAction.See, PermissionEffect.Allow, "Sales", "Invoice", appliesToAllProperties: true, targetRole: true);
        await fixture.AddRuleAsync(PermissionScope.Property, PermissionAction.Modify, PermissionEffect.Allow, "Sales", "Invoice", appliesToAllProperties: true, targetRole: true);
        var denyCost = await fixture.AddRuleAsync(PermissionScope.Property, PermissionAction.See, PermissionEffect.Deny, "Sales", "Invoice", "Cost");

        var entityQuery = await fixture.EvaluateEntityAsync(PermissionAction.Query, "Sales", "Invoice");
        var entityUpdate = await fixture.EvaluateEntityAsync(PermissionAction.Update, "Sales", "Invoice");
        var visibleName = await fixture.EvaluatePropertyAsync(PermissionAction.See, "Sales", "Invoice", "Name");
        var hiddenCost = await fixture.EvaluatePropertyAsync(PermissionAction.See, "Sales", "Invoice", "Cost");
        var editableDescription = await fixture.EvaluatePropertyAsync(PermissionAction.Modify, "Sales", "Invoice", "Description");

        Assert.IsTrue(entityQuery.IsAllowed, "A field SEE grant must be paired with entity QUERY access.");
        Assert.IsTrue(entityUpdate.IsAllowed, "A field MODIFY grant must be paired with entity UPDATE access.");
        Assert.IsTrue(visibleName.IsAllowed);
        Assert.IsFalse(hiddenCost.IsAllowed);
        Assert.AreEqual(denyCost.Guid, hiddenCost.RuleGuid);
        Assert.IsTrue(editableDescription.IsAllowed);
        Assert.AreEqual("Role", editableDescription.RuleSource);
    }

    private sealed class AuthFixture : IAsyncDisposable
    {
        private AuthFixture(AuthTestDbContext context, ChillAuthService service, AuthUser user, AuthRole? role)
        {
            Context = context;
            Service = service;
            User = user;
            Role = role;
        }

        public AuthTestDbContext Context { get; }
        public ChillAuthService Service { get; }
        public AuthUser User { get; }
        public AuthRole? Role { get; }

        public static AuthFixture Create(bool withRole = false)
        {
            var options = new DbContextOptionsBuilder<AuthTestDbContext>()
                .UseInMemoryDatabase($"chill-auth-test-{Guid.NewGuid():N}")
                .Options;
            var context = new AuthTestDbContext(options);
            context.Database.EnsureCreated();

            var user = new AuthUser
            {
                Guid = Guid.NewGuid(),
                ExternalId = "test-user",
                UserName = "test.user",
                DisplayName = "Test User"
            };
            context.Users.Add(user);

            AuthRole? role = null;
            if (withRole)
            {
                role = new AuthRole { Guid = Guid.NewGuid(), Name = "Sales editor", Description = "Sales field permissions" };
                context.Roles.Add(role);
                context.UserRoles.Add(new AuthUserRole { UserGuid = user.Guid, RoleGuid = role.Guid });
            }

            context.SaveChanges();
            var service = new ChillAuthService(context, context, new ChillAuthManagementAccessCache());
            return new AuthFixture(context, service, user, role);
        }

        public Task<PermissionEvaluationResult> EvaluateEntityAsync(PermissionAction action, string module, string table)
        {
            return Service.EvaluateEntityPermissionAsync(new EvaluateEntityPermissionRequest
            {
                UserGuid = User.Guid,
                Action = action,
                Module = module,
                EntityName = table
            });
        }

        public Task<PermissionEvaluationResult> EvaluatePropertyAsync(PermissionAction action, string module, string table, string field)
        {
            return Service.EvaluatePropertyPermissionAsync(new EvaluatePropertyPermissionRequest
            {
                UserGuid = User.Guid,
                Action = action,
                Module = module,
                EntityName = table,
                PropertyName = field
            });
        }

        public Task<AuthPermissionRule> AddRuleAsync(
            PermissionScope scope,
            PermissionAction action,
            PermissionEffect effect,
            string module,
            string? table = null,
            string? field = null,
            bool appliesToAllProperties = false,
            bool targetRole = false)
        {
            return Service.CreatePermissionRuleAsync(new CreateAuthPermissionRuleRequest
            {
                UserGuid = targetRole ? null : User.Guid,
                RoleGuid = targetRole ? Role!.Guid : null,
                Scope = scope,
                Action = action,
                Effect = effect,
                Module = module,
                EntityName = table,
                PropertyName = field,
                AppliesToAllProperties = appliesToAllProperties
            });
        }

        public ValueTask DisposeAsync() => Context.DisposeAsync();
    }

    private sealed class AuthTestDbContext(DbContextOptions<AuthTestDbContext> options) : DbContext(options), IChillAuthDbContext, IChillContext
    {
        public DbSet<AuthUser> Users => Set<AuthUser>();
        public DbSet<AuthRole> Roles => Set<AuthRole>();
        public DbSet<AuthUserRole> UserRoles => Set<AuthUserRole>();
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
