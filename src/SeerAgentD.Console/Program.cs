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
        }
    }
}