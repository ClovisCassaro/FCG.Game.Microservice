// FCG.Game.Infrastructure/Documents/GameDocument.cs

namespace FCG.Game.Infrastructure.Documents;

public class GameDocument
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Publisher { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public int PopularityScore { get; set; }
    public int TotalSales { get; set; }
    public List<string> Tags { get; set; } = new();
    public string CoverImageUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime IndexedAt { get; set; }
}