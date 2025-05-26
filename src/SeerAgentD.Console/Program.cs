using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SeerAgentD.Console.Console;
using SeerAgentD.Console.Services;
using SeerAgentD.Core.Interfaces;
using SeerAgentD.Core.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SeerAgentD.Console
{
    internal static class Program
    {
        public static async Task Main(string[] args)
        {
            if (args.Contains("--console"))
            {
                var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                var processMonitor = new ProcessMonitor(loggerFactory.CreateLogger<ProcessMonitor>());

                try
                {
                    await ConsoleInterface.RunAsync(processMonitor);
                }
                finally
                {
                    (processMonitor as IDisposable)?.Dispose();
                }
            }
            else
            {
                var host = Host.CreateDefaultBuilder(args)
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
                        services.AddSingleton<IProcessMonitor, ProcessMonitor>();
                        services.AddHostedService<Worker>();
                    })
                    .Build();

                await host.RunAsync();
            }
        }
    }
}