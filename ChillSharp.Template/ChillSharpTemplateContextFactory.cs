using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ChillSharp.Template;

public sealed class ChillSharpTemplateContextFactory : IDesignTimeDbContextFactory<ChillSharpTemplateContext>
{
    public ChillSharpTemplateContext CreateDbContext(string[] args)
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? ChillSharpTemplateContext.DefaultConnectionString;

        var optionsBuilder = new DbContextOptionsBuilder<ChillSharpTemplateContext>();
        optionsBuilder.UseSqlite(connectionString);

        return new ChillSharpTemplateContext(optionsBuilder.Options);
    }
}
