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

using ChillSharp.I18n.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;
using Microsoft.AspNetCore.Http;

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

        services.TryAddSingleton<IChillI18nCache, ChillI18nCache>();
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

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
