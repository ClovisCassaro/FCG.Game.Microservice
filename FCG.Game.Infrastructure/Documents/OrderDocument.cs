// FCG.Game.Infrastructure/Documents/OrderDocument.cs

namespace FCG.Game.Infrastructure.Documents;

public class OrderDocument
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public List<OrderItemDocument> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class OrderItemDocument
{
    public Guid GameId { get; set; }
    public string GameTitle { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}