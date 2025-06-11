using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;
using Shared.Models;
using Shared.RabbitMQ;
using Shared.MongoDB;

class Program
{
    static readonly ConcurrentQueue<WavyMessage> dataQueue = new();
    static RabbitMQRpcServer? rpcServer;
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
        Console.WriteLine($"   • Queue: {aggregatorConfig.QueueName}");

        Console.WriteLine($"🔧 Initializing RabbitMQ components...");

        try
        {
            // Initialize RabbitMQ Publisher for sending data to Server
            publisher = new RabbitMQPublisher();
            Console.WriteLine($"✅ RabbitMQ Publisher configured successfully.");

            // Initialize RabbitMQ RPC Server for handling Wavy requests
            rpcServer = new RabbitMQRpcServer(rpcQueueName, HandleRpcRequest);
            rpcServer.Start();
            Console.WriteLine($"✅ RabbitMQ RPC Server started on queue: {rpcQueueName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error configuring RabbitMQ: {ex.Message}");
            return;
        }

        Console.WriteLine($"🎯 {aggregatorID} ready and waiting for Wavy connections...\n");

        // Process data and send to Server every 10 seconds
        var dataTask = Task.Run(async () => await ProcessAndSendData());

        // Monitor shutdown command
        var shutdownTask = Task.Run(async () => await MonitorarComandoDesligar());

        // Wait until one of the tasks completes
        await Task.WhenAny(dataTask, shutdownTask);

        Console.WriteLine($"🔄 {aggregatorID} shutting down...");
        
        // Cleanup resources
        rpcServer?.Dispose();
        publisher?.Dispose();
        Console.WriteLine($"✅ {aggregatorID} RabbitMQ resources cleaned up.");
    }

    static Task<RpcResponse> HandleRpcRequest(RpcRequest request)
    {
        try
        {
            switch (request.Type?.ToUpper())
            {
                case "HANDSHAKE":
                    return Task.FromResult(HandleHandshake(request));
                
                case "DATA":
                    return Task.FromResult(HandleDataRequest(request));
                
                case "SHUTDOWN":
                    return Task.FromResult(HandleShutdownRequest(request));
                
                default:
                    return Task.FromResult(new RpcResponse
                    {
                        Status = "ERROR",
                        Message = "Tipo de request não reconhecido"
                    });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{aggregatorID}] Erro ao processar RPC request: {ex.Message}");
            return Task.FromResult(new RpcResponse
            {
                Status = "ERROR",
                Message = ex.Message
            });
        }
    }    static RpcResponse HandleHandshake(RpcRequest request)
    {
        var wavyId = request.WavyId ?? "";
        
        // Check if Wavy ID follows continent-based format and matches aggregator's continent
        if (!IsValidWavyIdForAggregator(wavyId))
        {
            Console.WriteLine($"❌ [{aggregatorID}] Handshake rejected: {wavyId} (wrong continent or invalid format)");
            return new RpcResponse
            {
                Status = "ERROR",
                Message = $"Wavy {wavyId} is not configured for continent {continentCode} or has invalid format"
            };
        }

        Console.WriteLine($"🤝 [{aggregatorID}] Handshake accepted: {wavyId}");
        return new RpcResponse
        {
            Status = "OK",
            Message = $"Handshake successful with {aggregatorID}",
            Data = JsonSerializer.Serialize(new { 
                aggregatorId = aggregatorID, 
                continent = continentName,
                continentCode = continentCode,
                serverId = serverId 
            })
        };
    }

    static bool IsValidWavyIdForAggregator(string wavyId)
    {
        if (string.IsNullOrEmpty(wavyId) || !wavyId.Contains('-'))
            return false;

        var parts = wavyId.Split('-');
        if (parts.Length != 2)
            return false;

        var wavyContinentCode = parts[0];
        // var wavyPart = parts[1]; // wavyPart is not used, can be removed or commented

        // Check if Wavy belongs to the same continent as this aggregator
        if (wavyContinentCode != continentCode) // continentCode is now a class member
            return false;

        // Validate Wavy part format (should be Wavy followed by number)
        // This validation can be enhanced based on specific Wavy ID naming conventions
        // For now, just checking if it starts with "Wavy" and has a numeric suffix.
        // Example: EU-Wavy01
        return ContinentConfig.IsValidWavyId(wavyId); // Using existing validation
    }

    static RpcResponse HandleDataRequest(RpcRequest request)
    {
        try
        {
            var wavyId = request.WavyId ?? "";
            var data = request.Data ?? "";            if (string.IsNullOrEmpty(data))
            {
                return new RpcResponse
                {
                    Status = "ERROR",
                    Message = "Data cannot be empty"
                };
            }

            // Parse the WavyMessage from JSON
            var wavyMessage = JsonSerializer.Deserialize<WavyMessage>(data);
            if (wavyMessage == null)
            {
                return new RpcResponse
                {
                    Status = "ERROR",
                    Message = "Failed to deserialize sensor data"
                };
            }

            // Add continent and aggregator information to the message
            wavyMessage.Continent = continentName;
            wavyMessage.ContinentCode = continentCode;
            wavyMessage.AggregatorId = aggregatorID;
            wavyMessage.ServerId = serverId;
            wavyMessage.AgregadorId = aggregatorID; // Legacy support

            // Add to processing queue
            dataQueue.Enqueue(wavyMessage);

            Console.WriteLine($"📊 [{aggregatorID}] Data received from {wavyId}: T={wavyMessage.Temperature:F1}°C, H={wavyMessage.Humidity:F1}%, CO2={wavyMessage.Co2}ppm");
            
            return new RpcResponse
            {
                Status = "OK",
                Message = "Data received successfully"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{aggregatorID}] Erro ao processar dados: {ex.Message}");
            return new RpcResponse
            {
                Status = "ERROR",
                Message = $"Erro ao processar dados: {ex.Message}"
            };
        }
    }

    static RpcResponse HandleShutdownRequest(RpcRequest request)
    {
        var wavyId = request.WavyId ?? "";
        Console.WriteLine($"[{aggregatorID}] Pedido de desligamento recebido de {wavyId}");
        
        return new RpcResponse
        {
            Status = "OK",
            Message = $"Desligamento de {wavyId} reconhecido"
        };
    }    static async Task ProcessAndSendData()
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