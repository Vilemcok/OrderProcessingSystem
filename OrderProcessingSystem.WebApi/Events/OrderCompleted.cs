using NServiceBus;

namespace OrderProcessingSystem.Messaging.Events;

/// <summary>
/// Event published when order processing completes successfully.
/// Published from OrderCreatedHandler after payment simulation succeeds (50% chance).
/// </summary>
public class OrderCompleted : IEvent
{
    public Guid OrderId { get; set; }
}
