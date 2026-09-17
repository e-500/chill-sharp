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

using ChillSharp.Auth.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChillSharp.Auth.Services;

internal sealed class ChillAuthRootUserInitializer<TUser> : IHostedService
    where TUser : class
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ChillAuthIdentityApiOptions _options;
    private readonly ILogger<ChillAuthRootUserInitializer<TUser>> _logger;

    public ChillAuthRootUserInitializer(
        IServiceProvider serviceProvider,
        IOptions<ChillAuthIdentityApiOptions> options,
        ILogger<ChillAuthRootUserInitializer<TUser>> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.InitializeRootUserOnStartup)
        {
            return;
        }

        var rootUserName = FirstNonEmpty(_options.RootUserName, ReadEnvironmentValue(_options.RootUserNameEnvironmentVariable));
        var rootPassword = FirstNonEmpty(_options.RootPassword, ReadEnvironmentValue(_options.RootPasswordEnvironmentVariable));
        var rootEmail = FirstNonEmpty(_options.RootEmail, ReadEnvironmentValue(_options.RootEmailEnvironmentVariable));
        var rootDisplayName = FirstNonEmpty(ReadEnvironmentValue(_options.RootDisplayNameEnvironmentVariable), _options.RootDisplayName, rootUserName);

        if (string.IsNullOrWhiteSpace(rootUserName) && string.IsNullOrWhiteSpace(rootPassword))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(rootUserName) || string.IsNullOrWhiteSpace(rootPassword))
        {
            throw new InvalidOperationException("Root-user initialization requires both user name and password.");
        }

        await using var scope = _serviceProvider.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
        var userStore = scope.ServiceProvider.GetRequiredService<IUserStore<TUser>>();
        var authService = scope.ServiceProvider.GetRequiredService<IChillAuthService>();

        var existingUser = await userManager.FindByNameAsync(rootUserName);
        if (existingUser == null)
        {
            var user = Activator.CreateInstance<TUser>() ?? throw new InvalidOperationException($"Cannot create an instance of {typeof(TUser).Name}.");
            await userStore.SetUserNameAsync(user, rootUserName, cancellationToken);

            if (userStore is IUserEmailStore<TUser> emailStore && !string.IsNullOrWhiteSpace(rootEmail))
            {
                await emailStore.SetEmailAsync(user, rootEmail, cancellationToken);
            }

            var createResult = await userManager.CreateAsync(user, rootPassword);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(FormatIdentityErrors(createResult));
            }

            existingUser = user;
            _logger.LogInformation("Initialized ChillSharp root Identity user '{RootUserName}'.", rootUserName);
        }

        if (!_options.CreateChillAuthUserForRoot)
        {
            return;
        }

        var identityUserId = await userManager.GetUserIdAsync(existingUser);
        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            throw new InvalidOperationException("The root Identity user did not expose a user id.");
        }

        if (await authService.GetUserByExternalIdAsync(identityUserId, cancellationToken) != null)
        {
            return;
        }

        var userName = await userManager.GetUserNameAsync(existingUser) ?? rootUserName;
        await authService.CreateUserAsync(new CreateAuthUserRequest
        {
            ExternalId = identityUserId,
            UserName = userName,
            DisplayName = string.IsNullOrWhiteSpace(rootDisplayName) ? userName : rootDisplayName,
            IsActive = true,
            CanManagePermissions = true
        }, cancellationToken);

        _logger.LogInformation("Initialized ChillSharp root auth user '{RootUserName}'.", userName);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static string? ReadEnvironmentValue(string variableName)
    {
        return string.IsNullOrWhiteSpace(variableName) ? null : Environment.GetEnvironmentVariable(variableName)?.Trim();
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static string FormatIdentityErrors(IdentityResult result)
    {
        return string.Join("; ", result.Errors.Select(x => $"{x.Code}: {x.Description}"));
    }
}
