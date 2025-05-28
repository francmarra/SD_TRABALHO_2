using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using Shared.Models;
using Shared.RabbitMQ;
using Shared.MongoDB;

class Program
{
    static RabbitMQSubscriber? subscriber;
    static MongoDBService? mongoService;
    static ConfigService? configService;
    static string serverId = "";
    static string continentCode = "";
    static string continentName = "";
    static volatile bool encerrarExecucao = false;    static async Task Main()
    {
        // Initialize configuration service
        configService = new ConfigService();

        // Get server ID from user
        while (true)
        {
            Console.Write("ID do Servidor: ");
            serverId = Console.ReadLine()?.Trim() ?? "";
            if (string.IsNullOrEmpty(serverId) || !serverId.Contains('-') || !ContinentConfig.IsValidServerId(serverId))
            {
                Console.WriteLine("ID inválido. O ID deve estar no formato <Continente>-S (ex: EU-S, NA-S).");
                Console.WriteLine("Continentes suportados: EU, NA, SA, AF, AS, OC, AQ");
                continue;
            }

            // Verify server is configured
            if (!await IsServerConfiguredAsync(serverId))
            {
                Console.WriteLine($"Servidor {serverId} não está configurado! Insira um ID válido.");
                continue;
            }

            break;
        }

        // Load server configuration and continent information
        var serverConfig = await configService.GetServerConfigAsync(serverId);
        if (serverConfig != null)
        {
            continentCode = serverConfig.ContinentCode;
            continentName = serverConfig.Continent;
            Console.WriteLine($"[{serverId}] Configuração carregada: {continentName} ({continentCode})");
        }
        else
        {
            Console.WriteLine($"Erro: Não foi possível carregar a configuração para {serverId}");
            return;
        }        // Initialize MongoDB
        Console.WriteLine($"[{serverId}] Inicializando MongoDB...");
        try
        {
            mongoService = new MongoDBService();
            var connectionTest = await mongoService.TestConnectionAsync();
            if (!connectionTest)
            {
                Console.WriteLine($"[{serverId}] Falha na conexão com MongoDB. Continuando apenas com arquivos locais.");
                mongoService = null;
            }
            else
            {
                Console.WriteLine($"[{serverId}] MongoDB conectado com sucesso!");
                await mongoService.LogSystemEventAsync(serverId, "STARTUP", $"Servidor {serverId} iniciado com sucesso");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{serverId}] Erro ao conectar com MongoDB: {ex.Message}. Continuando apenas com arquivos locais.");
            mongoService = null;
        }
        
        // Setup continent-specific server queue
        string serverQueue = ContinentConfig.GetQueueName(continentCode, "server");
        Console.WriteLine($"[{serverId}] Inicializando RabbitMQ Subscriber...");

        try
        {
            subscriber = new RabbitMQSubscriber(serverQueue);
            
            // Subscribe to data messages
            subscriber.SubscribeToData(OnDataReceived);
            
            // Subscribe to shutdown messages
            subscriber.SubscribeToShutdown(OnShutdownReceived);
            
            Console.WriteLine($"[{serverId}] RabbitMQ configurado com sucesso.");
            Console.WriteLine($"[{serverId}] Queue: {serverQueue}");
            Console.WriteLine($"[{serverId}] A ouvir mensagens de dados e shutdown via RabbitMQ...\n");        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{serverId}] Erro ao configurar RabbitMQ: {ex.Message}");
            return;
        }

        // Monitorar comando de desligamento do console
        Task.Run(() => MonitorarComandoDesligar());

        // Mantém a aplicação em execução
        while (!encerrarExecucao)
        {
            await Task.Delay(200);
        }        Console.WriteLine($"[{serverId}] Encerrando execução...");
        
        // Log shutdown event
        if (mongoService != null)
        {
            try
            {
                await mongoService.LogSystemEventAsync(serverId, "SHUTDOWN", $"Servidor {serverId} encerrando");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{serverId}] Erro ao logar shutdown no MongoDB: {ex.Message}");
            }
        }
        
        subscriber?.Dispose();
        Console.WriteLine($"[{serverId}] RabbitMQ resources cleaned up.");
    }    static async void OnDataReceived(string message)
    {
        try
        {
            var aggregatedData = JsonSerializer.Deserialize<AggregatedData>(message);
            if (aggregatedData == null) return;            Console.WriteLine($"[{serverId}] Dados recebidos de [{aggregatedData.AgregadorId}] - {aggregatedData.Messages.Count} mensagens");

            // Save to MongoDB if available
            if (mongoService != null)
            {
                try
                {
                    await mongoService.InsertAggregatedDataAsync(aggregatedData);
                    
                    // Also save individual wavy messages
                    foreach (var wavyMessage in aggregatedData.Messages)
                    {
                        await mongoService.InsertWavyMessageAsync(wavyMessage);
                    }
                    
                    Console.WriteLine($"[{serverId}] Dados salvos no MongoDB com sucesso");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{serverId}] Erro ao salvar no MongoDB: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"[{serverId}] MongoDB não disponível - dados não foram salvos");
            }        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{serverId}] Erro ao processar dados recebidos: {ex.Message}");
        }
    }

    static void OnShutdownReceived(string message)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(message);
            var aggregatorId = jsonDoc.RootElement.GetProperty("AggregatorId").GetString();
            var timestamp = jsonDoc.RootElement.GetProperty("Timestamp").GetString();
            
            Console.WriteLine($"[{serverId}] Notificação de shutdown recebida de {aggregatorId} em {timestamp}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{serverId}] Erro ao processar shutdown: {ex.Message}");
        }
    }

    static void MonitorarComandoDesligar()
    {
        while (!encerrarExecucao)
        {
            var comando = Console.ReadLine();
            if (comando != null && comando.Trim().Equals("DLG", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[{serverId}] Comando de desligamento recebido...");
                encerrarExecucao = true;
                break;
            }
        }
    }

    // Verifica se o servidor é configurado
    static async Task<bool> IsServerConfiguredAsync(string serverId)
    {
        try
        {
            if (configService == null) return false;
            
            var config = await configService.GetServerConfigAsync(serverId);
            return config != null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao verificar configuração do servidor: {ex.Message}");
            return false;
        }
    }
}