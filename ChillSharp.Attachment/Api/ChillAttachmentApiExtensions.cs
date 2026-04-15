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

using ChillSharp.Attachment.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace ChillSharp.Attachment.Api;

/// <summary>
/// Registers the attachment API surface and services.
/// </summary>
public static class ChillAttachmentApiExtensions
{
    public static IServiceCollection AddChillAttachmentApi<TContext>(this IServiceCollection services)
        where TContext : DbContext, IChillContext, IChillAttachmentDbContext
    {
        return services.AddChillAttachmentApi<TContext>(configureOptions: null);
    }

    public static IServiceCollection AddChillAttachmentApi<TContext>(this IServiceCollection services, Action<ChillAttachmentOptions>? configureOptions)
        where TContext : DbContext, IChillContext, IChillAttachmentDbContext
    {
        services.AddHttpContextAccessor();
        services.AddControllers()
            .AddApplicationPart(Assembly.GetExecutingAssembly())
            .AddControllersAsServices();

        services.AddOptions<ChillAttachmentOptions>();
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        services.AddSingleton<IConfigureOptions<ChillAttachmentOptions>, ChillAttachmentOptionsSetup>();

        services.AddScoped<IChillContext>(provider =>
        {
            var context = provider.GetService<TContext>();
            if (context == null)
            {
                throw new InvalidOperationException($"DbContext of type {typeof(TContext).Name} is not registered in the host application.");
            }

            return context;
        });

        services.AddScoped<IChillAttachmentDbContext>(provider =>
        {
            var context = provider.GetService<TContext>();
            if (context == null)
            {
                throw new InvalidOperationException($"DbContext of type {typeof(TContext).Name} is not registered in the host application.");
            }

            return context;
        });

        services.AddScoped<IChillAttachmentArchive, ChillAttachmentArchive>();
        return services;
    }

    private sealed class ChillAttachmentOptionsSetup : IConfigureOptions<ChillAttachmentOptions>
    {
        public void Configure(ChillAttachmentOptions options)
        {
            var environmentRoot = Environment.GetEnvironmentVariable(ChillAttachmentOptions.ArchiveRootEnvironmentVariableName)?.Trim();
            if (!string.IsNullOrWhiteSpace(environmentRoot))
            {
                options.ArchiveRoot = environmentRoot;
            }
        }
    }
}
