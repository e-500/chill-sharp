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
                policy.WithOrigins("http://localhost:4200",
                    "https://localhost:4200",
                    "http://localhost:6202",
                    "https://localhost:6202")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        builder.Services.AddDbContext<ChillSharpTemplateContext>(options =>
            options.UseSqlite(connectionString));

        builder.Services.AddIdentityCore<IdentityUser>()
            .AddEntityFrameworkStores<ChillSharpTemplateContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        builder.Services.AddAuthentication(ChillAuthIdentityDefaults.AuthenticationScheme)
            .AddChillAuthBearer();
        builder.Services.AddAuthorization();

        builder.Services.AddChillApi<ChillSharpTemplateContext, IdentityUser>(options =>
        {
            options.EnableAuthApi = true;
            options.EnableI18nApi = true;
            options.EnableAttachmentApi = true;
            options.EnableSchemaApi = true;
            options.EnableMcpApi = true;
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ChillSharpTemplateContext>();
            db.Database.EnsureCreated();
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // Enable CORS
        app.UseCors("AllowSpecificOrigin");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapChillApi();
        app.MapGet("/", () => "ChillSharp.Template is running. ChillSharp APIs are available under /api/chill. Open /swagger in development.");

        app.Run();
    }
}
