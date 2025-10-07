using FCG.Game.Domain.Entities;
using FCG.Game.Domain.Events;
using FCG.Game.Infrastructure.Documents;
using FCG.Game.Infrastructure.EventStore;
using Nest;

namespace FCG.Game.Application.Services;

public class OrderService
{
    private readonly ElasticClient _elasticClient;
    private readonly EventStoreRepository _eventStore;

    public OrderService(ElasticClient elasticClient, EventStoreRepository eventStore)
    {
        _elasticClient = elasticClient;
        _eventStore = eventStore;
    }

    public async Task<Guid> CreateOrderAsync(Guid userId, List<OrderItemRequest> items)
    {
        // Validar e buscar jogos
        var gameIds = items.Select(i => i.GameId).ToList();
        var games = await GetGamesByIdsAsync(gameIds);

        if (games.Count != gameIds.Count)
            throw new InvalidOperationException("Alguns jogos não foram encontrados");

        // Criar itens do pedido
        var orderItems = items.Select(item =>
        {
            var game = games.First(g => g.Id == item.GameId);
            return new OrderItem
            {
                GameId = game.Id,
                GameTitle = game.Title,
                Price = game.Price,
                Quantity = item.Quantity
            };
        }).ToList();

        var order = new Order(userId, orderItems);

        // Salvar evento
        var orderItemsData = orderItems.Select(i => new OrderItemData(
            i.GameId, i.GameTitle, i.Price, i.Quantity
        )).ToList();

        var @event = new OrderCreatedEvent(
            order.Id,
            userId,
            orderItemsData,
            order.TotalAmount
        );

        await _eventStore.AppendEventAsync($"order-{order.Id}", @event);

        // Indexar no Elasticsearch
        var document = new OrderDocument
        {
            Id = order.Id,
            UserId = userId,
            Items = orderItems.Select(i => new OrderItemDocument
            {
                GameId = i.GameId,
                GameTitle = i.GameTitle,
                Genre = games.First(g => g.Id == i.GameId).Genre,
                Price = i.Price,
                Quantity = i.Quantity
            }).ToList(),
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString(),
            CreatedAt = order.CreatedAt
        };

        await _elasticClient.IndexDocumentAsync(document);

        return order.Id;
    }

    public async Task<bool> CompleteOrderAsync(Guid orderId, Guid userId)
    {
        var orderDoc = await GetOrderByIdAsync(orderId);

        if (orderDoc == null || orderDoc.UserId != userId)
            return false;

        if (orderDoc.Status != OrderStatus.Pending.ToString())
            throw new InvalidOperationException("Pedido já foi processado");

        // Salvar evento
        var @event = new OrderCompletedEvent(orderId, userId, DateTime.UtcNow);
        await _eventStore.AppendEventAsync($"order-{orderId}", @event);

        // Atualizar status
        var updateResponse = await _elasticClient.UpdateAsync<OrderDocument>(orderId, u => u
            .Doc(new OrderDocument
            {
                Status = OrderStatus.Completed.ToString(),
                CompletedAt = DateTime.UtcNow
            })
        );

        // Atualizar estatísticas dos jogos
        foreach (var item in orderDoc.Items)
        {
            await IncrementGameSalesAsync(item.GameId);

            // Evento de compra do jogo
            var purchaseEvent = new GamePurchasedEvent(
                item.GameId, userId, orderId, item.Price);
            await _eventStore.AppendEventAsync($"game-{item.GameId}", purchaseEvent);
        }

        return updateResponse.IsValid;
    }

    public async Task<OrderDocument?> GetOrderByIdAsync(Guid orderId)
    {
        var response = await _elasticClient.GetAsync<OrderDocument>(orderId, i => i.Index("orders"));
        return response.Source;
    }

    public async Task<List<OrderDocument>> GetUserOrdersAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        var from = (page - 1) * pageSize;

        var response = await _elasticClient.SearchAsync<OrderDocument>(s => s
            .Index("orders")
            .From(from)
            .Size(pageSize)
            .Query(q => q
                .Term(t => t.Field(f => f.UserId).Value(userId))
            )
            .Sort(so => so
                .Descending(d => d.CreatedAt)
            )
        );

        return response.Documents.ToList();
    }

    private async Task<List<GameDocument>> GetGamesByIdsAsync(List<Guid> gameIds)
    {
        var response = await _elasticClient.SearchAsync<GameDocument>(s => s
            .Query(q => q
                .Ids(i => i.Values(gameIds))
            )
            .Size(gameIds.Count)
        );

        return response.Documents.ToList();
    }

    private async Task IncrementGameSalesAsync(Guid gameId)
    {
        var script = new InlineScript(@"
            ctx._source.totalSales += 1;
            ctx._source.popularityScore += 10;
        ");

        await _elasticClient.UpdateAsync<GameDocument>(gameId, u => u
            .Script(s => s.Source(script.Source))
        );
    }
}

public record OrderItemRequest(Guid GameId, int Quantity);