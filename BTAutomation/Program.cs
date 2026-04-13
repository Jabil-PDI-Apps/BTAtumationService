using BTAutomation.Service;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Iniciando o serviço de automação...");

    IHost host = Host.CreateDefaultBuilder(args)
        .UseWindowsService()
        .UseSerilog()
        .ConfigureServices(services =>
        {
            services.AddSingleton<JakaService>();  
            services.AddHostedService<FileWatcherService>(); 
        })
        .Build();


    var jakaService = host.Services.GetRequiredService<JakaService>();

    Log.Information("Estabelecendo conexão persistente com o robô Jaka...");
    await jakaService.ConnectAsync();

    Log.Information("✅ Conexão com o Jaka estabelecida com sucesso!");

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