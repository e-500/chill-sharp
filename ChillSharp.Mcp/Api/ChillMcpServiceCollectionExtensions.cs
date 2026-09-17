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

using ChillSharp.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;
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
    public static IServiceCollection AddChillMcpApi<TContext>(
        this IServiceCollection services,
        Action<ChillMcpOptions>? configureOptions = null)
        where TContext : DbContext, IChillContext, IChillSchemaDbContext
    {
        var options = new ChillMcpOptions();
        configureOptions?.Invoke(options);
        services.AddSingleton(options);

        if (!options.Enabled)
        {
            return services;
        }

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
        services.TryAddScoped<IChillSchemaRuntimeContext>(provider =>
            new ChillContextSchemaRuntimeContext(provider.GetRequiredService<IChillContext>()));
        services.TryAddScoped<IChillSchemaResolverService, ChillSchemaService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ChillMcpSchemaDiscoveryService>();
        services.AddScoped<ChillMcpTools>();
        services.AddMcpServer()
            .WithHttpTransport()
            .WithTools<ChillMcpTools>();
        return services;
    }
}
