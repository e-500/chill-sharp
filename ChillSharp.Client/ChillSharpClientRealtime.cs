using ChillSharp.Client.Dto;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace ChillSharp.Client;

public partial class ChillSharpClient
{
    private sealed record EntityChangeRegistration(string ChillType, Guid? Guid);
    private sealed record EntityChangeSubscriptionState(
        string ChillType,
        Guid? Guid,
        ChillEntityChangeCallback Callback);

    private readonly SemaphoreSlim _entityChangeOperationLock = new(1, 1);
    private readonly ConcurrentDictionary<Guid, EntityChangeSubscriptionState> _entityChangeSubscriptions = new();
    private readonly Dictionary<EntityChangeRegistration, int> _entityChangeRegistrationCounts = new();
    private HubConnection? _notificationConnection;
    private TaskCompletionSource<bool>? _entityChangeReconnectSignal;

    /// <summary>
    /// Subscribes to changes for every entity of a Chill type, or for one entity when <paramref name="guid"/> is supplied.
    /// The callback receives only notifications matching this subscription.
    /// </summary>
    public async Task<ChillClientEntityChangeSubscription> SubscribeToEntityChangesAsync(
        string chillType,
        ChillEntityChangeCallback callback,
        Guid? guid = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedChillType = NormalizeRequiredValue(chillType, nameof(chillType));
        ArgumentNullException.ThrowIfNull(callback);

        await _entityChangeOperationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var connection = await EnsureNotificationConnectionAsync(cancellationToken).ConfigureAwait(false);
            var registration = new EntityChangeRegistration(normalizedChillType, guid);
            _entityChangeRegistrationCounts.TryGetValue(registration, out var count);
            if (count == 0)
                await connection.InvokeAsync("Register", normalizedChillType, guid, cancellationToken).ConfigureAwait(false);

            var subscriptionId = Guid.NewGuid();
            _entityChangeSubscriptions[subscriptionId] = new EntityChangeSubscriptionState(normalizedChillType, guid, callback);
            _entityChangeRegistrationCounts[registration] = count + 1;

            return new ChillClientEntityChangeSubscription(subscriptionId, normalizedChillType, guid, UnsubscribeEntityChangesAsync);
        }
        finally
        {
            _entityChangeOperationLock.Release();
        }
    }

    /// <summary>
    /// Stops the shared notification connection and clears all local subscriptions.
    /// </summary>
    public async Task DisconnectEntityChangesAsync(CancellationToken cancellationToken = default)
    {
        await _entityChangeOperationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _entityChangeSubscriptions.Clear();
            _entityChangeRegistrationCounts.Clear();
            var connection = _notificationConnection;
            _notificationConnection = null;
            Interlocked.Exchange(ref _entityChangeReconnectSignal, null)?.TrySetResult(true);
            if (connection != null)
            {
                await connection.StopAsync(cancellationToken).ConfigureAwait(false);
                await connection.DisposeAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            _entityChangeOperationLock.Release();
        }
    }

    private async Task<HubConnection> EnsureNotificationConnectionAsync(CancellationToken cancellationToken)
    {
        if (_notificationConnection != null)
        {
            if (_notificationConnection.State == HubConnectionState.Reconnecting)
            {
                var reconnectTask = Volatile.Read(ref _entityChangeReconnectSignal)?.Task;
                if (reconnectTask != null)
                    await reconnectTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            if (_notificationConnection.State == HubConnectionState.Disconnected)
                await _notificationConnection.StartAsync(cancellationToken).ConfigureAwait(false);
            return _notificationConnection;
        }

        var connection = new HubConnectionBuilder()
            .WithUrl(BuildApiUrl("notify"), options =>
            {
                options.AccessTokenProvider = () =>
                {
                    if (CanUseAuthentication())
                        GetAuthTokenWithPasswordIfNecessary();
                    return Task.FromResult<string?>(_AccessToken ?? string.Empty);
                };
            })
            .WithAutomaticReconnect()
            .AddJsonProtocol(options => options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true)
            .Build();

        connection.On<ChillClientEntityChangeNotification[]>("EntitiesChanged", DispatchEntityChangeNotificationsAsync);
        connection.Reconnecting += OnNotificationConnectionReconnectingAsync;
        connection.Reconnected += OnNotificationConnectionReconnectedAsync;
        connection.Closed += OnNotificationConnectionClosedAsync;
        _notificationConnection = connection;
        try
        {
            await connection.StartAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch
        {
            _notificationConnection = null;
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private async Task OnNotificationConnectionReconnectedAsync(string? connectionId)
    {
        Interlocked.Exchange(ref _entityChangeReconnectSignal, null)?.TrySetResult(true);
        await _entityChangeOperationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var connection = _notificationConnection;
            if (connection == null || connection.State != HubConnectionState.Connected)
                return;

            foreach (var registration in _entityChangeRegistrationCounts.Keys.ToArray())
                await connection.InvokeAsync("Register", registration.ChillType, registration.Guid).ConfigureAwait(false);
        }
        finally
        {
            _entityChangeOperationLock.Release();
        }
    }

    private Task OnNotificationConnectionReconnectingAsync(Exception? exception)
    {
        Interlocked.Exchange(
            ref _entityChangeReconnectSignal,
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));
        return Task.CompletedTask;
    }

    private Task OnNotificationConnectionClosedAsync(Exception? exception)
    {
        Interlocked.Exchange(ref _entityChangeReconnectSignal, null)?.TrySetResult(true);
        return Task.CompletedTask;
    }

    private async Task UnsubscribeEntityChangesAsync(Guid subscriptionId)
    {
        await _entityChangeOperationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!_entityChangeSubscriptions.TryRemove(subscriptionId, out var subscription))
                return;

            var registration = new EntityChangeRegistration(subscription.ChillType, subscription.Guid);
            if (!_entityChangeRegistrationCounts.TryGetValue(registration, out var count))
                return;

            if (count > 1)
            {
                _entityChangeRegistrationCounts[registration] = count - 1;
                return;
            }

            _entityChangeRegistrationCounts.Remove(registration);
            var connection = _notificationConnection;
            if (connection?.State == HubConnectionState.Connected)
                await connection.InvokeAsync("Unregister", subscription.ChillType, subscription.Guid).ConfigureAwait(false);
        }
        finally
        {
            _entityChangeOperationLock.Release();
        }
    }

    private async Task DispatchEntityChangeNotificationsAsync(ChillClientEntityChangeNotification[]? notifications)
    {
        if (notifications == null || notifications.Length == 0)
            return;

        var subscriptions = _entityChangeSubscriptions.Values.ToArray();
        foreach (var subscription in subscriptions)
        {
            var matchingChanges = notifications
                .Where(change => change != null
                    && string.Equals(change.ChillType, subscription.ChillType, StringComparison.Ordinal)
                    && (!subscription.Guid.HasValue || change.Guid == subscription.Guid.Value))
                .ToArray();
            if (matchingChanges.Length > 0)
                await subscription.Callback(matchingChanges).ConfigureAwait(false);
        }
    }
}
