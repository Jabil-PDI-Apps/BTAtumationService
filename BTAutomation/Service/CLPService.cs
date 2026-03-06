using System.Net.Sockets;
using NModbus;

namespace BTAutomation.Service
{
    public class CLPService(ILogger<CLPService> logger) : BackgroundService
    {
        private IModbusMaster? master;
        private TcpClient? _tcpClient;
        private readonly ILogger<CLPService> _logger = logger;

        //string ipAddress = "192.168.100.20";
        //private ushort registerAddress = 8252; //M60
        private string ipAddress = "127.0.0.1";
        private ushort registerAddress = 0;
        private int port = 502;
        private byte slaveId = 1;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await StartConnectionMaster(stoppingToken);
        }

        private async Task StartConnectionMaster(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Tentando conectar ao CLP Master em {IP}:{Port}...", ipAddress, port);

                    _tcpClient = new TcpClient();
                    
                    // Conecta de forma assíncrona
                    var connectTask = _tcpClient.ConnectAsync(ipAddress, port);

                    // Aguarda a conexão ou cancelamento/timeout
                    await Task.WhenAny(connectTask, Task.Delay(5000, stoppingToken));

                    if (_tcpClient.Connected)
                    {
                        _logger.LogInformation("Conectado ao CLP com sucesso!");

                        var factory = new ModbusFactory();
                        master = factory.CreateMaster(_tcpClient);

                        // Configura Timeouts para não travar o serviço em caso de perda de pacote
                        master.Transport.ReadTimeout = 2000;
                        master.Transport.WriteTimeout = 2000;
                        break;
                    }
                    else
                    {
                        _logger.LogWarning("Não foi possível conectar. Verifique se o CLP está online.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Falha crítica no Worker: {Message}", ex.Message);
                }

                _logger.LogInformation("Aguardando 5 segundos para tentar reconectar...");
                await Task.Delay(5000, stoppingToken);
            }
        }

        public void WriteToCLP(int value)
        {
            if (master == null)
            {
                _logger.LogWarning("Master não inicializado. Não é possível escrever no CLP.");
                return;
            }
            try
            {
                master.WriteSingleCoil(slaveId, registerAddress, value != 0);
                _logger.LogInformation("Valor {Value} escrito com sucesso no endereço {Address} do CLP.", value, registerAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao escrever no CLP: {Message}", ex.Message);
            }
        }

    }
}