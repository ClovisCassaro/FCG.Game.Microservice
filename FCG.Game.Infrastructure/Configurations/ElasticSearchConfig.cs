using Nest;
using FCG.Game.Infrastructure.Documents;

namespace FCG.Game.Infrastructure.Configurations;

public class ElasticSearchConfig
{
    public static ElasticClient CreateClient(string uri)
    {
        var settings = new ConnectionSettings(new Uri(uri))
            .EnableDebugMode()
            .PrettyJson()
            .DefaultMappingFor<GameDocument>(m => m
                .IndexName("games")
                .IdProperty(p => p.Id)
            )
            .DefaultMappingFor<OrderDocument>(m => m
                .IndexName("orders")
                .IdProperty(p => p.Id)
            );

        return new ElasticClient(settings);
    }

    public static async Task InitializeIndicesAsync(ElasticClient client)
    {
        await CreateGamesIndexAsync(client);
        await CreateOrdersIndexAsync(client);
    }

    private static async Task CreateGamesIndexAsync(ElasticClient client)
    {
        var indexName = "games";
        var existsResponse = await client.Indices.ExistsAsync(indexName);

        if (existsResponse.Exists)
            return;

        var createIndexResponse = await client.Indices.CreateAsync(indexName, c => c
            .Settings(s => s
                .NumberOfShards(1)
                .NumberOfReplicas(1)
                .Analysis(a => a
                    .Analyzers(an => an
                        .Custom("game_analyzer", ca => ca
                            .Tokenizer("standard")
                            .Filters("lowercase", "asciifolding")
                        )
                    )
                )
            )
            .Map<GameDocument>(m => m
                .AutoMap()
                .Properties(p => p
                    .Keyword(k => k.Name(n => n.Id))
                    .Text(t => t
                        .Name(n => n.Title)
                        .Analyzer("game_analyzer")
                        .Fields(f => f
                            .Keyword(k => k.Name("keyword"))
                        )
                    )
                    .Text(t => t.Name(n => n.Description).Analyzer("game_analyzer"))
                    .Keyword(k => k.Name(n => n.Genre))
                    .Number(n => n.Name(nn => nn.Price).Type(NumberType.ScaledFloat).ScalingFactor(100))
                    .Keyword(k => k.Name(n => n.Publisher))
                    .Date(d => d.Name(n => n.ReleaseDate))
                    .Number(n => n.Name(nn => nn.PopularityScore).Type(NumberType.Integer))
                    .Number(n => n.Name(nn => nn.TotalSales).Type(NumberType.Integer))
                    .Keyword(k => k.Name(n => n.Tags))
                    .Boolean(b => b.Name(n => n.IsActive))
                )
            )
        );

        if (!createIndexResponse.IsValid)
            throw new Exception($"Erro ao criar índice games: {createIndexResponse.DebugInformation}");
    }

    private static async Task CreateOrdersIndexAsync(ElasticClient client)
    {
        var indexName = "orders";
        var existsResponse = await client.Indices.ExistsAsync(indexName);

        if (existsResponse.Exists)
            return;

        var createIndexResponse = await client.Indices.CreateAsync(indexName, c => c
            .Map<OrderDocument>(m => m
                .AutoMap()
                .Properties(p => p
                    .Keyword(k => k.Name(n => n.Id))
                    .Keyword(k => k.Name(n => n.UserId))
                    .Number(n => n.Name(nn => nn.TotalAmount).Type(NumberType.ScaledFloat).ScalingFactor(100))
                    .Keyword(k => k.Name(n => n.Status))
                    .Date(d => d.Name(n => n.CreatedAt))
                    .Date(d => d.Name(n => n.CompletedAt))
                    .Nested<OrderItemDocument>(n => n
                        .Name(nn => nn.Items)
                        .Properties(ip => ip
                            .Keyword(k => k.Name(i => i.GameId))
                            .Text(t => t.Name(i => i.GameTitle))
                            .Keyword(k => k.Name(i => i.Genre))
                            .Number(nu => nu.Name(i => i.Price).Type(NumberType.ScaledFloat).ScalingFactor(100))
                            .Number(nu => nu.Name(i => i.Quantity).Type(NumberType.Integer))
                        )
                    )
                )
            )
        );

        if (!createIndexResponse.IsValid)
            throw new Exception($"Erro ao criar índice orders: {createIndexResponse.DebugInformation}");
    }
}