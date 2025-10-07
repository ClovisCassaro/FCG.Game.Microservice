// ============================================
// CAMINHO: FCG.Game.Application\Services\GameService.cs
// AÇÃO: SUBSTITUIR o arquivo existente
// CORREÇÃO: Linha 172 - problema com Select
// ============================================

using FCG.Game.Domain.Entities;
using FCG.Game.Domain.Events;
using FCG.Game.Infrastructure.Documents;
using FCG.Game.Infrastructure.EventStore;
using Nest;

namespace FCG.Game.Application.Services;

public class GameService
{
    private readonly ElasticClient _elasticClient;
    private readonly EventStoreRepository _eventStore;

    public GameService(ElasticClient elasticClient, EventStoreRepository eventStore)
    {
        _elasticClient = elasticClient;
        _eventStore = eventStore;
    }

    public async Task<Guid> CreateGameAsync(CreateGameDto dto)
    {
        var gameId = Guid.NewGuid();
        var game = new Domain.Entities.Game(
            gameId,
            dto.Title,
            dto.Description,
            dto.Genre,
            dto.Price,
            dto.Publisher,
            dto.ReleaseDate,
            dto.Tags,
            dto.CoverImageUrl
        );

        var @event = new GameCreatedEvent(
            gameId, dto.Title, dto.Description, dto.Genre,
            dto.Price, dto.Publisher, dto.ReleaseDate, dto.Tags);

        await _eventStore.AppendEventAsync($"game-{gameId}", @event);

        var document = new GameDocument
        {
            Id = gameId,
            Title = dto.Title,
            Description = dto.Description,
            Genre = dto.Genre,
            Price = dto.Price,
            Publisher = dto.Publisher,
            ReleaseDate = dto.ReleaseDate,
            PopularityScore = 0,
            TotalSales = 0,
            Tags = dto.Tags,
            CoverImageUrl = dto.CoverImageUrl,
            IsActive = true,
            IndexedAt = DateTime.UtcNow
        };

        await _elasticClient.IndexDocumentAsync(document);
        return gameId;
    }

    public async Task<GameDocument?> GetGameByIdAsync(Guid gameId)
    {
        var response = await _elasticClient.GetAsync<GameDocument>(gameId);
        return response.Source;
    }

    public async Task<List<GameDocument>> SearchGamesAsync(string searchTerm, int page = 1, int pageSize = 20)
    {
        var from = (page - 1) * pageSize;

        var response = await _elasticClient.SearchAsync<GameDocument>(s => s
            .From(from)
            .Size(pageSize)
            .Query(q => q
                .Bool(b => b
                    .Must(m => m
                        .MultiMatch(mm => mm
                            .Fields(f => f
                                .Field(ff => ff.Title, boost: 3.0)
                                .Field(ff => ff.Description, boost: 1.5)
                                .Field(ff => ff.Tags, boost: 2.0)
                                .Field(ff => ff.Genre)
                                .Field(ff => ff.Publisher)
                            )
                            .Query(searchTerm)
                            .Fuzziness(Fuzziness.Auto)
                        )
                    )
                    .Filter(f => f
                        .Term(t => t.Field(ff => ff.IsActive).Value(true))
                    )
                )
            )
            .Sort(so => so
                .Descending(SortSpecialField.Score)
                .Descending(d => d.PopularityScore)
            )
        );

        return response.Documents.ToList();
    }

    public async Task<List<GameDocument>> GetGamesByGenreAsync(string genre, int limit = 20)
    {
        var response = await _elasticClient.SearchAsync<GameDocument>(s => s
            .Size(limit)
            .Query(q => q
                .Bool(b => b
                    .Must(m => m
                        .Term(t => t.Field(f => f.Genre).Value(genre))
                    )
                    .Filter(f => f
                        .Term(t => t.Field(ff => ff.IsActive).Value(true))
                    )
                )
            )
            .Sort(so => so
                .Descending(d => d.PopularityScore)
                .Descending(d => d.TotalSales)
            )
        );

        return response.Documents.ToList();
    }

    public async Task<List<GameDocument>> GetRecommendationsAsync(Guid userId, int limit = 10)
    {
        // Buscar histórico de compras do usuário
        var userOrders = await GetUserOrderHistoryAsync(userId);

        if (!userOrders.Any())
        {
            return await GetMostPopularGamesAsync(limit);
        }

        // Extrair gêneros dos jogos comprados
        var purchasedGenres = userOrders
            .SelectMany(o => o.Items)
            .Select(i => i.Genre)
            .Where(g => !string.IsNullOrEmpty(g))
            .ToList();

        if (!purchasedGenres.Any())
        {
            return await GetMostPopularGamesAsync(limit);
        }

        // Encontrar gênero mais frequente
        var genreFrequency = purchasedGenres
            .GroupBy(g => g)
            .OrderByDescending(g => g.Count())
            .ToList();

        var mostFrequentGenre = genreFrequency.First().Key;

        // IDs dos jogos já comprados
        var purchasedGameIds = userOrders
            .SelectMany(o => o.Items)
            .Select(i => i.GameId)
            .ToList();

        // Construir queries para gêneros secundários
        var secondaryGenreQueries = new List<Func<QueryContainerDescriptor<GameDocument>, QueryContainer>>();

        // Adicionar gênero principal
        secondaryGenreQueries.Add(sh => sh.Term(t => t.Field(f => f.Genre).Value(mostFrequentGenre).Boost(3.0)));

        // Adicionar outros gêneros (até 2)
        foreach (var genreGroup in genreFrequency.Skip(1).Take(2))
        {
            var genre = genreGroup.Key;
            secondaryGenreQueries.Add(sh => sh.Term(t => t.Field(f => f.Genre).Value(genre).Boost(1.5)));
        }

        // Buscar jogos similares excluindo os já comprados
        var response = await _elasticClient.SearchAsync<GameDocument>(s => s
            .Size(limit)
            .Query(q => q
                .Bool(b => b
                    .Should(secondaryGenreQueries.ToArray())
                    .MustNot(mn => mn
                        .Ids(i => i.Values(purchasedGameIds))
                    )
                    .Filter(f => f
                        .Term(t => t.Field(ff => ff.IsActive).Value(true))
                    )
                    .MinimumShouldMatch(1)
                )
            )
            .Sort(so => so
                .Descending(SortSpecialField.Score)
                .Descending(d => d.PopularityScore)
            )
        );

        return response.Documents.ToList();
    }

    public async Task<List<GameDocument>> GetMostPopularGamesAsync(int limit = 10)
    {
        var response = await _elasticClient.SearchAsync<GameDocument>(s => s
            .Size(limit)
            .Query(q => q
                .Term(t => t.Field(f => f.IsActive).Value(true))
            )
            .Sort(so => so
                .Descending(d => d.TotalSales)
                .Descending(d => d.PopularityScore)
            )
        );

        return response.Documents.ToList();
    }

    private async Task<List<OrderDocument>> GetUserOrderHistoryAsync(Guid userId)
    {
        var response = await _elasticClient.SearchAsync<OrderDocument>(s => s
            .Index("orders")
            .Query(q => q
                .Bool(b => b
                    .Must(
                        m => m.Term(t => t.Field(f => f.UserId).Value(userId)),
                        m => m.Term(t => t.Field(f => f.Status).Value("Completed"))
                    )
                )
            )
            .Size(100)
        );

        return response.Documents.ToList();
    }
}

public record CreateGameDto(
    string Title,
    string Description,
    string Genre,
    decimal Price,
    string Publisher,
    DateTime ReleaseDate,
    List<string> Tags,
    string CoverImageUrl
);