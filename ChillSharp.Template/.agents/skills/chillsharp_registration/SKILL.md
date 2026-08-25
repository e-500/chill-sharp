---
name: chillsharp_registration
description: Guidance on registering ChillSharp API, DbContext modules, schema services, auth, and i18n configurations in a ChillSharp client application.
---

# Registering a ChillContext and ChillSharp Modules

This skill describes how ChillSharp is configured in your application and how to manage registrations.

## 1. DbContext Configuration

Your `ChillSharpTemplateContext` (or any custom DbContext you create) must implement `IChillContext` (and optionally the other module interfaces like `IChillAuthDbContext`, `IChillI18nDbContext`, `IChillAttachmentDbContext`, `IChillSchemaDbContext`):

```csharp
public partial class ChillSharpTemplateContext : IdentityDbContext<IdentityUser>, IChillContext, IChillAuthDbContext, IChillI18nDbContext, IChillAttachmentDbContext, IChillSchemaDbContext
{
    // Required by IChillContext to know the namespace prefix of your entity types
    public string GetChillTypePrefix() => "ChillSharp.Template";

    public string GetPrimaryCultureName() => "en-US";
    public string GetSecondaryCultureName() => "it-IT";
    public string GetCurrentUserName() => Environment.UserName; // Replace with principal/HttpContext username mapping

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Add ChillSharp internal entity/model structures
        modelBuilder.AddChillAuthModel();
        modelBuilder.AddChillI18nModel();
        modelBuilder.AddChillAttachmentModel();
        modelBuilder.AddChillSchemaModel();
    }
}
```

## 2. API Registration in Program.cs

In `Program.cs`, ChillSharp is registered via:

```csharp
// Register ASP.NET Core Identity
builder.Services.AddIdentityCore<IdentityUser>()
    .AddEntityFrameworkStores<ChillSharpTemplateContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Add ChillAuth authentication/authorization mechanisms
builder.Services.AddAuthentication(ChillAuthIdentityDefaults.AuthenticationScheme)
    .AddChillAuthBearer();
builder.Services.AddAuthorization();

// Register the complete Chill API
builder.Services.AddChillApi<ChillSharpTemplateContext, IdentityUser>(options =>
{
    options.ApiBasePath = "/api";
    options.ProtectedApi = true; // True to enforce ChillSharp authorization
    options.EnableAuthApi = true;
    options.EnableI18nApi = true;
    options.EnableAttachmentApi = true;
    options.EnableSchemaApi = true;
    options.EnableMcpApi = true;
    
    // Seed a root administrator account on startup if not present
    options.InitializeRootUserOnStartup = true;
    options.CreateChillAuthUserForRoot = true;
    options.RootUserName = builder.Configuration["CHILLSHARP_AUTH_ROOT_USERNAME"] ?? "root";
    options.RootPassword = builder.Configuration["CHILLSHARP_AUTH_ROOT_PASSWORD"] ?? "Pass123$";
});

var app = builder.Build();

// Enable routing middleware
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints (e.g. /api/chill)
app.MapChillApi();
```

## 3. Customize Options
You can toggle modules (e.g., `EnableAttachmentApi`, `EnableMcpApi`) directly inside the options callback of `AddChillApi`.
To add new custom entities, simply declare them in your model namespace and add the corresponding `DbSet<T>` to your `ChillSharpTemplateContext`.
