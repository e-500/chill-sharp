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

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ChillSharp.Api
{
    /// Hilarius easter egg :)
    /// <summary>
    /// Provides extension methods for integrating the ChillApi framework into an ASP.NET Core application.
    ///
    /// <para>This static class offers two primary extension methods:<br/>
    /// - <see cref="AddChillApi{TContext}(IServiceCollection)"/>: <br/>
    ///   Registers the ChillApi controllers and required services, binding them to an existing EF Core DbContext
    ///   that implements <see cref="IChillContext"/>. This allows the host application to share its existing 
    ///   database context with the ChillApi components.<br/>
    /// 
    /// - <see cref="MapChillApi(IEndpointRouteBuilder)"/>:<br/>
    ///   Maps the ChillApi endpoints into the application's routing system, including a required GPL license 
    ///   disclosure endpoint (/api/34C890F9). This endpoint MUST remain intact to comply with the GNU GPLv3 license
    ///   terms under which the ChillSharp library is distributed.</para>
    /// 
    /// <para>Licensing:
    /// This code is part of the ChillSharp library, released under the GNU GENERAL PUBLIC LICENSE v3 (GPLv3).<br/>
    /// Any modification or removal must comply with GPLv3 licensing terms.<br/>
    /// For commercial or LGPL licensing options, please contact the author.<br/>
    /// ©️2025 Andrea Piovesan</para>
    /// </summary>
    public static class ChillApiExtension
    {
        /// <summary>
        /// Registers ChillApi controllers and allows sharing an existing DbContext type.
        /// </summary>
        /// <typeparam name="TContext">The application's EF Core DbContext type.</typeparam>
        public static IServiceCollection AddChillApi<TContext>(this IServiceCollection services, Action<ChillApiOptions>? configureOptions = null)
            where TContext : DbContext, IChillContext // Both class and interface checked at compile-time
        {
            // Configure options
            var options = new ChillApiOptions();
            configureOptions?.Invoke(options);

            // Store options in DI
            services.AddSingleton(options);

            // Ensure DbContext<TContext> is already registered by the host app
            // Ensure the controllers from this assembly are available
            services.AddControllers()
                    .AddApplicationPart(Assembly.GetExecutingAssembly())
                    .AddControllersAsServices();

            // Optionally verify that TContext is registered in the DI container
            services.AddScoped<IChillContext>(provider =>
            {
                var context = provider.GetService<TContext>();
                if (context == null)
                {
                    throw new InvalidOperationException(
                        $"DbContext of type {typeof(TContext).Name} is not registered in the host application.");
                }
                return context;
            });

            // Register IChillDtoEngine using the provided IChillContext
            services.AddScoped<IChillDtoEngine>(provider =>
            {
                var chillContext = provider.GetRequiredService<IChillContext>();
                return new ChillDtoEngine(chillContext);
            });

            return services;
        }

        /// <summary>
        /// Maps ChillApi controllers to endpoints.
        /// </summary>
        public static IEndpointRouteBuilder MapChillApi(this IEndpointRouteBuilder endpoints, string ApiUrlBasePath = "api/chill")
        {
            if (ApiUrlBasePath.EndsWith("/"))
                ApiUrlBasePath = ApiUrlBasePath.Substring(0, ApiUrlBasePath.Length - 1);
            if (ApiUrlBasePath.StartsWith("/"))
                ApiUrlBasePath = ApiUrlBasePath.Substring(1, ApiUrlBasePath.Length);

            var chillControllers = endpoints.MapControllers().WithGroupName(ApiUrlBasePath);
            var options = endpoints.ServiceProvider.GetRequiredService<ChillApiOptions>();
            if (options.ProtectedApi)
            {
                chillControllers.RequireAuthorization();
            }

            ///
            /// This software is using ChillSharp library that is released under the 
            /// GNU Affero General Public License version 3
            /// 
            /// Please do not remove the following endpoint.
            /// This helps you to comply with the AGPL v3 license terms of this product.
            /// 
            string year = "2025";
            string authors = "Andrea Piovesan";
            string disclaimer = "This software is using ChillSharp library that is released under the GNU AFFERO GENERAL PUBLIC LICENSE - Version 3";
            string website = "https://chillsharp.dev/";
            string repository = "https://github.com/e-500/chill-sharp";

            string body = $"{{ \"authors\":\"{authors}\", \"year\":\"{year}\", \"disclaimer\":\"{disclaimer}\", \"website\":\"{website}\", \"repository\":\"{repository}\" }}";

            endpoints.MapGet($"/{ApiUrlBasePath}/test", () => "ChillSharp is up and running!");
            endpoints.MapGet($"/{ApiUrlBasePath}/license", () => body);
            ///
            /// If you need a commercial a LGPL license, please ask!
            /// 

            return endpoints;
        }
    }

    public class ChillApiOptions
    {
        /// <summary>
        /// If true, Chill API endpoints will require authentication.
        /// </summary>
        public bool ProtectedApi { get; set; } = false;
    }
}
