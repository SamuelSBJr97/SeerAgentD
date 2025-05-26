using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SeerAgentD.Core.Interfaces;
using SeerAgentD.Core.Models;

namespace SeerAgentD.Console.Services
{
    /// <summary>
    /// Worker Service principal que gerencia o ciclo de vida dos aplicativos monitorados.
    /// </summary>
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IProcessMonitor _processMonitor;
        private readonly string _configPath;

        public Worker(ILogger<Worker> logger, IProcessMonitor processMonitor)
        {
            _logger = logger;
            _processMonitor = processMonitor;
            _configPath = Path.Combine(AppContext.BaseDirectory, "apps-config.json");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker service starting at: {time}", DateTimeOffset.Now);

            try
            {
                var config = await LoadConfigurationAsync();
                foreach (var app in config.Apps)
                {
                    await _processMonitor.StartMonitoringAsync(
                        app.Name,
                        app.ExecutablePath,
                        app.Arguments,
                        app.WorkingDirectory,
                        stoppingToken
                    );
                }

                while (!stoppingToken.IsCancellationRequested)
                {
                    var processes = await _processMonitor.GetAllProcessesInfoAsync(stoppingToken);
                    foreach (var process in processes)
                    {
                        _logger.LogInformation(
                            "Process {name} (ID: {id}) - Status: {status}, CPU: {cpu}ms, Memory: {memory}MB",
                            process.Name,
                            process.ProcessId,
                            process.Status,
                            process.CpuUsage,
                            process.MemoryUsage / 1024 / 1024
                        );
                    }

                    await Task.Delay(5000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while running the worker service");
                throw;
            }
        }

        private async Task<AppConfiguration> LoadConfigurationAsync()
        {
            if (!File.Exists(_configPath))
            {
                throw new FileNotFoundException("Configuration file not found", _configPath);
            }

            var jsonString = await File.ReadAllTextAsync(_configPath);
            var config = JsonSerializer.Deserialize<AppConfiguration>(jsonString);

            if (config == null || config.Apps == null || !config.Apps.Any())
            {
                throw new InvalidOperationException("Invalid or empty configuration");
            }

            return config;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker service stopping at: {time}", DateTimeOffset.Now);

            var processes = await _processMonitor.GetAllProcessesInfoAsync(cancellationToken);
            foreach (var process in processes)
            {
                await _processMonitor.StopMonitoringAsync(process.Name, cancellationToken);
            }

            await base.StopAsync(cancellationToken);
        }
    }

    public class AppConfiguration
    {
        public List<AppInfo> Apps { get; set; } = new();
    }

    public class AppInfo
    {
        public string Name { get; set; } = "";
        public string ExecutablePath { get; set; } = "";
        public string Arguments { get; set; } = "";
        public string WorkingDirectory { get; set; } = "";
    }
}
