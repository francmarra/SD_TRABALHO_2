using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Shared.Models;
using Shared.RabbitMQ;
using Shared.MongoDB;

class Program
{
    static RabbitMQRpcClient? rpcClient;
    static ConfigService? configService;    
    static string wavyID = "";
    static string continentCode = ""; // Loaded from MongoDB config
    static string continentName = ""; // Loaded from MongoDB config
    static string aggregatorId = "";  // Loaded from MongoDB config
    static string serverId = "";      // Loaded from MongoDB config
    static string rpcQueueName = "";  // Determined from MongoDB config
    static volatile bool encerrarExecucao = false;    

    static async Task Main(string[] args)
    {
        // Initialize configuration service
        configService = new ConfigService();

        // Check if Wavy ID was provided as command line argument
        if (args.Length > 0)
        {
            wavyID = args[0].Trim();
        }

        ConfigWavy? wavyConfig = null; 

        // Loop até que um ID válido seja fornecido
        while (true)
        {
            // If no command line argument provided, ask for input
            if (string.IsNullOrEmpty(wavyID))
            {
                Console.Write("ID da Wavy: ");
                wavyID = Console.ReadLine()?.Trim() ?? "";
            }

            if (string.IsNullOrEmpty(wavyID))
            {
                Console.WriteLine("ID inválido. O ID não pode ser vazio.");
                wavyID = ""; // Reset to ask again
                continue;
            }

            // Load wavy configuration from MongoDB
            wavyConfig = await configService.GetWavyConfigAsync(wavyID);
            if (wavyConfig == null)
            {
                Console.WriteLine($"Wavy {wavyID} não encontrada na configuração do MongoDB ou ID inválido.");
                Console.WriteLine("💡 Verifique o ID e se o ConfigImporter foi executado.");
                wavyID = ""; // Reset to ask again
                continue;
            }
            
            // Assign loaded configuration to static fields
            continentCode = wavyConfig.ContinentCode;
            continentName = wavyConfig.Continent;
            aggregatorId = wavyConfig.AggregatorId;
            serverId = wavyConfig.ServerId;
            Console.WriteLine($"[{wavyID}] Configuração carregada do MongoDB: {continentName} ({continentCode}), Aggregator: {aggregatorId}, Server: {serverId}");

            // Status check and update
            if (wavyConfig.Status == 0) 
            {
                Console.Write($"{wavyID} Offline! Deseja voltar a ligá-la? (y/n) [y]: ");
                string input = (Console.ReadLine() ?? "").Trim().ToLower();
                if (string.IsNullOrEmpty(input) || input == "y")
                {
                    // Update status in MongoDB
                    await configService.UpdateWavyStatusAsync(wavyID, 1, DateTime.UtcNow);
                    Console.WriteLine($"{wavyID} foi atualizado para Online no MongoDB.");
                    // wavyConfig.Status = 1; // The local object is not used beyond this point for status
                }
                else
                {
                    wavyID = ""; 
                    continue; 
                }
            }
            break; 
        }
        
        // Determine RPC queue name based on the Wavy's continent code from loaded config
        rpcQueueName = RabbitMQConfig.RPC_QUEUE_PREFIX + continentCode; 

        Console.WriteLine($"[{wavyID}] Inicializando RabbitMQ RPC Client...");

        try
        {
            rpcClient = new RabbitMQRpcClient();
            Console.WriteLine($"[{wavyID}] RabbitMQ configurado com sucesso.");
            Console.WriteLine($"[{wavyID}] RPC Queue: {rpcQueueName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{wavyID}] Erro ao configurar RabbitMQ: {ex.Message}");
            return;
        }

        // Estabelece handshake com o Agregador
        bool conectado = await EstabelecerHandshake();
        if (!conectado)
        {
            Console.WriteLine($"[{wavyID}] Falha ao estabelecer conexão com o Agregador.");
            return;
        }

        Console.WriteLine($"[{wavyID}] Conexão estabelecida com o Agregador via RabbitMQ RPC.");

        // Inicia o envio de dados
        var dataTask = Task.Run(async () => await EnviarDadosPeriodicamente());

        // Monitorar comando de desligamento
        var shutdownTask = Task.Run(async () => await MonitorarComandoDesligar());

        // Aguarda até que uma das tarefas complete
        await Task.WhenAny(dataTask, shutdownTask);

        Console.WriteLine($"[{wavyID}] Encerrando execução...");
        rpcClient?.Dispose();
        Console.WriteLine($"[{wavyID}] RabbitMQ resources cleaned up.");
    }

