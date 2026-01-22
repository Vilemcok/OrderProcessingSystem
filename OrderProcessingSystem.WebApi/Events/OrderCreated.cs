using NServiceBus;

namespace OrderProcessingSystem.Messaging.Events;

/// <summary>
/// Event published when a new order is created via the API.
/// Published after order is saved with status = Pending.
/// </summary>
public class OrderCreated : IEvent
{
    public Guid OrderId { get; set; }
}
