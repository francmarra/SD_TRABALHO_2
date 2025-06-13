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
    }    static async Task EnviarDadosPeriodicamente()
    {
        var rnd = new Random();
        int segundos = 0;

        while (!encerrarExecucao && rpcClient != null)
        {            
            try
            {
                // Generate comprehensive ocean sensor data
                var wavyMessage = GenerateOceanSensorData(rnd);
                
                // Set metadata fields
                wavyMessage.WavyId = wavyID;
                wavyMessage.Continent = continentName;
                wavyMessage.ContinentCode = continentCode;
                wavyMessage.AggregatorId = aggregatorId;
                wavyMessage.ServerId = serverId;
                wavyMessage.Timestamp = DateTime.UtcNow.ToString("o");

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
        }    }

    /// <summary>
    /// Generates comprehensive ocean sensor data simulating real-world marine monitoring buoy readings.
    /// This method creates realistic data for 12 different types of ocean sensors commonly used in
    /// oceanographic research and marine monitoring systems.
    /// </summary>
    /// <param name="rnd">Random number generator for data variation</param>
    /// <returns>WavyMessage containing comprehensive ocean sensor data</returns>
    static WavyMessage GenerateOceanSensorData(Random rnd)
    {
        var message = new WavyMessage();
        
        // 1. Sea Surface Temperature (SST) - Realistic oceanic range
        // Tropical: 26-30°C, Temperate: 15-25°C, Polar: -2-10°C
        message.SeaSurfaceTemperatureCelsius = Math.Round(GetRandomInRange(rnd, -2.0, 30.0), 2);
        
        // 2. Wind Speed and Direction
        // Surface winds typically 0-40 m/s (0-144 km/h)
        message.WindSpeedMs = Math.Round(GetRandomInRange(rnd, 0.0, 40.0), 1);
        message.WindDirectionDegrees = Math.Round(GetRandomInRange(rnd, 0.0, 360.0), 0);
        
        // 3. Sea Level / Tide Height
        // Typical tidal range: -2m to +2m relative to mean sea level
        message.SeaLevelMeters = Math.Round(GetRandomInRange(rnd, -2.0, 2.0), 3);
        
        // 4. Ocean Surface Currents
        // Surface currents typically 0-2 m/s
        message.CurrentSpeedMs = Math.Round(GetRandomInRange(rnd, 0.0, 2.0), 2);
        message.CurrentDirectionDegrees = Math.Round(GetRandomInRange(rnd, 0.0, 360.0), 0);
        
        // 5. Salinity
        // Ocean salinity typically 32-37 PSU, with 35 PSU being average
        message.SalinityPsu = Math.Round(GetRandomInRange(rnd, 32.0, 37.0), 1);
        
        // 6. Chlorophyll Concentration
        // Open ocean: 0.1-1 mg/m³, Coastal/upwelling: 1-10 mg/m³
        message.ChlorophyllMgM3 = Math.Round(GetRandomInRange(rnd, 0.1, 10.0), 2);
        
        // 7. Wave Height and Direction
        // Significant wave height typically 0-15m in extreme conditions
        message.WaveHeightMeters = Math.Round(GetRandomInRange(rnd, 0.1, 8.0), 2);
        message.WaveDirectionDegrees = Math.Round(GetRandomInRange(rnd, 0.0, 360.0), 0);
        
        // 8. Acoustic Activity
        // Underwater sound levels: 50-180 dB re 1 μPa
        message.AcousticLevelDb = Math.Round(GetRandomInRange(rnd, 50.0, 180.0), 1);
        
        // 9. Turbidity / Water Clarity
        // Clear ocean: 0.1-1 NTU, Coastal/turbid: 1-100+ NTU
        message.TurbidityNtu = Math.Round(GetRandomInRange(rnd, 0.1, 50.0), 1);
        
        // 10. Rainfall / Precipitation Rate
        // 0-100 mm/h (extreme rainfall can exceed this)
        message.PrecipitationRateMmH = Math.Round(GetRandomInRange(rnd, 0.0, 25.0), 1);
        
        // 11. Pressure at Sea Surface
        // Sea level pressure: 980-1040 hPa typically
        message.SurfacePressureHpa = Math.Round(GetRandomInRange(rnd, 980.0, 1040.0), 1);
          // 12. Temperature Gradient (horizontal surface)
        // Oceanic fronts can have gradients 0.1-5°C/km
        message.TemperatureGradientCKm = Math.Round(GetRandomInRange(rnd, 0.0, 5.0), 2);
        
        return message;
    }
    
    static double GetRandomInRange(Random rnd, double min, double max)
    {
        return min + (rnd.NextDouble() * (max - min));
    }
    
    // Removed local helper methods: IsWavyConfiguredAsync, GetWavyStatusAsync, UpdateWavyStatusAsync, UpdateWavyLastSyncAsync.
    // Configuration and status are now handled via ConfigService and direct MongoDB interactions.
}