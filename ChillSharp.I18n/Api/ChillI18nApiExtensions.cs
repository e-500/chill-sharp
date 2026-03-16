using ChillSharp.I18n.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ChillSharp.I18n.Api;

/// <summary>
/// Registers the ChillSharp i18n controllers and services for ASP.NET Core applications.
/// </summary>
public static class ChillI18nApiExtensions
{
    /// <summary>
    /// Adds the ChillSharp i18n API surface and binds it to an existing i18n-aware DbContext.
    /// </summary>
    public static IServiceCollection AddChillI18nApi<TContext>(this IServiceCollection services)
        where TContext : DbContext, IChillI18nDbContext
    {
        services.AddControllers()
            .AddApplicationPart(Assembly.GetExecutingAssembly())
            .AddControllersAsServices();

        services.AddScoped<IChillI18nDbContext>(provider =>
        {
            var context = provider.GetService<TContext>();
            if (context is null)
            {
                throw new InvalidOperationException($"DbContext of type {typeof(TContext).Name} is not registered in the host application.");
            }

            return context;
        });

        services.AddScoped<IChillI18nService, ChillI18nService>();
        return services;
    }
}

