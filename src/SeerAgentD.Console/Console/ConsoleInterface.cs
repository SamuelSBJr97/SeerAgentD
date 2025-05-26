using SeerAgentD.Core.Interfaces;
using SeerAgentD.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeerAgentD.Console.Console
{
    /// <summary>
    /// Interface de texto reativa para gerenciamento dos apps.
    /// </summary>
    internal static class ConsoleInterface
    {
        public static async Task RunAsync(IProcessMonitor processMonitor)
        {
            System.Console.WriteLine("SeerAgentD Console Interface");
            System.Console.WriteLine("Commands:");
            System.Console.WriteLine("  start <name> <path> [args] [workdir] - Start monitoring a process");
            System.Console.WriteLine("  stop <name>                          - Stop monitoring a process");
            System.Console.WriteLine("  list                                 - List all monitored processes");
            System.Console.WriteLine("  info <name>                         - Show detailed process info");
            System.Console.WriteLine("  exit                                - Exit the application");
            System.Console.WriteLine();

            processMonitor.ProcessStatusChanged += (sender, info) =>
            {
                System.Console.WriteLine($"Process {info.Name} status changed to {info.Status}");
            };

            while (true)
            {
                System.Console.Write("> ");
                var command = System.Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(command)) continue;

                var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;

                try
                {
                    switch (parts[0].ToLower())
                    {
                        case "start":
                            if (parts.Length < 3)
                            {
                                System.Console.WriteLine("Usage: start <name> <path> [args] [workdir]");
                                continue;
                            }
                            var args = parts.Length > 3 ? parts[3] : "";
                            var workdir = parts.Length > 4 ? parts[4] : "";
                            await processMonitor.StartMonitoringAsync(parts[1], parts[2], args, workdir);
                            break;

                        case "stop":
                            if (parts.Length < 2)
                            {
                                System.Console.WriteLine("Usage: stop <name>");
                                continue;
                            }
                            await processMonitor.StopMonitoringAsync(parts[1]);
                            break;

                        case "list":
                            var processes = await processMonitor.GetAllProcessesInfoAsync();
                            if (!processes.Any())
                            {
                                System.Console.WriteLine("No processes are currently being monitored.");
                            }
                            else
                            {
                                foreach (var process in processes)
                                {
                                    System.Console.WriteLine(
                                        $"{process.Name} (ID: {process.ProcessId}) - Status: {process.Status}, " +
                                        $"CPU: {process.CpuUsage:F2}ms, Memory: {process.MemoryUsage / 1024 / 1024}MB"
                                    );
                                }
                            }
                            break;

                        case "info":
                            if (parts.Length < 2)
                            {
                                System.Console.WriteLine("Usage: info <name>");
                                continue;
                            }
                            var processInfo = await processMonitor.GetProcessInfoAsync(parts[1]);
                            if (processInfo != null)
                            {
                                System.Console.WriteLine($"Name: {processInfo.Name}");
                                System.Console.WriteLine($"Process ID: {processInfo.ProcessId}");
                                System.Console.WriteLine($"Status: {processInfo.Status}");
                                System.Console.WriteLine($"Executable: {processInfo.ExecutablePath}");
                                System.Console.WriteLine($"Arguments: {processInfo.Arguments}");
                                System.Console.WriteLine($"Working Directory: {processInfo.WorkingDirectory}");
                                System.Console.WriteLine($"Start Time: {processInfo.StartTime}");
                                System.Console.WriteLine($"CPU Usage: {processInfo.CpuUsage:F2}ms");
                                System.Console.WriteLine($"Memory Usage: {processInfo.MemoryUsage / 1024 / 1024}MB");
                            }
                            else
                            {
                                System.Console.WriteLine($"Process {parts[1]} not found");
                            }
                            break;

                        case "help":
                            System.Console.WriteLine("Available commands:");
                            System.Console.WriteLine("  start <name> <path> [args] [workdir] - Start monitoring a process");
                            System.Console.WriteLine("  stop <name>                          - Stop monitoring a process");
                            System.Console.WriteLine("  list                                 - List all monitored processes");
                            System.Console.WriteLine("  info <name>                         - Show detailed process info");
                            System.Console.WriteLine("  exit                                - Exit the application");
                            break;

                        case "exit":
                            return;

                        default:
                            System.Console.WriteLine("Unknown command. Type 'help' for available commands.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
}
