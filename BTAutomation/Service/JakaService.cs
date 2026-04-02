using System.Net.Sockets;
using System.Text;
using System.Text.Json;
namespace BTAutomation.Service
{
    public class JakaService 
    {
        //private string ip = "10.56.37.91";
        private string ip = "192.168.100.60";
        private int port = 10001;

        public async Task<string> Send_PASS_BT1(CancellationToken ct = default)
        {
            var payload = new
            {
                cmdName = "set_digital_output",
                type = 4,
                index = 15,
                value = 1
            };

            var resp = await SendJakaCommandAsync(payload);
            await Task.Delay(40);
            resp = await SendJakaCommandAsync(payload);
            await Task.Delay(40);
            resp = await SendJakaCommandAsync(payload);

            return resp;
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

            var resp = await SendJakaCommandAsync(payload);
            await Task.Delay(40);
            resp = await SendJakaCommandAsync(payload);
            await Task.Delay(40);
            resp = await SendJakaCommandAsync(payload);

            return resp;
        }

        public async Task<string> SendJakaCommandAsync(object payload, CancellationToken ct = default)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(ip, port, ct);

            await using var stream = client.GetStream();

            var json = JsonSerializer.Serialize(payload);
            var bytes = Encoding.UTF8.GetBytes(json + "\n");

            await stream.WriteAsync(bytes, ct);
            await stream.FlushAsync(ct);

            using var ms = new MemoryStream();
            var buffer = new byte[1024];

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(2));


            try
            {
                while (true)
                {
                    var n = await stream.ReadAsync(buffer, cts.Token);
                    if (n == 0) break;

                    ms.Write(buffer, 0, n);

                    if (buffer.Take(n).Contains((byte)'\n'))
                        break;
                }
            }
            catch (OperationCanceledException)
            {
            }

            var resp = Encoding.UTF8.GetString(ms.ToArray());
            return resp.TrimEnd('\r', '\n');

        }
    }
}
