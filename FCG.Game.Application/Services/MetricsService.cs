// ============================================
// CAMINHO: FCG.Game.Application\Services\MetricsService.cs
// AÇÃO: SUBSTITUIR apenas a PARTE FINAL (DTOs) do arquivo
// OU substituir o arquivo INTEIRO se preferir
// CORREÇÃO: Adicionou valores padrão nos DTOs
// ============================================

using FCG.Game.Infrastructure.Documents;
using Nest;

namespace FCG.Game.Application.Services;

public class MetricsService
{
    private readonly ElasticClient _elasticClient;

    public MetricsService(ElasticClient elasticClient)
    {
        _elasticClient = elasticClient;
    }

    public async Task<TopGamesMetrics> GetTopGamesAsync(int limit = 10)
    {
        var response = await _elasticClient.SearchAsync<GameDocument>(s => s
            .Size(0)
            .Aggregations(a => a
                .Terms("top_by_sales", t => t
                    .Field(f => f.Id.Suffix("keyword"))
                    .Size(limit)
                    .Order(o => o.Descending("total_sales"))
                    .Aggregations(aa => aa
                        .Sum("total_sales", sum => sum.Field(f => f.TotalSales))
                        .Terms("game_info", ti => ti
                            .Field(f => f.Title.Suffix("keyword"))
                            .Size(1)
                        )
                    )
                )
            )
        );

        var topGames = new List<GameSalesMetric>();
        var buckets = response.Aggregations.Terms("top_by_sales").Buckets;

        foreach (var bucket in buckets)
        {
            var sales = (int)bucket.Sum("total_sales").Value.GetValueOrDefault();
            topGames.Add(new GameSalesMetric
            {
                GameId = Guid.Parse(bucket.Key),
                TotalSales = sales
            });
        }

        return new TopGamesMetrics { TopGames = topGames };
    }

    public async Task<GenreMetrics> GetGenreStatisticsAsync()
    {
        var response = await _elasticClient.SearchAsync<GameDocument>(s => s
            .Size(0)
            .Aggregations(a => a
                .Terms("genres", t => t
                    .Field(f => f.Genre)
                    .Size(50)
                    .Aggregations(aa => aa
                        .Sum("total_sales", sum => sum.Field(f => f.TotalSales))
                        .Average("avg_price", avg => avg.Field(f => f.Price))
                        .ValueCount("game_count", vc => vc.Field(f => f.Id))
                    )
                )
            )
        );

        var genreStats = response.Aggregations.Terms("genres").Buckets.Select(b => new GenreStatistic
        {
            Genre = b.Key,
            TotalGames = (int)b.ValueCount("game_count").Value.GetValueOrDefault(),
            TotalSales = (int)b.Sum("total_sales").Value.GetValueOrDefault(),
            AveragePrice = (decimal)b.Average("avg_price").Value.GetValueOrDefault()
        }).ToList();

        return new GenreMetrics { Genres = genreStats };
    }

    public async Task<SalesMetrics> GetSalesMetricsAsync(DateTime startDate, DateTime endDate)
    {
        var response = await _elasticClient.SearchAsync<OrderDocument>(s => s
            .Index("orders")
            .Size(0)
            .Query(q => q
                .Bool(b => b
                    .Must(
                        m => m.DateRange(dr => dr
                            .Field(f => f.CreatedAt)
                            .GreaterThanOrEquals(startDate)
                            .LessThanOrEquals(endDate)
                        ),
                        m => m.Term(t => t.Field(f => f.Status).Value("Completed"))
                    )
                )
            )
            .Aggregations(a => a
                .Sum("total_revenue", sum => sum.Field(f => f.TotalAmount))
                .ValueCount("total_orders", vc => vc.Field(f => f.Id))
                .Average("average_order_value", avg => avg.Field(f => f.TotalAmount))
                .DateHistogram("sales_over_time", dh => dh
                    .Field(f => f.CreatedAt)
                    .CalendarInterval(DateInterval.Day)
                    .Aggregations(aa => aa
                        .Sum("daily_revenue", sum => sum.Field(f => f.TotalAmount))
                    )
                )
            )
        );

        var aggs = response.Aggregations;

        return new SalesMetrics
        {
            TotalRevenue = (decimal)aggs.Sum("total_revenue").Value.GetValueOrDefault(),
            TotalOrders = (int)aggs.ValueCount("total_orders").Value.GetValueOrDefault(),
            AverageOrderValue = (decimal)aggs.Average("average_order_value").Value.GetValueOrDefault(),
            StartDate = startDate,
            EndDate = endDate
        };
    }

    public async Task<UserBehaviorMetrics> GetUserBehaviorMetricsAsync()
    {
        var response = await _elasticClient.SearchAsync<OrderDocument>(s => s
            .Index("orders")
            .Size(0)
            .Aggregations(a => a
                .Terms("top_buyers", t => t
                    .Field(f => f.UserId)
                    .Size(10)
                    .Order(o => o.Descending("total_spent"))
                    .Aggregations(aa => aa
                        .Sum("total_spent", sum => sum.Field(f => f.TotalAmount))
                        .ValueCount("order_count", vc => vc.Field(f => f.Id))
                    )
                )
                .Nested("genre_preferences", n => n
                    .Path(p => p.Items)
                    .Aggregations(aa => aa
                        .Terms("popular_genres", t => t
                            .Field("items.genre")
                            .Size(10)
                        )
                    )
                )
            )
        );

        return new UserBehaviorMetrics
        {
            TopBuyers = response.Aggregations.Terms("top_buyers").Buckets
                .Select(b => new TopBuyer
                {
                    UserId = Guid.Parse(b.Key),
                    TotalSpent = (decimal)b.Sum("total_spent").Value.GetValueOrDefault(),
                    OrderCount = (int)b.ValueCount("order_count").Value.GetValueOrDefault()
                }).ToList()
        };
    }
}

// ============================================
// DTOs de Métricas - CORRIGIDOS
// ============================================

public class TopGamesMetrics
{
    public List<GameSalesMetric> TopGames { get; set; } = new();
}

public class GameSalesMetric
{
    public Guid GameId { get; set; }
    public int TotalSales { get; set; }
}

public class GenreMetrics
{
    public List<GenreStatistic> Genres { get; set; } = new();
}

public class GenreStatistic
{
    public string Genre { get; set; } = string.Empty;
    public int TotalGames { get; set; }
    public int TotalSales { get; set; }
    public decimal AveragePrice { get; set; }
}

public class SalesMetrics
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class UserBehaviorMetrics
{
    public List<TopBuyer> TopBuyers { get; set; } = new();
}

public class TopBuyer
{
    public Guid UserId { get; set; }
    public decimal TotalSpent { get; set; }
    public int OrderCount { get; set; }
}