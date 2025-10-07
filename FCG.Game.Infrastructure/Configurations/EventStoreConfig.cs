// FCG.Game.Infrastructure/Configurations/EventStoreConfig.cs

using EventStore.Client;

namespace FCG.Game.Infrastructure.Configurations;

public class EventStoreConfig
{
    public static EventStoreClient CreateClient(string connectionString)
    {
        var settings = EventStoreClientSettings.Create(connectionString);

        // Configurações adicionais de conexão
        settings.ConnectivitySettings.MaxDiscoverAttempts = 10;
        settings.ConnectivitySettings.DiscoveryInterval = TimeSpan.FromSeconds(1);
        settings.ConnectivitySettings.GossipTimeout = TimeSpan.FromSeconds(5);
        settings.ConnectivitySettings.KeepAliveInterval = TimeSpan.FromSeconds(10);
        settings.ConnectivitySettings.KeepAliveTimeout = TimeSpan.FromSeconds(10);

        // Log level - usa o logger padrão da aplicação
        settings.LoggerFactory = null;

        var client = new EventStoreClient(settings);
        return client;
    }

    public static async Task VerifyConnectionAsync(EventStoreClient client)
    {
        try
        {
            // Tentar ler do stream $all com catch para stream não encontrado
            // Se conseguir conectar (mesmo que o stream não exista), está OK
            await client.ReadStreamAsync(
                Direction.Forwards,
                "$all",
                StreamPosition.Start,
                maxCount: 1
            ).ToListAsync();

            Console.WriteLine("✅ EventStore conectado com sucesso!");
        }
        catch (StreamNotFoundException)
        {
            // Stream não existe ainda, mas conexão funcionou!
            Console.WriteLine("✅ EventStore conectado com sucesso! (banco vazio)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro ao conectar com EventStore: {ex.Message}");
            throw;
        }
    }

    public static async Task InitializeProjectionsAsync(EventStoreClient client)
    {
        // Aqui você pode criar projeções customizadas se necessário
        Console.WriteLine("📊 Projeções do EventStore inicializadas");
        await Task.CompletedTask;
    }
}