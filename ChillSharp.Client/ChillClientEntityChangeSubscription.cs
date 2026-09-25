namespace ChillSharp.Client;

/// <summary>
/// A registered SignalR subscription. Dispose it asynchronously to unregister it.
/// </summary>
public sealed class ChillClientEntityChangeSubscription : IAsyncDisposable
{
    private readonly Func<Guid, Task> _unsubscribe;
    private readonly Guid _subscriptionId;
    private int _isUnsubscribed;

    internal ChillClientEntityChangeSubscription(
        Guid subscriptionId,
        string chillType,
        Guid? entityGuid,
        Func<Guid, Task> unsubscribe)
    {
        _subscriptionId = subscriptionId;
        ChillType = chillType;
        Guid = entityGuid;
        _unsubscribe = unsubscribe;
    }

    public string ChillType { get; }
    public Guid? Guid { get; }

    public async Task UnsubscribeAsync()
    {
        if (Interlocked.Exchange(ref _isUnsubscribed, 1) == 0)
            await _unsubscribe(_subscriptionId).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync() => new(UnsubscribeAsync());
}
