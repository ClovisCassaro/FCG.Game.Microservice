namespace FCG.Game.Domain.Entities;

public class Order
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public List<OrderItem> Items { get; private set; }
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public Order(Guid userId, List<OrderItem> items)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Items = items;
        TotalAmount = items.Sum(i => i.Price * i.Quantity);
        Status = OrderStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Pedido já foi processado");

        Status = OrderStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Completed)
            throw new InvalidOperationException("Não é possível cancelar pedido completado");

        Status = OrderStatus.Cancelled;
    }

    public void Fail(string reason)
    {
        Status = OrderStatus.Failed;
    }
}

public class OrderItem
{
    public Guid GameId { get; set; }
    public string GameTitle { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public enum OrderStatus
{
    Pending = 0,
    Completed = 1,
    Cancelled = 2,
    Failed = 3
}