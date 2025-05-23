using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SeerD.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SeerD
{
    internal static class Program
    {
        public static async Task Main(string[] args)
        {
            if (args.Contains("--console"))
            {
                var processManager = new ProcessManager(new ConsoleLogger<ProcessManager>());
                await processManager.StartAllAsync(default);

                await ConsoleInterface.RunAsync(processManager);

                await processManager.StopAllAsync();
            }
            else
            {
                Host.CreateDefaultBuilder(args)
                    .ConfigureLogging((context, logging) =>
                    {
                        logging.ClearProviders();
                        logging.AddConsole();
                        logging.AddFile(
                            Path.Combine(AppContext.BaseDirectory, "Logs", "seer.log")
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

    /// <summary>
    /// Interface de texto reativa para gerenciamento dos apps.
    /// </summary>
    public static class ConsoleInterface
    {
        public static async Task RunAsync(ProcessManager manager)
        {
            await ShowMenuAsync(manager);
        }

        private static async Task ShowMenuAsync(ProcessManager manager)
        {
            var apps = manager.GetManagedApps();
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== SeerD - Gerenciador de Apps ===");
                for (int i = 0; i < apps.Count; i++)
                    Console.WriteLine($"{i + 1}. {apps[i].Config.Name}");

                Console.WriteLine("0. Sair");
                Console.Write("Selecione um app para gerenciar: ");
                var input = await ReadLineAsync();
                if (!int.TryParse(input, out int idx) || idx < 0 || idx > apps.Count)
                    continue;
                if (idx == 0) break;

                var app = apps[idx - 1];
                await ShowAppMenuAsync(app);
            }
        }

        private static async Task ShowAppMenuAsync(ManagedProcess app)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine($"=== {app.Config.Name} ===");
                Console.WriteLine("1. Ver output (últimas linhas do log)");
                Console.WriteLine("2. Enviar comando");
                Console.WriteLine("3. Parar app");
                Console.WriteLine("4. Reiniciar app");
                Console.WriteLine("0. Voltar");
                Console.Write("Escolha: ");
                var op = await ReadLineAsync();
                if (op == "0") break;
                if (op == "1")
                {
                    Console.Clear();
                    var logPath = Directory.GetFiles(app.Config.WorkingDirectory, $"{app.Config.Name}_*.log")
                        .OrderByDescending(f => f)
                        .FirstOrDefault();
                    if (logPath != null)
                    {
                        var lines = File.ReadLines(logPath).Reverse().Take(40).Reverse();
                        foreach (var line in lines)
                            Console.WriteLine(line);
                    }
                    else
                    {
                        Console.WriteLine("Nenhum log encontrado.");
                    }
                    Console.WriteLine("\nPressione qualquer tecla para voltar...");
                    Console.ReadKey();
                }
                else if (op == "2")
                {
                    Console.Write("Digite o comando: ");
                    var cmd = await ReadLineAsync();
                    await app.SendCommandAsync(cmd);
                }
                else if (op == "3")
                {
                    await app.StopAsync();
                    Console.WriteLine("App parado. Pressione qualquer tecla...");
                    Console.ReadKey();
                }
                else if (op == "4")
                {
                    await app.RestartAsync();
                    Console.WriteLine("App reiniciado. Pressione qualquer tecla...");
                    Console.ReadKey();
                }
            }
        }

        // Leitura assíncrona de entrada do usuário, sem bloquear thread principal
        private static Task<string> ReadLineAsync()
        {
            return Task.Run(() => Console.ReadLine());
        }
    }

    // Logger simples para modo console
    public class ConsoleLogger<T> : ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }
    }
}