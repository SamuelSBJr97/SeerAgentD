using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SeerD.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using SeerAgentD.Console.Console;
using SeerAgentD.Console.Services;

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

            string? logPath = Path.Combine(AppContext.BaseDirectory, "Logs", "seer.log");
            if (logPath != null)
            {
                List<string> lines = new();
                using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs))
                {
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                        lines.Add(line);
                }
                foreach (var line in lines.Skip(Math.Max(0, lines.Count - 40)))
                    Console.WriteLine(line);
            }
            else
            {
                Console.WriteLine("Nenhum log encontrado.");
            }
        }
    }
}