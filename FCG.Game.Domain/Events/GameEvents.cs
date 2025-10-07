namespace FCG.Game.Domain.Events;

public abstract record DomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public string EventType { get; init; }

    protected DomainEvent()
    {
        EventType = GetType().Name;
    }
}

public record GameCreatedEvent(
    Guid GameId,
    string Title,
    string Description,
    string Genre,
    decimal Price,
    string Publisher,
    DateTime ReleaseDate,
    List<string> Tags
) : DomainEvent;

public record GamePriceChangedEvent(
    Guid GameId,
    decimal OldPrice,
    decimal NewPrice
) : DomainEvent;

public record GamePurchasedEvent(
    Guid GameId,
    Guid UserId,
    Guid OrderId,
    decimal Price
) : DomainEvent;