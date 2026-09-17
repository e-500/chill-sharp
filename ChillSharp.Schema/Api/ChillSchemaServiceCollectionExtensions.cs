/*
 * ChillSharp is a lightweight .NET library that sits on top of Entity Framework Core 
 * and turns an existing data model into a fully working REST API with almost no setup.
 * Copyright (C) 2025 Andrea Piovesan
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace ChillSharp.Schema.Api;

/// <summary>
/// Provides DI registration helpers for the ChillSharp schema persistence module.
/// </summary>
public static class ChillSchemaServiceCollectionExtensions
{
    /// <summary>
    /// Registers the ChillSharp schema persistence services against an existing EF Core context.
    /// </summary>
    public static IServiceCollection AddChillSchemaApi<TContext>(this IServiceCollection services)
        where TContext : DbContext, IChillContext, IChillSchemaDbContext
    {
        services.AddControllers()
            .AddApplicationPart(Assembly.GetExecutingAssembly())
            .AddControllersAsServices();

        services.TryAddSingleton<IChillSchemaCache, ChillSchemaCache>();

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

        services.AddScoped<IChillSchemaRuntimeContext>(provider =>
        {
            var context = provider.GetRequiredService<IChillContext>();
            return new ChillContextSchemaRuntimeContext(context);
        });

        services.AddScoped<IChillSchemaResolverService, ChillSchemaService>();
        services.AddScoped<IChillSchemaService, ChillSchemaService>();
        services.AddScoped<ChillSchemaManagementAccessFilter>();
        return services;
    }
}
