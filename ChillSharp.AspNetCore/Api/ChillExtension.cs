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
using ChillSharp.Attachment;
using ChillSharp.Attachment.Api;
using ChillSharp.Auth;
using ChillSharp.Auth.Api;
using ChillSharp.I18n;
using ChillSharp.I18n.Api;
using ChillSharp.Mcp;
using ChillSharp.Mcp.Api;
using ChillSharp.Schema;
using ChillSharp.Schema.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.AspNetCore;
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
                .AddMvcOptions(mvcOptions => mvcOptions.Conventions.Add(new ChillApiRouteBasePathConvention(options.ApiBasePath)))
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
                var schemaResolver = provider.GetService<IChillSchemaResolverService>();
                if (schemaResolver != null)
                {
                    chillContext.RegisterSchemaService(schemaResolver);
                }

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

            if (options.EnableAttachmentApi)
            {
                InvokeModuleRegistration<TContext>(services, typeof(ChillAttachmentApiExtensions), nameof(ChillAttachmentApiExtensions.AddChillAttachmentApi));
            }

            return services;
        }

        /// <summary>
        /// Registers the base Chill API plus ASP.NET Core Identity-backed auth account endpoints against an existing DbContext type.
        /// </summary>
        public static IServiceCollection AddChillApi<TContext, TUser>(this IServiceCollection services, Action<ChillIdentityApiOptions>? configureOptions = null)
            where TContext : DbContext, IChillContext, IChillAuthDbContext
            where TUser : class
        {
            var options = new ChillIdentityApiOptions();
            configureOptions?.Invoke(options);

            services.AddChillApi<TContext>(apiOptions =>
            {
                apiOptions.ProtectedApi = options.ProtectedApi;
                apiOptions.ApiBasePath = options.ApiBasePath;
                apiOptions.EnableAuthApi = false;
                apiOptions.EnableI18nApi = options.EnableI18nApi;
                apiOptions.EnableSchemaApi = options.EnableSchemaApi;
                apiOptions.EnableMcpApi = options.EnableMcpApi;
                apiOptions.EnableAttachmentApi = options.EnableAttachmentApi;
            });

            services.AddChillAuthIdentityApi<TContext, TUser>(identityOptions =>
            {
                identityOptions.AccessTokenLifetime = options.AccessTokenLifetime;
                identityOptions.RefreshTokenLifetime = options.RefreshTokenLifetime;
                identityOptions.CreateChillAuthUserOnRegister = options.CreateChillAuthUserOnRegister;
                identityOptions.ReturnPasswordResetTokensInResponse = options.ReturnPasswordResetTokensInResponse;
                identityOptions.SendPasswordResetEmails = options.SendPasswordResetEmails;
                identityOptions.SmtpHost = options.SmtpHost;
                identityOptions.SmtpPort = options.SmtpPort;
                identityOptions.SmtpEnableSsl = options.SmtpEnableSsl;
                identityOptions.SmtpUserName = options.SmtpUserName;
                identityOptions.SmtpPassword = options.SmtpPassword;
                identityOptions.PasswordResetFromEmail = options.PasswordResetFromEmail;
                identityOptions.PasswordResetFromDisplayName = options.PasswordResetFromDisplayName;
                identityOptions.PasswordResetEmailSubject = options.PasswordResetEmailSubject;
                identityOptions.PasswordResetUrlBase = options.PasswordResetUrlBase;
                identityOptions.InitializeRootUserOnStartup = options.InitializeRootUserOnStartup;
                identityOptions.RootUserName = options.RootUserName;
                identityOptions.RootPassword = options.RootPassword;
                identityOptions.RootEmail = options.RootEmail;
                identityOptions.RootDisplayName = options.RootDisplayName;
                identityOptions.CreateChillAuthUserForRoot = options.CreateChillAuthUserForRoot;
                identityOptions.RootUserNameEnvironmentVariable = options.RootUserNameEnvironmentVariable;
                identityOptions.RootPasswordEnvironmentVariable = options.RootPasswordEnvironmentVariable;
                identityOptions.RootEmailEnvironmentVariable = options.RootEmailEnvironmentVariable;
                identityOptions.RootDisplayNameEnvironmentVariable = options.RootDisplayNameEnvironmentVariable;
            });

            return services;
        }

        /// <summary>
        /// Maps ChillApi controllers to endpoints.
        /// </summary>
        public static IEndpointRouteBuilder MapChillApi(this IEndpointRouteBuilder endpoints, string? ApiUrlBasePath = null)
        {
            var options = endpoints.ServiceProvider.GetRequiredService<ChillApiOptions>();
            var apiUrlBasePath = NormalizeRouteSegment(ApiUrlBasePath ?? options.ApiBasePath);

            var chillControllers = endpoints.MapControllers().WithGroupName(apiUrlBasePath);
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

            var apiRootPath = BuildEndpointPath(apiUrlBasePath, string.Empty);
            endpoints.MapGet(apiRootPath, () => "ChillSharp is up and running!");
            if (apiRootPath.Length > 1)
            {
                endpoints.MapGet($"{apiRootPath}/", () => "ChillSharp is up and running!");
            }

            endpoints.MapGet(BuildEndpointPath(apiUrlBasePath, "test"), () => "ChillSharp is up and running!");
            endpoints.MapGet(BuildEndpointPath(apiUrlBasePath, "license"), () => body);
            var chillEntityChangeHub = endpoints.MapHub<ChillEntityChangeHub>(BuildEndpointPath(apiUrlBasePath, ChillEntityChangeHub.HubRouteSuffix));
            if (options.ProtectedApi)
            {
                chillEntityChangeHub.RequireAuthorization();
            }

            var mcpOptions = endpoints.ServiceProvider.GetService<ChillMcpOptions>();
            if (options.EnableMcpApi && mcpOptions?.Enabled == true)
            {
                var routePattern = NormalizeRoutePattern(mcpOptions.RoutePattern, apiUrlBasePath);
                var mcpEndpoint = endpoints.MapMcp(routePattern);
                if (options.ProtectedApi)
                {
                    mcpEndpoint.RequireAuthorization();
                }
            }

            return endpoints;
        }

        private static string NormalizeRoutePattern(string routePattern, string apiUrlBasePath)
        {
            var normalized = routePattern?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
            {
                normalized = "chill-mcp";
            }
            else if (normalized.Equals("/api/chill-mcp", StringComparison.OrdinalIgnoreCase))
            {
                normalized = "chill-mcp";
            }

            if (!normalized.StartsWith("/"))
            {
                normalized = $"{apiUrlBasePath}/{normalized}";
            }

            if (!normalized.StartsWith("/"))
            {
                normalized = "/" + normalized;
            }

            return normalized.Length > 1
                ? normalized.TrimEnd('/')
                : normalized;
        }

        private static string NormalizeRouteSegment(string routeSegment)
        {
            var normalized = routeSegment?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
            {
                normalized = ChillSharpInitOptions.DefaultApiBasePath;
            }

            normalized = normalized.Trim('/');
            return string.IsNullOrWhiteSpace(normalized) ? string.Empty : normalized;
        }

        private static string BuildEndpointPath(string apiUrlBasePath, string endpointName)
        {
            var normalizedEndpointName = endpointName.Trim('/');
            if (string.IsNullOrWhiteSpace(normalizedEndpointName))
            {
                return string.IsNullOrWhiteSpace(apiUrlBasePath) ? "/" : $"/{apiUrlBasePath}";
            }

            return string.IsNullOrWhiteSpace(apiUrlBasePath)
                ? $"/{normalizedEndpointName}"
                : $"/{apiUrlBasePath}/{normalizedEndpointName}";
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

            if (options.EnableAttachmentApi && !typeof(IChillAttachmentDbContext).IsAssignableFrom(contextType))
            {
                throw new InvalidOperationException(
                    $"{nameof(ChillApiOptions.EnableAttachmentApi)} requires {contextType.Name} to implement {nameof(IChillAttachmentDbContext)}.");
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
                    IsModuleRegistrationMethod(m));

            if (method == null)
            {
                throw new InvalidOperationException($"Unable to locate {extensionType.FullName}.{methodName}(IServiceCollection).");
            }

            var parameters = method.GetParameters();
            var arguments = new object?[parameters.Length];
            arguments[0] = services;

            for (var index = 1; index < parameters.Length; index++)
            {
                arguments[index] = parameters[index].DefaultValue;
            }

            method.MakeGenericMethod(typeof(TContext)).Invoke(null, arguments);
        }

        private static bool IsModuleRegistrationMethod(MethodInfo method)
        {
            var parameters = method.GetParameters();
            return parameters.Length > 0 &&
                parameters[0].ParameterType == typeof(IServiceCollection) &&
                parameters.Skip(1).All(parameter => parameter.IsOptional);
        }

        private sealed class ChillApiRouteBasePathConvention(string apiBasePath) : IControllerModelConvention
        {
            private readonly string _apiBasePath = NormalizeRouteSegment(apiBasePath);

            public void Apply(ControllerModel controller)
            {
                if (!IsChillSharpController(controller))
                {
                    return;
                }

                foreach (var selector in controller.Selectors)
                {
                    var attributeRouteModel = selector.AttributeRouteModel;
                    if (attributeRouteModel?.Template == null)
                    {
                        continue;
                    }

                    attributeRouteModel.Template = RewriteTemplate(attributeRouteModel.Template);
                }
            }

            private string RewriteTemplate(string template)
            {
                var normalizedTemplate = template.TrimStart('/');
                if (normalizedTemplate.Equals("api", StringComparison.OrdinalIgnoreCase))
                {
                    return _apiBasePath;
                }

                const string defaultApiPrefix = "api/";
                if (!normalizedTemplate.StartsWith(defaultApiPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    return template;
                }

                var suffix = normalizedTemplate[defaultApiPrefix.Length..];
                return string.IsNullOrWhiteSpace(_apiBasePath)
                    ? suffix
                    : $"{_apiBasePath}/{suffix}";
            }

            private static bool IsChillSharpController(ControllerModel controller)
            {
                return controller.ControllerType.Namespace?.StartsWith("ChillSharp.", StringComparison.Ordinal) == true;
            }
        }
    }

    public class ChillApiOptions
    {
        /// <summary>
        /// If true, Chill API endpoints will require authentication.
        /// </summary>
        public bool ProtectedApi { get; set; } = false;

        /// <summary>
        /// Gets or sets the base URL path used by ChillSharp API endpoints. Defaults to <c>/api</c>.
        /// </summary>
        public string ApiBasePath { get; set; } = ChillSharpInitOptions.Current.ApiBasePath;

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

        /// <summary>
        /// Enables the embedded ChillSharp attachment API module.
        /// </summary>
        public bool EnableAttachmentApi { get; set; } = true;
    }

    /// <summary>
    /// Configures the combined Chill API and ASP.NET Core Identity-backed auth registration path.
    /// </summary>
    public class ChillIdentityApiOptions : ChillApiOptions
    {
        /// <summary>
        /// Gets or sets the lifetime of issued access tokens.
        /// </summary>
        public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(20);

        /// <summary>
        /// Gets or sets the lifetime of issued refresh tokens.
        /// </summary>
        public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(14);

        /// <summary>
        /// Gets or sets whether register should also create the matching ChillSharp auth user.
        /// </summary>
        public bool CreateChillAuthUserOnRegister { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the reset-token endpoint returns the generated token in the HTTP response.
        /// </summary>
        public bool ReturnPasswordResetTokensInResponse { get; set; } = true;

        /// <summary>
        /// Gets or sets whether password-reset requests should send an email when the target account exposes an email address.
        /// </summary>
        public bool SendPasswordResetEmails { get; set; }

        /// <summary>
        /// Gets or sets the SMTP host used to send password-reset emails.
        /// </summary>
        public string? SmtpHost { get; set; }

        /// <summary>
        /// Gets or sets the SMTP port used to send password-reset emails.
        /// </summary>
        public int SmtpPort { get; set; } = 587;

        /// <summary>
        /// Gets or sets whether the SMTP client should use SSL/TLS.
        /// </summary>
        public bool SmtpEnableSsl { get; set; } = true;

        /// <summary>
        /// Gets or sets the optional SMTP user name used for authenticated delivery.
        /// </summary>
        public string? SmtpUserName { get; set; }

        /// <summary>
        /// Gets or sets the optional SMTP password used for authenticated delivery.
        /// </summary>
        public string? SmtpPassword { get; set; }

        /// <summary>
        /// Gets or sets the sender email address used for password-reset messages.
        /// </summary>
        public string? PasswordResetFromEmail { get; set; }

        /// <summary>
        /// Gets or sets the optional sender display name used for password-reset messages.
        /// </summary>
        public string? PasswordResetFromDisplayName { get; set; }

        /// <summary>
        /// Gets or sets the subject line used for password-reset emails.
        /// </summary>
        public string PasswordResetEmailSubject { get; set; } = "Reset your password";

        /// <summary>
        /// Gets or sets the optional base URL used to build a clickable password-reset link.
        /// </summary>
        public string? PasswordResetUrlBase { get; set; }

        /// <summary>
        /// Gets or sets whether the startup initializer should create a root Identity account when credentials are configured.
        /// </summary>
        public bool InitializeRootUserOnStartup { get; set; } = true;

        /// <summary>
        /// Gets or sets the root user name to initialize. When empty, the value can be resolved from environment variables.
        /// </summary>
        public string? RootUserName { get; set; }

        /// <summary>
        /// Gets or sets the root password to initialize. When empty, the value can be resolved from environment variables.
        /// </summary>
        public string? RootPassword { get; set; }

        /// <summary>
        /// Gets or sets the optional root email address. When empty, the value can be resolved from environment variables.
        /// </summary>
        public string? RootEmail { get; set; }

        /// <summary>
        /// Gets or sets the display name copied into the matching ChillSharp auth user.
        /// </summary>
        public string RootDisplayName { get; set; } = "Root";

        /// <summary>
        /// Gets or sets whether the startup initializer should also create the matching ChillSharp auth user.
        /// </summary>
        public bool CreateChillAuthUserForRoot { get; set; } = true;

        /// <summary>
        /// Gets or sets the environment-variable name used to resolve the root user name.
        /// </summary>
        public string RootUserNameEnvironmentVariable { get; set; } = "CHILLSHARP_AUTH_ROOT_USERNAME";

        /// <summary>
        /// Gets or sets the environment-variable name used to resolve the root password.
        /// </summary>
        public string RootPasswordEnvironmentVariable { get; set; } = "CHILLSHARP_AUTH_ROOT_PASSWORD";

        /// <summary>
        /// Gets or sets the environment-variable name used to resolve the optional root email.
        /// </summary>
        public string RootEmailEnvironmentVariable { get; set; } = "CHILLSHARP_AUTH_ROOT_EMAIL";

        /// <summary>
        /// Gets or sets the environment-variable name used to resolve the optional root display name.
        /// </summary>
        public string RootDisplayNameEnvironmentVariable { get; set; } = "CHILLSHARP_AUTH_ROOT_DISPLAY_NAME";
    }
}
