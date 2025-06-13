using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;
using Shared.Models;
using Shared.RabbitMQ;
using Shared.MongoDB;

class Program
{
    static readonly ConcurrentQueue<WavyMessage> dataQueue = new();
    static RabbitMQPublisher? publisher;
    static ConfigService? configService;
    static volatile bool encerrarExecucao = false;

    static string aggregatorID = "";
    static string continentCode = "";
    static string continentName = "";
    static string serverId = "";
    static string rpcQueueName = "";
    static ConfigAgr? aggregatorConfig;

    static async Task Main(string[] args)
    {
        // Support command line argument for aggregator ID
        if (args.Length > 0)
        {
            aggregatorID = args[0].Trim();
        }
        else
        {
            Console.Write("🌍 Aggregator ID (e.g., EU-Agr01, NA-Agr01): ");
            aggregatorID = Console.ReadLine()?.Trim() ?? "";
        }

        if (string.IsNullOrEmpty(aggregatorID))
        {
            Console.WriteLine("❌ ID cannot be empty.");
            return;
        }

        // Initialize configuration service
        configService = new ConfigService(); // Moved up

        // Validate aggregator ID format (continent-based)
        // and load configuration from MongoDB
        aggregatorConfig = await configService.GetAgrConfigAsync(aggregatorID);

        if (aggregatorConfig == null)
        {
            Console.WriteLine($"❌ Aggregator {aggregatorID} not found in MongoDB configuration or invalid format.");
            Console.WriteLine("💡 Ensure the aggregator ID is correct and ConfigImporter has been run.");
            return;
        }

        // Extract continent code from aggregator ID
        continentCode = aggregatorConfig.ContinentCode;
        continentName = aggregatorConfig.Continent; // Use continent name from config

        Console.WriteLine($"🚀 Starting {aggregatorID} for {continentName} ({continentCode})...");

        // Configuration is already loaded, assign values
        serverId = aggregatorConfig.ServerId;
        rpcQueueName = aggregatorConfig.QueueName;

        Console.WriteLine($"📡 Configuration loaded from MongoDB:");
        Console.WriteLine($"   • Continent: {aggregatorConfig.Continent} ({aggregatorConfig.ContinentCode})");
        Console.WriteLine($"   • Server: {aggregatorConfig.ServerId}");
        Console.WriteLine($"   • Port: {aggregatorConfig.Port}");
        Console.WriteLine($"   • Queue: {aggregatorConfig.QueueName}");        Console.WriteLine($"🔧 Initializing RabbitMQ components...");

        try
        {
            // Initialize RabbitMQ Publisher for sending data to Server
            publisher = new RabbitMQPublisher();
            Console.WriteLine($"✅ RabbitMQ Publisher configured successfully.");            // Subscribe to ocean data exchange instead of using RPC
            var subscriber = new RabbitMQSubscriber($"{aggregatorID}_ocean_queue");
            subscriber.SubscribeToTopic("ocean_data_exchange", "ocean.data.*", HandleWavyData);
            Console.WriteLine($"✅ Subscribed to ocean data exchange with pattern: ocean.data.*");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error configuring RabbitMQ: {ex.Message}");
            return;
        }        Console.WriteLine($"🎯 {aggregatorID} ready and waiting for Wavy data...\n");

        // Process data and send to Server every 10 seconds
        var dataTask = Task.Run(async () => await ProcessAndSendData());

        // Monitor shutdown command
        var shutdownTask = Task.Run(async () => await MonitorarComandoDesligar());

        // Wait until one of the tasks completes
        await Task.WhenAny(dataTask, shutdownTask);

        Console.WriteLine($"🔄 {aggregatorID} shutting down...");
        
        // Cleanup resources
        publisher?.Dispose();
        Console.WriteLine($"✅ {aggregatorID} RabbitMQ resources cleaned up.");
    }

    static void HandleWavyData(string routingKey, string message)
    {
        try
        {
            var wavyMessage = JsonSerializer.Deserialize<WavyMessage>(message);
            if (wavyMessage != null)
            {
                // Add to processing queue
                dataQueue.Enqueue(wavyMessage);
                
                Console.WriteLine($"📊 [{aggregatorID}] Received data from {wavyMessage.WavyId}: " +
                    $"SST={wavyMessage.SeaSurfaceTemperatureCelsius:F1}°C, " +
                    $"Lat={wavyMessage.Latitude:F4}, Lon={wavyMessage.Longitude:F4}");
            }        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [{aggregatorID}] Error processing Wavy data: {ex.Message}");
        }
    }

    static async Task ProcessAndSendData()
    {
        while (!encerrarExecucao)
        {
            await Task.Delay(5000);

            if (!dataQueue.IsEmpty)
            {
                var messages = new List<WavyMessage>();

                // Collect all pending messages
                while (dataQueue.TryDequeue(out var message))
                {
                    messages.Add(message);
                }

                if (messages.Count > 0)
                {
                    // Create aggregated data
                    var aggregatedData = new AggregatedData
                    {
                        AgregadorId = aggregatorID,
                        Messages = messages,
                        Timestamp = DateTime.Now.ToString("o")
                    };

                    // Send to server via RabbitMQ
                    SendDataToServer(aggregatedData);
                }
            }
        }
    }

    static void SendDataToServer(AggregatedData aggregatedData)
    {
        if (publisher == null)
        {
            Console.WriteLine($"[{aggregatorID}] RabbitMQ Publisher não está disponível.");
            return;
        }

        try
        {
            Console.WriteLine($"[{aggregatorID}] Enviando {aggregatedData.Messages.Count} mensagens para o Servidor via RabbitMQ...");
            publisher.PublishData(aggregatedData);
            Console.WriteLine($"[{aggregatorID}] Dados enviados com sucesso para o Servidor.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{aggregatorID}] Erro ao enviar dados para o Servidor: {ex.Message}");
        }
    }

    static async Task MonitorarComandoDesligar()
    {
        while (!encerrarExecucao)
        {
            var comando = Console.ReadLine();
            if (comando != null && comando.Trim().Equals("DLG", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[{aggregatorID}] Comando de desligamento recebido...");
                
                // Send shutdown notification to server
                if (publisher != null)
                {
                    try
                    {
                        publisher.PublishShutdown(aggregatorID);
                        Console.WriteLine($"[{aggregatorID}] Notificação de shutdown enviada ao Servidor.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{aggregatorID}] Erro ao enviar notificação de shutdown: {ex.Message}");
                    }
                }

                encerrarExecucao = true;
                break;
            }

            await Task.Delay(100);
        }
    }    static bool IsValidAggregatorId(string id)
    {
        if (string.IsNullOrEmpty(id) || !id.Contains('-'))
            return false;

        var parts = id.Split('-');
        if (parts.Length != 2)
            return false;

        var continentCode = parts[0];
        var agrPart = parts[1];

        // Validate continent code
        if (!ContinentConfig.IsValidContinentCode(continentCode))
            return false;

        // Validate aggregator part (should be Agr followed by number)
        if (!agrPart.StartsWith("Agr") || agrPart.Length < 4)
            return false;

        // Check if the number part is valid
        var numberPart = agrPart.Substring(3);
        return int.TryParse(numberPart, out _);
    }
}