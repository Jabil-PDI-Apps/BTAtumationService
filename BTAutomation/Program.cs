using BTAutomation.Service;
using Serilog;
using System.ComponentModel.Design;




// Configura��o do Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug() // Define o n�vel m�nimo de log
    .WriteTo.Console()    // Mant�m o log no console para voc� ver agora
    .WriteTo.File("Logs/log-.txt",
        rollingInterval: RollingInterval.Day, // Cria log-20231027.txt, etc.
        retainedFileCountLimit: 7)            // Mant�m apenas os �ltimos 7 dias
    .CreateLogger();

try
{
    Log.Information("Iniciando o servi�o de automa��o...");

    // O Host.CreateDefaultBuilder configura Logs, Inje��o de Depend�ncia 
    // e l� o seu appsettings.json automaticamente.
    IHost host = Host.CreateDefaultBuilder(args)
        .UseWindowsService()
        .UseSerilog()
        .ConfigureServices(services =>
        {
            //services.AddSingleton<CLPService>();
            services.AddSingleton<JakaService>();
            //services.AddHostedService(sp => sp.GetRequiredService<CLPService>());
            services.AddHostedService<FileWatcherService>();
        })
        .Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "O servi�o parou inesperadamente!");
}
finally
{
    Log.CloseAndFlush();
}
