// ============================================
// CAMINHO: FCG.Game.Infrastructure\EventStore\EventStoreRepository.cs
// AÇÃO: SUBSTITUIR o arquivo existente completamente
// CORREÇÃO: Linha 222 - mudou StreamPosition.Start para FromStream.Start
// ============================================

using EventStore.Client;
using System.Text;
using System.Text.Json;

namespace FCG.Game.Infrastructure.EventStore;

public class EventStoreRepository
{
    private readonly EventStoreClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public EventStoreRepository(EventStoreClient client)
    {
        _client = client;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Adiciona um evento a um stream
    /// </summary>
    public async Task AppendEventAsync<T>(string streamName, T @event, IDictionary<string, string>? metadata = null) where T : class
    {
        var eventType = typeof(T).Name;
        var jsonData = JsonSerializer.Serialize(@event, _jsonOptions);
        var jsonMetadata = metadata != null
            ? JsonSerializer.Serialize(metadata, _jsonOptions)
            : "{}";

        var eventData = new EventData(
            Uuid.NewUuid(),
            eventType,
            Encoding.UTF8.GetBytes(jsonData),
            Encoding.UTF8.GetBytes(jsonMetadata)
        );

        try
        {
            await _client.AppendToStreamAsync(
                streamName,
                StreamState.Any,
                new[] { eventData }
            );
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao adicionar evento ao stream '{streamName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Adiciona múltiplos eventos a um stream
    /// </summary>
    public async Task AppendEventsAsync<T>(string streamName, IEnumerable<T> events) where T : class
    {
        var eventDataList = events.Select(@event =>
        {
            var eventType = typeof(T).Name;
            var jsonData = JsonSerializer.Serialize(@event, _jsonOptions);

            return new EventData(
                Uuid.NewUuid(),
                eventType,
                Encoding.UTF8.GetBytes(jsonData)
            );
        }).ToList();

        if (!eventDataList.Any())
            return;

        try
        {
            await _client.AppendToStreamAsync(
                streamName,
                StreamState.Any,
                eventDataList
            );
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao adicionar eventos ao stream '{streamName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Lê todos os eventos de um stream
    /// </summary>
    public async Task<List<T>> ReadEventsAsync<T>(string streamName) where T : class
    {
        var events = new List<T>();

        try
        {
            var result = _client.ReadStreamAsync(
                Direction.Forwards,
                streamName,
                StreamPosition.Start
            );

            await foreach (var resolvedEvent in result)
            {
                if (resolvedEvent.Event.Data.Length > 0)
                {
                    var jsonData = Encoding.UTF8.GetString(resolvedEvent.Event.Data.ToArray());
                    var @event = JsonSerializer.Deserialize<T>(jsonData, _jsonOptions);

                    if (@event != null)
                        events.Add(@event);
                }
            }
        }
        catch (StreamNotFoundException)
        {
            // Stream não existe ainda, retorna lista vazia
            return events;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao ler eventos do stream '{streamName}': {ex.Message}", ex);
        }

        return events;
    }

    /// <summary>
    /// Lê eventos de um stream a partir de uma posição específica
    /// </summary>
    public async Task<List<T>> ReadEventsFromPositionAsync<T>(
        string streamName,
        ulong position,
        long maxCount = 100) where T : class
    {
        var events = new List<T>();

        try
        {
            var result = _client.ReadStreamAsync(
                Direction.Forwards,
                streamName,
                StreamPosition.FromInt64((long)position),
                maxCount
            );

            await foreach (var resolvedEvent in result)
            {
                if (resolvedEvent.Event.Data.Length > 0)
                {
                    var jsonData = Encoding.UTF8.GetString(resolvedEvent.Event.Data.ToArray());
                    var @event = JsonSerializer.Deserialize<T>(jsonData, _jsonOptions);

                    if (@event != null)
                        events.Add(@event);
                }
            }
        }
        catch (StreamNotFoundException)
        {
            return events;
        }

        return events;
    }

    /// <summary>
    /// Verifica se um stream existe
    /// </summary>
    public async Task<bool> StreamExistsAsync(string streamName)
    {
        try
        {
            var result = _client.ReadStreamAsync(
                Direction.Forwards,
                streamName,
                StreamPosition.Start,
                maxCount: 1
            );

            await result.ToListAsync();
            return true;
        }
        catch (StreamNotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// Deleta um stream (soft delete)
    /// </summary>
    public async Task DeleteStreamAsync(string streamName)
    {
        try
        {
            await _client.DeleteAsync(
                streamName,
                StreamState.Any
            );
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao deletar stream '{streamName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Lê eventos de todos os streams ($all)
    /// </summary>
    public async Task<List<ResolvedEvent>> ReadAllEventsAsync(
        Position position,
        long maxCount = 100)
    {
        var events = new List<ResolvedEvent>();

        var result = _client.ReadAllAsync(
            Direction.Forwards,
            position,
            maxCount
        );

        await foreach (var resolvedEvent in result)
        {
            events.Add(resolvedEvent);
        }

        return events;
    }
}