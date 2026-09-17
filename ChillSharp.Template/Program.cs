using ChillSharp.Api;
using ChillSharp.Auth.Api;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Template;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? ChillSharpTemplateContext.DefaultConnectionString;

        // Add CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOrigin", policy =>
            {
                // Read allowed origins from configuration, with a default fallback for local development
                var corsString = builder.Configuration["CHILLSHARP__CORS_ORIGINS"];

                // If no CORS origins are configured, default to allowing localhost on common development ports
                if (string.IsNullOrWhiteSpace(corsString))
                    corsString = "http://localhost:4200 https://localhost:4200 http://localhost:6202 https://localhost:6202";

                // Split the CORS origins string into an array, trimming whitespace and ignoring empty entries
                var configuredOrigins = corsString.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                // Configure CORS to allow the specified origins, and allow any header and method
                policy.WithOrigins(configuredOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        // Add DBContext
        builder.Services.AddDbContext<ChillSharpTemplateContext>(options =>
            options.UseSqlite(connectionString));

        // Add Identity
        builder.Services.AddIdentityCore<IdentityUser>()
            .AddEntityFrameworkStores<ChillSharpTemplateContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        // Add ChillAuth
        builder.Services.AddAuthentication(ChillAuthIdentityDefaults.AuthenticationScheme)
            .AddChillAuthBearer();
        builder.Services.AddAuthorization();

        // Add Chill API
        builder.Services.AddChillApi<ChillSharpTemplateContext, IdentityUser>(options =>
        {
            options.ApiBasePath = "/api";
            options.ProtectedApi = true;
            options.EnableAuthApi = true;
            options.EnableI18nApi = true;
            options.EnableAttachmentApi = true;
            options.EnableSchemaApi = true;
            options.EnableMcpApi = true;
            // Root user setup
            options.InitializeRootUserOnStartup = true;
            options.CreateChillAuthUserForRoot = true;
            options.RootUserName = builder.Configuration["CHILLSHARP_AUTH_ROOT_USERNAME"] ?? "root";
            options.RootPassword = builder.Configuration["CHILLSHARP_AUTH_ROOT_PASSWORD"] ?? "Pass123$";
            options.RootEmail = builder.Configuration["CHILLSHARP_AUTH_ROOT_EMAIL"] ?? "root@chillsharp.dev";
            options.RootDisplayName = builder.Configuration["CHILLSHARP_AUTH_ROOT_DISPLAY_NAME"] ?? "Root";
        });

        // Add Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Build the app
        var app = builder.Build();

        // Ensure database is created and apply migrations
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ChillSharpTemplateContext>();
            db.Database.EnsureCreated();
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // Enable CORS
        app.UseCors("AllowSpecificOrigin");

        // Enable authentication and authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Map Chill API endpoints
        app.MapChillApi();

        // Map a simple root endpoint
        app.MapGet("/", () => "ChillSharp.Template is running. ChillSharp APIs are available under /api/chill. Open /swagger in development.");

        // Run the app
        app.Run();
    }
}
