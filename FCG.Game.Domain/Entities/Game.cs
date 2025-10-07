namespace FCG.Game.Domain.Entities;

public class Game
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public string Genre { get; private set; }
    public decimal Price { get; private set; }
    public string Publisher { get; private set; }
    public DateTime ReleaseDate { get; private set; }
    public int PopularityScore { get; private set; }
    public int TotalSales { get; private set; }
    public List<string> Tags { get; private set; }
    public string CoverImageUrl { get; private set; }
    public bool IsActive { get; private set; }

    public Game(
        Guid id,
        string title,
        string description,
        string genre,
        decimal price,
        string publisher,
        DateTime releaseDate,
        List<string> tags,
        string coverImageUrl)
    {
        Id = id;
        Title = title;
        Description = description;
        Genre = genre;
        Price = price;
        Publisher = publisher;
        ReleaseDate = releaseDate;
        PopularityScore = 0;
        TotalSales = 0;
        Tags = tags ?? new List<string>();
        CoverImageUrl = coverImageUrl;
        IsActive = true;
    }

    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice < 0)
            throw new ArgumentException("Preço não pode ser negativo");
        Price = newPrice;
    }

    public void IncrementSales()
    {
        TotalSales++;
        PopularityScore += 10; // Cada venda aumenta popularidade
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}