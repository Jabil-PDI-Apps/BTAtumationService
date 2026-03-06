
using BTAutomation.Utils;


namespace BTAutomation.Service
{
    public class FileWatcherService : BackgroundService
    {
        private readonly ILogger<FileWatcherService> _logger;
        //private readonly string _path = @"C:\DGS\LOGS";
        private readonly string _path = @"C:\Users\4134331\OneDrive - Jabil\Documents\AutomaçaoDownloader";
        private readonly Dictionary<string, DateTime> _lastProcessed = new();
        private FileSystemWatcher? _watcher;
        CLPService _clpService;

       

        public FileWatcherService(ILogger<FileWatcherService> logger, CLPService clpService)
        {
            _logger = logger;
            _clpService = clpService;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Iniciando monitoramento em tempo real em: {path}", _path);

            if (!Directory.Exists(_path))
            {
                _logger.LogError("Diretório não encontrado: {path}", _path);
                return Task.CompletedTask;
            }

            // Configuração do Watcher
            _watcher = new FileSystemWatcher(_path, "*.csv")
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                EnableRaisingEvents = true
            };

            // Eventos disparados quando algo acontece
            _watcher.Created += OnFileChanged;
            _watcher.Changed += OnFileChanged;

            return Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // Filtra apenas arquivos que começam com "SM"
            if (!Path.GetFileName(e.FullPath).StartsWith("SM")) return;

            // Debaunce
            lock (_lastProcessed)
            {
                if (_lastProcessed.TryGetValue(e.FullPath, out var lastTime))
                {
                    if (DateTime.Now - lastTime < TimeSpan.FromSeconds(1))
                    {
                        _logger.LogInformation("Arquivo modificado commenos de 1s: {file}", e.Name);
                        return;
                    }
                }
                _lastProcessed[e.FullPath] = DateTime.Now;
            }

            _logger.LogInformation("Alteração detectada no arquivo: {file}", e.Name);

            await Task.Delay(500);

            try
            {
                await Arquivo.ProcessarArquivo(e.FullPath, _logger, _clpService);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar alteração no arquivo {file}", e.Name);
            }
        }

        public override void Dispose()
        {
            _watcher?.Dispose();
            base.Dispose();
        }

    }
}

