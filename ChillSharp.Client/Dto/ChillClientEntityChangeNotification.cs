namespace ChillSharp.Client.Dto;

/// <summary>
/// Describes a Chill entity change received through the notification hub.
/// </summary>
public sealed class ChillClientEntityChangeNotification
{
    public string ChillType { get; set; } = string.Empty;
    public Guid Guid { get; set; }
    public string Action { get; set; } = string.Empty;
}

/// <summary>
/// Receives entity change batches for a type or entity subscription.
/// </summary>
public delegate Task ChillEntityChangeCallback(IReadOnlyList<ChillClientEntityChangeNotification> changes);
