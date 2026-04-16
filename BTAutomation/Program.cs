using BTAutomation.Service;
using Serilog;
using System.ComponentModel.Design;

// Configuração do Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug() // Define o nível mínimo de log
    .WriteTo.Console()    // Mantém o log no console para você ver agora
    .WriteTo.File("Logs/log-.txt",
        rollingInterval: RollingInterval.Day, // Cria log-20231027.txt, etc.
        retainedFileCountLimit: 7)            // Mantém apenas os últimos 7 dias
    .CreateLogger();

try
{
    Log.Information("Iniciando o serviço de automação...");

    // O Host.CreateDefaultBuilder configura Logs, Injeção de Dependência 
    // e lê o seu appsettings.json automaticamente.
    IHost host = Host.CreateDefaultBuilder(args)
        .UseWindowsService()
        .UseSerilog() 
        .ConfigureServices(services =>
        {
            //services.AddSingleton<CLPService>();
            //services.AddHostedService(sp => sp.GetRequiredService<CLPService>());
            services.AddHostedService<FileWatcherService>();
            services.AddTransient<JakaService>();
        })
        .Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "O serviço parou inesperadamente!");
}
finally
{
    Log.CloseAndFlush();
}

