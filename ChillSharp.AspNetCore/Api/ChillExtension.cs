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

using ChillSharp.Api.Controllers;
using ChillSharp.Auth;
using ChillSharp.Auth.Api;
using ChillSharp.I18n;
using ChillSharp.I18n.Api;
using ChillSharp.Mcp.Api;
using ChillSharp.Schema;
using ChillSharp.Schema.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ChillSharp.Api
{
    /// Hilarius easter egg :)
    /// <summary>
    /// Provides extension methods for integrating the full ChillSharp API stack into an ASP.NET Core application.
    /// </summary>
    public static class ChillApiExtension
    {
        /// <summary>
        /// Registers the base Chill API plus enabled built-in modules against an existing DbContext type.
        /// </summary>
        public static IServiceCollection AddChillApi<TContext>(this IServiceCollection services, Action<ChillApiOptions>? configureOptions = null)
            where TContext : DbContext, IChillContext
        {
            var options = new ChillApiOptions();
            configureOptions?.Invoke(options);

            ValidateEnabledModules<TContext>(options);

            services.AddSingleton(options);
            services.AddSignalR();
            services.AddScoped<IChillEntityChangeDispatcher, ChillEntityChangeDispatcher>();

            services.AddControllers()
                .AddApplicationPart(typeof(ChillController).Assembly)
                .AddControllersAsServices();

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

            services.AddScoped<IChillDtoEngine>(provider =>
            {
                var chillContext = provider.GetRequiredService<IChillContext>();
                var changeDispatcher = provider.GetService<IChillEntityChangeDispatcher>();
                return new ChillDtoEngine(chillContext, changeDispatcher);
            });

            if (options.EnableAuthApi)
            {
                InvokeModuleRegistration<TContext>(services, typeof(ChillAuthApiExtensions), nameof(ChillAuthApiExtensions.AddChillAuthApi));
            }

            if (options.EnableI18nApi)
            {
                InvokeModuleRegistration<TContext>(services, typeof(ChillI18nApiExtensions), nameof(ChillI18nApiExtensions.AddChillI18nApi));
            }

            if (options.EnableSchemaApi)
            {
                InvokeModuleRegistration<TContext>(services, typeof(ChillSchemaServiceCollectionExtensions), nameof(ChillSchemaServiceCollectionExtensions.AddChillSchemaApi));
            }

            if (options.EnableMcpApi)
            {
                InvokeModuleRegistration<TContext>(services, typeof(ChillMcpServiceCollectionExtensions), nameof(ChillMcpServiceCollectionExtensions.AddChillMcpApi));
            }

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

            string year = "2025";
            string authors = "Andrea Piovesan";
            string disclaimer = "This software is using ChillSharp library that is released under the GNU AFFERO GENERAL PUBLIC LICENSE - Version 3";
            string website = "https://chillsharp.dev/";
            string repository = "https://github.com/e-500/chill-sharp";

            string body = $"{{ \"authors\":\"{authors}\", \"year\":\"{year}\", \"disclaimer\":\"{disclaimer}\", \"website\":\"{website}\", \"repository\":\"{repository}\" }}";

            endpoints.MapGet($"/{ApiUrlBasePath}/test", () => "ChillSharp is up and running!");
            endpoints.MapGet($"/{ApiUrlBasePath}/license", () => body);
            var chillEntityChangeHub = endpoints.MapHub<ChillEntityChangeHub>($"/{ApiUrlBasePath}/{ChillEntityChangeHub.HubRouteSuffix}");
            if (options.ProtectedApi)
            {
                chillEntityChangeHub.RequireAuthorization();
            }

            return endpoints;
        }

        private static void ValidateEnabledModules<TContext>(ChillApiOptions options)
            where TContext : DbContext, IChillContext
        {
            var contextType = typeof(TContext);

            if (options.EnableAuthApi && !typeof(IChillAuthDbContext).IsAssignableFrom(contextType))
            {
                throw new InvalidOperationException(
                    $"{nameof(ChillApiOptions.EnableAuthApi)} requires {contextType.Name} to implement {nameof(IChillAuthDbContext)}.");
            }

            if (options.EnableI18nApi && !typeof(IChillI18nDbContext).IsAssignableFrom(contextType))
            {
                throw new InvalidOperationException(
                    $"{nameof(ChillApiOptions.EnableI18nApi)} requires {contextType.Name} to implement {nameof(IChillI18nDbContext)}.");
            }

            if ((options.EnableSchemaApi || options.EnableMcpApi) && !typeof(IChillSchemaDbContext).IsAssignableFrom(contextType))
            {
                var optionName = options.EnableSchemaApi ? nameof(ChillApiOptions.EnableSchemaApi) : nameof(ChillApiOptions.EnableMcpApi);
                throw new InvalidOperationException(
                    $"{optionName} requires {contextType.Name} to implement {nameof(IChillSchemaDbContext)}.");
            }
        }

        private static void InvokeModuleRegistration<TContext>(IServiceCollection services, Type extensionType, string methodName)
            where TContext : DbContext, IChillContext
        {
            var method = extensionType
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                    m.Name == methodName &&
                    m.IsGenericMethodDefinition &&
                    m.GetGenericArguments().Length == 1 &&
                    m.GetParameters().Length == 1 &&
                    m.GetParameters()[0].ParameterType == typeof(IServiceCollection));

            if (method == null)
            {
                throw new InvalidOperationException($"Unable to locate {extensionType.FullName}.{methodName}(IServiceCollection).");
            }

            method.MakeGenericMethod(typeof(TContext)).Invoke(null, [services]);
        }
    }

    public class ChillApiOptions
    {
        /// <summary>
        /// If true, Chill API endpoints will require authentication.
        /// </summary>
        public bool ProtectedApi { get; set; } = false;

        /// <summary>
        /// Enables the embedded ChillSharp auth API module.
        /// </summary>
        public bool EnableAuthApi { get; set; } = true;

        /// <summary>
        /// Enables the embedded ChillSharp i18n API module.
        /// </summary>
        public bool EnableI18nApi { get; set; } = true;

        /// <summary>
        /// Enables the embedded ChillSharp schema API module.
        /// </summary>
        public bool EnableSchemaApi { get; set; } = true;

        /// <summary>
        /// Enables the embedded ChillSharp MCP API module.
        /// </summary>
        public bool EnableMcpApi { get; set; } = true;
    }
}
