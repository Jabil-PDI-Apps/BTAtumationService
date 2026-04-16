using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BTAutomation.Service
{
    public class JakaService : IDisposable
    {
        private readonly string ip = "192.168.100.60";
        private readonly int port = 10001;

        private TcpClient? _client;
        private NetworkStream? _stream;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly ILogger<JakaService> _logger;
        private readonly Random _random = new();

        public JakaService(ILogger<JakaService> logger)
        {
            _logger = logger;
        }

        public async Task<string> Send_PASS_BT1(CancellationToken ct = default)
        {
            var payload = new
            {
                cmdName = "set_digital_output",
                type = 4,
                index = 15,
                value = 1
            };
            // Removido o Burst de 3 envios para evitar flood no controlador
            return await SendJakaCommandAsync(payload, ct);
        }

        public async Task<string> Send_Fail_BT1(CancellationToken ct = default)
        {
            var payload = new
            {
                cmdName = "set_digital_output",
                type = 4,
                index = 16,
                value = 1
            };
            return await SendJakaCommandAsync(payload, ct);
        }

        public async Task<string> SendJakaCommandAsync(object payload, CancellationToken ct = default)
        {
            // Jitter: evita que múltiplas instâncias no Switch 2 batam no robô ao mesmo tempo
            await Task.Delay(_random.Next(10, 100), ct);

            await _lock.WaitAsync(ct);
            try
            {
                await EnsureConnectedAsync(ct);

                var json = JsonSerializer.Serialize(payload) + "\n";
                var bytes = Encoding.UTF8.GetBytes(json);

                await _stream!.WriteAsync(bytes, ct);
                await _stream.FlushAsync(ct);

                _logger.LogInformation($"Comando enviado: {json.Trim()}");

                // Aguarda a resposta do robô (obrigatório para limpar o buffer do Jaka)
                var result = await ReadResponseAsync(ct);

                _logger.LogInformation($"Resposta do Jaka: {result}");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha na comunicação com o robô Jaka.");
                return $"ERROR: {ex.Message}";
            }
            finally
            {
                // Sempre fecha a conexão para libertar o socket no controlador
                CloseConnection();
                _lock.Release();
            }
        }

        private async Task EnsureConnectedAsync(CancellationToken ct)
        {
            if (_client == null || !_client.Connected)
            {
                _client = new TcpClient();

                var connectTask = _client.ConnectAsync(ip, port, ct).AsTask();
                if (await Task.WhenAny(connectTask, Task.Delay(3000, ct)) == connectTask)
                {
                    await connectTask; 
                    _stream = _client.GetStream();
                }
                else
                {
                    _client.Dispose();
                    _client = null;
                    throw new TimeoutException("Não foi possível conectar ao robô Jaka no tempo limite de 3 segundos.");
                }
            }
        }

        private async Task<string> ReadResponseAsync(CancellationToken ct)
        {
            if (_stream == null) return "NO_STREAM";

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            try
            {
                // O StreamReader com ReadLineAsync é a forma mais segura de ler até o '\n' do Jaka
                using var reader = new StreamReader(_stream, Encoding.UTF8, leaveOpen: true);
                string? response = await reader.ReadLineAsync();

                return response ?? "EMPTY_RESPONSE";
            }
            catch (OperationCanceledException)
            {
                return "TIMEOUT_READING_RESPONSE";
            }
        }

        private void CloseConnection()
        {
            try
            {
                _stream?.Close();
                _stream?.Dispose();
                _client?.Close();
                _client?.Dispose();
            }
            finally
            {
                _stream = null;
                _client = null;
            }
        }

        public void Dispose()
        {
            CloseConnection();
            _lock.Dispose();
        }
    }
}


//using System.Net.Sockets;
//using System.Text;
//using System.Text.Json;
//namespace BTAutomation.Service
//{
//    public class JakaService 
//    {
//        //private string ip = "10.56.37.91";
//        private string ip = "192.168.100.60";
//        private int port = 10001;

//        public async Task<string> Send_PASS_BT1(CancellationToken ct = default)
//        {
//            var payload = new
//            {
//                cmdName = "set_digital_output",
//                type = 4,
//                index = 15,
//                value = 1
//            };

//            var resp = await SendJakaCommandAsync(payload);
//            await Task.Delay(40);
//            resp = await SendJakaCommandAsync(payload);
//            await Task.Delay(40);
//            resp = await SendJakaCommandAsync(payload);

//            return resp;
//        }
//        public async Task<string> Send_Fail_BT1(CancellationToken ct = default)
//        {
//            var payload = new
//            {
//                cmdName = "set_digital_output",
//                type = 4,
//                index = 16,
//                value = 1
//            };

//            var resp = await SendJakaCommandAsync(payload);
//            await Task.Delay(40, ct);
//            resp = await SendJakaCommandAsync(payload);
//            await Task.Delay(40, ct);
//            resp = await SendJakaCommandAsync(payload);

//            return resp;
//        }

//        public async Task<string> SendJakaCommandAsync(object payload, CancellationToken ct = default)
//        {
//            using var client = new TcpClient();
//            await client.ConnectAsync(ip, port, ct);

//            await using var stream = client.GetStream();

//            var json = JsonSerializer.Serialize(payload);
//            var bytes = Encoding.UTF8.GetBytes(json + "\n");

//            await stream.WriteAsync(bytes, ct);
//            await stream.FlushAsync(ct);

//            using var ms = new MemoryStream();
//            var buffer = new byte[1024];

//            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
//            cts.CancelAfter(TimeSpan.FromSeconds(2));


//            try
//            {
//                while (true)
//                {
//                    var n = await stream.ReadAsync(buffer, cts.Token);
//                    if (n == 0) break;

//                    ms.Write(buffer, 0, n);

//                    if (buffer.Take(n).Contains((byte)'\n'))
//                        break;
//                }
//            }
//            catch (OperationCanceledException)
//            {
//            }

//            var resp = Encoding.UTF8.GetString(ms.ToArray());
//            return resp.TrimEnd('\r', '\n');

//        }
//    }
//}
