using SeerD.Services;
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
                System.Console.Clear();
                System.Console.WriteLine("=== SeerD - Gerenciador de Apps ===");

                for (int i = 0; i < apps.Count; i++)
                    System.Console.WriteLine($"{i + 1}. {apps[i].Config.Name}");
                System.Console.WriteLine("0. Sair");
                System.Console.Write("Selecione um app para gerenciar: ");

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
                System.Console.Clear();
                System.Console.WriteLine($"=== {app.Config.Name} ===");
                await app.ShowAppMenuAsync();
                System.Console.WriteLine($"=== {app.Config.Name} ===");
                System.Console.WriteLine("1. Ver output (últimas linhas do log)");
                System.Console.WriteLine("2. Enviar comando");
                System.Console.WriteLine("3. Parar app");
                System.Console.WriteLine("4. Reiniciar app");
                System.Console.WriteLine("0. Voltar");
                System.Console.WriteLine($"=== {app.Config.Name} ===");

                System.Console.Write("Escolha: ");
                var op = await ReadLineAsync();
                if (op == "0") break;
                if (op == "1")
                {
                    System.Console.Clear();
                    var logPath = Directory.GetFiles(app.Config.WorkingDirectory, $"{app.Config.Name}_*.log")
                        .OrderByDescending(f => f)
                        .FirstOrDefault();
                    if (logPath != null)
                    {
                        var lines = File.ReadLines(logPath).Reverse().Take(40).Reverse();
                        foreach (var line in lines)
                            System.Console.WriteLine(line);
                    }
                    else
                    {
                        System.Console.WriteLine("Nenhum log encontrado.");
                    }
                    System.Console.WriteLine("\nPressione qualquer tecla para voltar...");
                    System.Console.ReadKey();
                }
                else if (op == "2")
                {
                    System.Console.Write("Digite o comando: ");
                    var cmd = await ReadLineAsync();
                    await app.SendCommandAsync(cmd);
                }
                else if (op == "3")
                {
                    await app.StopAsync();
                    System.Console.WriteLine("App parado. Pressione qualquer tecla...");
                    System.Console.ReadKey();
                    break;
                }
                else if (op == "4")
                {
                    await app.RestartAsync();
                    System.Console.WriteLine("App reiniciado. Pressione qualquer tecla...");
                }
            }

            await Task.CompletedTask;
        }

        // Leitura assíncrona de entrada do usuário, sem bloquear thread principal
        private static Task<string> ReadLineAsync()
        {
            return Task.Run(() => System.Console.ReadLine() ?? string.Empty);
        }
    }
}
