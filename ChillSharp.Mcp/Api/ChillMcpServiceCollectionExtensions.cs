using ChillSharp.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace ChillSharp.Mcp.Api;

/// <summary>
/// Registers the ChillSharp MCP resource module for ASP.NET Core applications.
/// </summary>
public static class ChillMcpServiceCollectionExtensions
{
    /// <summary>
    /// Adds the ChillSharp MCP module and binds it to an existing Chill context and schema-aware DbContext.
    /// </summary>
    public static IServiceCollection AddChillMcpApi<TContext>(this IServiceCollection services)
        where TContext : DbContext, IChillContext, IChillSchemaDbContext
    {
        services.AddControllers()
            .AddApplicationPart(Assembly.GetExecutingAssembly())
            .AddControllersAsServices();

        services.AddScoped<IChillContext>(provider =>
        {
            var context = provider.GetService<TContext>();
            if (context == null)
            {
                throw new InvalidOperationException($"DbContext of type {typeof(TContext).Name} is not registered in the host application.");
            }

            return context;
        });

        services.AddScoped<IChillSchemaDbContext>(provider =>
        {
            var context = provider.GetService<TContext>();
            if (context == null)
            {
                throw new InvalidOperationException($"DbContext of type {typeof(TContext).Name} is not registered in the host application.");
            }

            return context;
        });

        services.TryAddSingleton<IChillSchemaCache, ChillSchemaCache>();
        services.TryAddScoped<IChillSchemaResolverService, ChillSchemaService>();
        services.AddScoped<IChillMcpService, ChillMcpService>();
        return services;
    }
}
