using NServiceBus;

namespace OrderProcessingSystem.Messaging.Events;

/// <summary>
/// Event published when an order expires due to being stuck in Processing state.
/// Published from OrderExpirationBackgroundService for orders older than 10 minutes.
/// </summary>
public class OrderExpired : IEvent
{
    public Guid OrderId { get; set; }
}
