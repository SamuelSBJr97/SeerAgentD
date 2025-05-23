using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SeerD.Services
{
    /// <summary>
    /// Gerencia múltiplos processos definidos na configuração.
    /// </summary>
    public class ProcessManager
    {
        private readonly ILogger<ProcessManager> _logger;
        private readonly List<ManagedProcess> _processes = new();

        public ProcessManager(ILogger<ProcessManager> logger)
        {
            _logger = logger;
            LoadConfig();
        }

        // Construtor para modo console sem logger
        public ProcessManager() : this(new ConsoleLogger<ProcessManager>()) { }

        private void LoadConfig()
        {
            var configPath = Path.Combine(AppContext.BaseDirectory, "apps-config.json");
            if (!File.Exists(configPath))
            {
                _logger.LogError("Arquivo de configuração apps-config.json não encontrado.");
                return;
            }

            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<AppsConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (config?.Apps != null)
            {
                foreach (var app in config.Apps)
                {
                    _processes.Add(new ManagedProcess(app, _logger));
                }
            }
        }

        public async Task StartAllAsync(CancellationToken stoppingToken)
        {
            foreach (var proc in _processes)
            {
                await proc.StartAsync(stoppingToken);
            }
        }

        public async Task StopAllAsync()
        {
            foreach (var proc in _processes)
            {
                await proc.StopAsync();
            }
        }

        /// <summary>
        /// Envia comando para um processo específico pelo nome.
        /// </summary>
        public async Task SendCommandAsync(string appName, string command)
        {
            var proc = _processes.Find(p => p.ToString() == appName);
            if (proc != null)
                await proc.SendCommandAsync(command);
        }

        public List<ManagedProcess> GetManagedApps() => _processes;
    }
}