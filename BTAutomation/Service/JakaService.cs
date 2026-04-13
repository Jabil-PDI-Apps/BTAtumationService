using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Serilog;

namespace BTAutomation.Service
{
    public class JakaService : IAsyncDisposable
    {
        private readonly string _ip = "192.168.100.60";
        private readonly int _port = 10001;

        private TcpClient? _client;
        private NetworkStream? _stream;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        private bool IsConnected => _client?.Connected == true && _stream?.CanWrite == true && _stream?.CanRead == true;

        public async Task ConnectAsync(CancellationToken ct = default)
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                await CloseConnectionAsync(); 

                _client = new TcpClient();

                _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                //_client.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 30);  
                //_client.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 10);

                Log.Information("Conectando ao robô Jaka em {Ip}:{Port}...", _ip, _port);

                await _client.ConnectAsync(_ip, _port, ct);
                _stream = _client.GetStream();

                Log.Information("✅ Conexão PERSISTENTE estabelecida com o Jaka!");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Falha ao conectar ao Jaka");
                await CloseConnectionAsync();
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task CloseConnectionAsync()
        {
            try
            {
                if (_stream != null)
                {
                    await _stream.DisposeAsync();
                    _stream = null;
                }
                _client?.Dispose();
                _client = null;
            }
            catch {  }
        }

        private async Task<string> SendJakaCommandAsync(object payload, CancellationToken ct = default)
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                if (!IsConnected)
                {
                    Log.Warning("Conexão com Jaka perdida. Reconectando...");
                    await ConnectAsync(ct);
                }

                var json = JsonSerializer.Serialize(payload);
                var bytes = Encoding.UTF8.GetBytes(json + "\n");

                await _stream!.WriteAsync(bytes, ct);
                await _stream.FlushAsync(ct);

                using var ms = new MemoryStream();
                var buffer = new byte[1024];

                using var readCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                readCts.CancelAfter(TimeSpan.FromSeconds(3)); 

                try
                {
                    while (true)
                    {
                        int n = await _stream.ReadAsync(buffer, readCts.Token);
                        if (n == 0) break;

                        ms.Write(buffer, 0, n);

                        if (buffer.AsSpan(0, n).Contains((byte)'\n'))
                            break;
                    }
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    Log.Warning("Timeout ao aguardar resposta do Jaka");
                }

                var response = Encoding.UTF8.GetString(ms.ToArray()).TrimEnd('\r', '\n', ' ');

                if (string.IsNullOrWhiteSpace(response))
                    response = "{ \"error\": \"no response\" }";

                Log.Debug("Resposta Jaka: {Response}", response);
                return response;
            }
            catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException)
            {
                Log.Warning(ex, "Erro de comunicação com Jaka. A conexão será fechada para reconexão.");
                await CloseConnectionAsync();
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<string> Send_PASS_BT1(CancellationToken ct = default)
        {
            var payload = new { cmdName = "set_digital_output", type = 4, index = 1, value = 1 };

            return await SendWithRetry(payload, "Send_PASS_BT1", "DO 1 = 1", ct);
        }

        public async Task<string> Send_Fail_BT1(CancellationToken ct = default)
        {
            var payload = new { cmdName = "set_digital_output", type = 4, index = 2, value = 1 };

            return await SendWithRetry(payload, "Send_Fail_BT1", "DO 2 = 1", ct);
        }

        private async Task<string> SendWithRetry(object payload, string methodName, string description, CancellationToken ct)
        {
            string lastResponse = string.Empty;

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                Log.Information("[{Method}] Tentativa {Attempt}/3 - Enviando {Desc}", methodName, attempt, description);

                try
                {
                    lastResponse = await SendJakaCommandAsync(payload, ct);
                    Log.Information("[{Method}] Resposta recebida: {Response}", methodName, lastResponse);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[{Method}] Falha na tentativa {Attempt}", methodName, attempt);
                    if (attempt == 3) throw;
                }

                if (attempt < 3)
                    await Task.Delay(300, ct); 
            }

            return lastResponse;
        }

        public async ValueTask DisposeAsync()
        {
            _semaphore.Dispose();
            await CloseConnectionAsync();
            Log.Information("Conexão com Jaka fechada.");
        }
    }
}