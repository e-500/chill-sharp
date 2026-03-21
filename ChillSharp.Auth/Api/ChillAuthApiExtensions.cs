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

using ChillSharp.Auth.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ChillSharp.Auth.Api;

/// <summary>
/// Registers the ChillSharp authorization controllers and services for ASP.NET Core applications.
/// </summary>
public static class ChillAuthApiExtensions
{
    /// <summary>
    /// Adds the ChillSharp authorization API surface and binds it to an existing auth-aware DbContext.
    /// </summary>
    /// <typeparam name="TContext">The host application DbContext type.</typeparam>
    /// <param name="services">The service collection receiving the auth registrations.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddChillAuthApi<TContext>(this IServiceCollection services)
        where TContext : DbContext, IChillAuthDbContext
    {
        services.AddControllers()
            .AddApplicationPart(Assembly.GetExecutingAssembly())
            .AddControllersAsServices();

        services.AddScoped<IChillAuthDbContext>(provider =>
        {
            var context = provider.GetService<TContext>();
            if (context is null)
            {
                throw new InvalidOperationException($"DbContext of type {typeof(TContext).Name} is not registered in the host application.");
            }

            return context;
        });

        services.AddScoped<IChillAuthService, ChillAuthService>();
        services.AddChillAuthIdentityIntegration();
        services.AddScoped<ChillAuthManagementAccessFilter>();
        return services;
    }

    /// <summary>
    /// Adds the ChillSharp authorization API plus ASP.NET Core Identity account endpoints backed by the host application's user type.
    /// </summary>
    /// <typeparam name="TContext">The host application DbContext type.</typeparam>
    /// <typeparam name="TUser">The ASP.NET Core Identity user type.</typeparam>
    /// <param name="services">The service collection receiving the auth registrations.</param>
    /// <param name="configureOptions">Optional configuration for token lifetimes and password-reset endpoint behavior.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddChillAuthIdentityApi<TContext, TUser>(this IServiceCollection services, Action<ChillAuthIdentityApiOptions>? configureOptions = null)
        where TContext : DbContext, IChillAuthDbContext
        where TUser : class
    {
        services.AddChillAuthApi<TContext>();
        services.AddDataProtection();
        services.AddOptions<ChillAuthIdentityApiOptions>();
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        services.AddScoped<IChillAuthTokenService, ChillAuthTokenService>();
        services.AddScoped<IChillAuthIdentityService, ChillAuthIdentityService<TUser>>();
        services.AddScoped<IChillAuthPasswordResetEmailSender, ChillAuthPasswordResetEmailSender>();
        services.AddHostedService<ChillAuthRootUserInitializer<TUser>>();
        return services;
    }
}
