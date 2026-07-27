using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Re.DeviceService.Workers;

/// <summary>
/// Yazdırma kuyruğunu işleyen arka plan servisi.
/// Barkod yazıcı (ZPL/TSPL) ve termal fiş yazıcı işlemlerini yönetir.
/// </summary>
public class PrintWorker : BackgroundService
{
    private readonly ILogger<PrintWorker> _logger;

    public PrintWorker(ILogger<PrintWorker> logger) => _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Re Yazıcı Servisi başlatıldı.");

        while (!stoppingToken.IsCancellationRequested)
        {
            // TODO: Yazdırma kuyruğunu dinle (RabbitMQ / named pipe / shared memory)
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        _logger.LogInformation("Re Yazıcı Servisi durduruldu.");
    }
}

