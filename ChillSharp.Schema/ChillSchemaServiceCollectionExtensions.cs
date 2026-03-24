using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;
using ChillSharp.Schema.Api;

namespace ChillSharp.Schema;

/// <summary>
/// Provides DI registration helpers for the ChillSharp schema persistence module.
/// </summary>
public static class ChillSchemaServiceCollectionExtensions
{
    /// <summary>
    /// Registers the ChillSharp schema persistence services against an existing EF Core context.
    /// </summary>
    public static IServiceCollection AddChillSchema<TContext>(this IServiceCollection services)
        where TContext : DbContext, IChillSchemaDbContext
    {
        services.AddControllers()
            .AddApplicationPart(Assembly.GetExecutingAssembly())
            .AddControllersAsServices();

        services.TryAddSingleton<IChillSchemaCache, ChillSchemaCache>();

        services.AddScoped<IChillSchemaDbContext>(provider =>
        {
            var context = provider.GetService<TContext>();
            if (context == null)
            {
                throw new InvalidOperationException($"DbContext of type {typeof(TContext).Name} is not registered in the host application.");
            }

            return context;
        });

        services.AddScoped<IChillSchemaService, ChillSchemaService>();
        services.AddScoped<ChillSchemaManagementAccessFilter>();
        return services;
    }
}
