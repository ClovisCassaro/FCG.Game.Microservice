namespace FCG.Game.Domain.Events;

public record OrderCreatedEvent(
    Guid OrderId,
    Guid UserId,
    List<OrderItemData> Items,
    decimal TotalAmount
) : DomainEvent;

public record OrderCompletedEvent(
    Guid OrderId,
    Guid UserId,
    DateTime CompletedAt
) : DomainEvent;

public record OrderCancelledEvent(
    Guid OrderId,
    Guid UserId,
    string Reason
) : DomainEvent;

public record OrderFailedEvent(
    Guid OrderId,
    Guid UserId,
    string Reason
) : DomainEvent;

public record OrderItemData(
    Guid GameId,
    string GameTitle,
    decimal Price,
    int Quantity
);