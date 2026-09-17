using ChillSharp.Api;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Template;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? ChillSharpTemplateContext.DefaultConnectionString;

        builder.Services.AddDbContext<ChillSharpTemplateContext>(options =>
            options.UseSqlite(connectionString));

        builder.Services.AddChillApi<ChillSharpTemplateContext>(options =>
        {
            options.EnableAuthApi = false;
            options.EnableI18nApi = false;
            options.EnableAttachmentApi = false;
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

        app.MapChillApi();
        app.MapGet("/", () => "ChillSharp.Template is running. Open /swagger in development.");

        app.Run();
    }
}
