/*
 * Author: Andrea Piovesan
 * Year: 2025
 * License: GNU Affero General Public License (AGPL) version 3
 *
 * Disclaimer:
 * You are free to use, modify, and distribute it under the terms of the AGPL v3 license.
 * This code comes with no warranty; use it at your own risk.
 * 
 * For further information, please refer to README and LICENSE files.
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
        public static IServiceCollection AddChillApi<TContext>(this IServiceCollection services)
            where TContext : DbContext, IChillContext // Both class and interface checked at compile-time
        {
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
            endpoints.MapControllers().WithGroupName(ApiUrlBasePath);

            ///
            /// This software is using ChillSharp library that is released under the GNU GENERAL PUBLIC LICENSE - Version 3, 29 June 2007
            /// 
            /// The following endpoint MUST NOT be removed or altered.
            /// It is required to comply with the AGPL v3 license terms of this product.
            /// Removing or modifying this endpoint would violate the licensing conditions.
            /// 
            endpoints.MapGet($"/{ApiUrlBasePath}/34C890F9", () => "{ disclaimer = \"This software is using ChillSharp library that is released under the GNU GENERAL PUBLIC LICENSE - Version 3, 29 June 2007\" }");
            ///
            /// If you need a commercial a LGPL license, please ask!
            /// 
            /// The Author: Andrea Piovesan, Year: 2025
            /// 

            return endpoints;
        }
    }
}
