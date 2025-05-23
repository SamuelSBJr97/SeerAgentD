using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SeerD.Services;

namespace SeerD
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddFile(
                        @"C:\ProgramData\SeerD\Logs\seer.log"
                    );
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<ProcessManager>();
                    services.AddHostedService<Worker>();
                })
                .Build()
                .Run();
        }
    }

    /// <summary>
    /// Worker Service principal que gerencia o ciclo de vida dos aplicativos monitorados.
    /// </summary>
    public class Worker : BackgroundService
    {
        private readonly ProcessManager _processManager;
        private readonly ILogger<Worker> _logger;

        public Worker(ProcessManager processManager, ILogger<Worker> logger)
        {
            _processManager = processManager;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SeerD Service iniciado.");
            await _processManager.StartAllAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SeerD Service parando...");
            await _processManager.StopAllAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}