    static async Task<bool> EstabelecerHandshake()
    {
        if (rpcClient == null) return false;

        try
        {
            var request = new RpcRequest
            {
                Type = "HANDSHAKE",
                WavyId = wavyID
            };

            var response = await rpcClient.CallAsync(rpcQueueName, request, TimeSpan.FromSeconds(10));
            
            if (response.Status == "OK")
            {
                Console.WriteLine($"[{wavyID}] Handshake estabelecido: {response.Message}");
                return true;
            }
            else
            {
                Console.WriteLine($"[{wavyID}] Falha no handshake: {response.Message}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{wavyID}] Erro durante handshake: {ex.Message}");
            return false;
        }
    }

    static async Task EnviarDadosPeriodicamente()
    {
        var rnd = new Random();
        int segundos = 0;

        while (!encerrarExecucao && rpcClient != null)
        {            
            try
            {
                double temperatura = Math.Round(15 + rnd.NextDouble() * 10, 2);
                
                WavyMessage wavyMessage;
                if (segundos % 2 == 0)
                {
                    double umidade = rnd.Next(0, 100);
                    wavyMessage = new WavyMessage
                    {
                        WavyId = wavyID,
                        Continent = continentName,       // Uses static field loaded in Main
                        ContinentCode = continentCode,   // Uses static field loaded in Main
                        AggregatorId = aggregatorId,     // Uses static field loaded in Main
                        ServerId = serverId,             // Uses static field loaded in Main
                        Temperature = temperatura,
                        Humidity = umidade,
                        Co2 = 0, 
                        Timestamp = DateTime.Now.ToString("o")
                    };
                }
                else
                {
                    wavyMessage = new WavyMessage
                    {
                        WavyId = wavyID,
                        Continent = continentName,       // Uses static field loaded in Main
                        ContinentCode = continentCode,   // Uses static field loaded in Main
                        AggregatorId = aggregatorId,     // Uses static field loaded in Main
                        ServerId = serverId,             // Uses static field loaded in Main
                        Temperature = temperatura,
                        Humidity = 0, 
                        Co2 = 0, 
                        Timestamp = DateTime.Now.ToString("o")
                    };
                }

                var request = new RpcRequest
                {
                    Type = "DATA",
                    WavyId = wavyID,
                    Data = JsonSerializer.Serialize(wavyMessage)
                };

                var response = await rpcClient.CallAsync(rpcQueueName, request, TimeSpan.FromSeconds(10));
                if (response.Status == "OK")
                {
                    Console.WriteLine($"[{wavyID}] Dados enviados com sucesso: {response.Message}");
                    // Update LastSync and ensure status is 1 (Online)
                    await configService.UpdateWavyStatusAsync(wavyID, 1, DateTime.UtcNow);
                }
                else
                {
                    Console.WriteLine($"[{wavyID}] Erro ao enviar dados: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{wavyID}] Erro ao enviar dados: {ex.Message}");
            }

            await Task.Delay(1000);
            segundos++;
        }
    }

    static async Task MonitorarComandoDesligar()
    {
        while (!encerrarExecucao)
        {
            var comando = Console.ReadLine();
            if (comando != null && comando.Trim().Equals("DLG", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[{wavyID}] Terminando execução. Enviando pedido de desligamento...");
                
                if (rpcClient != null)
                {
                    try
                    {
                        var request = new RpcRequest
                        {
                            Type = "SHUTDOWN",
                            WavyId = wavyID
                        };

                        var response = await rpcClient.CallAsync(rpcQueueName, request, TimeSpan.FromSeconds(10));
                        Console.WriteLine($"[{wavyID}] Resposta do Agregador: {response.Message}");
                        // Update Wavy status to Offline (0) in MongoDB upon clean shutdown
                        await configService.UpdateWavyStatusAsync(wavyID, 0, DateTime.UtcNow);
                        Console.WriteLine($"[{wavyID}] Status atualizado para Offline no MongoDB.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{wavyID}] Erro ao enviar desligamento: {ex.Message}");
                    }
                }

                encerrarExecucao = true;
                break;
            }
        }
    }
    // Removed local helper methods: IsWavyConfiguredAsync, GetWavyStatusAsync, UpdateWavyStatusAsync, UpdateWavyLastSyncAsync.
    // Configuration and status are now handled via ConfigService and direct MongoDB interactions.
}