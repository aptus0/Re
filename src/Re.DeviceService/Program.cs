using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/device-service-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options => options.ServiceName = "Re Device Service")
    .UseSerilog()
    .ConfigureServices((ctx, services) =>
    {
        services.AddHostedService<Re.DeviceService.Workers.PrintWorker>();
    })
    .Build();

await host.RunAsync();

