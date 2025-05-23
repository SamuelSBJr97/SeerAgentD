using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SeerD.Services
{
    /// <summary>
    /// Gerencia o ciclo de vida de um processo individual.
    /// </summary>
    public class ManagedProcess : IDisposable
    {
        private readonly ManagedAppConfig _config;
        private readonly ILogger _logger;
        private Process? _process;
        private CancellationTokenSource _cts;
        private StreamWriter _logWriter;

        public ManagedAppConfig Config => _config;

        public ManagedProcess(ManagedAppConfig config, ILogger logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

            try
            {
                await Task.Run(StartProcess, _cts.Token);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Processo cancelado pelo token de cancelamento.");
            }
        }

        private async void StartProcess()
        {
            try
            {
                // Garante que o diretório de trabalho existe
                Directory.CreateDirectory(_config.WorkingDirectory);

                // Cria arquivo de log no WorkingDirectory do app
                var logFile = Path.Combine(
                    _config.WorkingDirectory,
                    $"{_config.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.log"
                );
                _logWriter = new StreamWriter(logFile, append: true, encoding: Encoding.UTF8)
                {
                    AutoFlush = true
                };

                var psi = new ProcessStartInfo
                {
                    FileName = _config.ExecutablePath,
                    Arguments = _config.Arguments,
                    WorkingDirectory = _config.WorkingDirectory,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                _process = new Process { StartInfo = psi, EnableRaisingEvents = true };

                _process.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        var line = $"[OUT][{DateTime.Now:HH:mm:ss}] {e.Data}";
                        _logger.LogInformation($"[{_config.Name}] {e.Data}");
                        _logWriter.WriteLine(line);
                    }
                };
                _process.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        var line = $"[ERR][{DateTime.Now:HH:mm:ss}] {e.Data}";
                        _logger.LogError($"[{_config.Name}] {e.Data}");
                        _logWriter.WriteLine(line);
                    }
                };
                _process.Exited += async (s, e) =>
                {
                    _logger.LogWarning($"[{_config.Name}] Processo finalizado inesperadamente. Reiniciando...");
                    _logWriter?.WriteLine($"[SYS][{DateTime.Now:HH:mm:ss}] Processo finalizado inesperadamente. Reiniciando...");
                    _logWriter?.Flush();
                    _logWriter?.Close();

                    await Task.CompletedTask; // Placeholder para lógica adicional após a saída do processo
                };

                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao iniciar o processo {_config.Name}");
                _logWriter?.WriteLine($"[SYS][{DateTime.Now:HH:mm:ss}] Erro ao iniciar o processo: {ex}");
                _logWriter?.Flush();
                _logWriter?.Close();
            }
        }

        public async Task SendCommandAsync(string command)
        {
            if (_process != null && !_process.HasExited)
            {
                await _process.StandardInput.WriteLineAsync(command);
                await _process.StandardInput.FlushAsync();
            }
        }

        public async Task StopAsync()
        {
            try
            {
                _cts?.Cancel();
                if (_process != null && !_process.HasExited)
                {
                    _process.Kill(true);
                    await _process.WaitForExitAsync();
                    // faz o encerramento completo do processo
                    _process.Dispose();
                    _process = null;
                }
                _cts?.TryReset();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao parar o processo {_config.Name}");
            }
        }

        public async Task RestartAsync()
        {
            await StopAsync();
            await StartAsync(_cts?.Token ?? CancellationToken.None);
        }

        public async Task ShowAppMenuAsync()
        {
            if (_process == null)
            {
                Console.WriteLine("Processo não iniciado.");
                return;
            }

            Console.WriteLine("Informações do processo:");
            Console.WriteLine($"Id: {_process.Id}");
            Console.WriteLine($"ProcessName: {_process.ProcessName}");
            Console.WriteLine($"HasExited: {_process.HasExited}");
            Console.WriteLine($"StartTime: {(_process.HasExited ? "N/A" : _process.StartTime.ToString())}");
            Console.WriteLine($"TotalProcessorTime: {(_process.HasExited ? "N/A" : _process.TotalProcessorTime.ToString())}");
            Console.WriteLine($"MainWindowTitle: {_process.MainWindowTitle}");
            Console.WriteLine($"MainModule: {(_process.HasExited ? "N/A" : _process.MainModule?.FileName)}");
            Console.WriteLine($"Responding: {_process.Responding}");
            Console.WriteLine($"Threads: {_process.Threads.Count}");
            Console.WriteLine($"WorkingSet64: {_process.WorkingSet64}");
            Console.WriteLine($"PagedMemorySize64: {_process.PagedMemorySize64}");
            Console.WriteLine($"VirtualMemorySize64: {_process.VirtualMemorySize64}");
            Console.WriteLine($"StartInfo: {_process.StartInfo.FileName} {_process.StartInfo.Arguments}");
            Console.WriteLine($"EnableRaisingEvents: {_process.EnableRaisingEvents}");
            Console.WriteLine($"ExitCode: {(_process.HasExited ? _process.ExitCode.ToString() : "N/A")}");
            Console.WriteLine($"ExitTime: {(_process.HasExited ? _process.ExitTime.ToString() : "N/A")}");
            Console.WriteLine($"MachineName: {_process.MachineName}");
            Console.WriteLine($"SessionId: {_process.SessionId}");
            Console.WriteLine($"PriorityClass: {(_process.HasExited ? "N/A" : _process.PriorityClass.ToString())}");
            Console.WriteLine($"BasePriority: {_process.BasePriority}");
            Console.WriteLine($"Handle: {_process.Handle}");
            Console.WriteLine($"HandleCount: {_process.HandleCount}");
            Console.WriteLine($"UserProcessorTime: {(_process.HasExited ? "N/A" : _process.UserProcessorTime.ToString())}");
            Console.WriteLine($"PrivilegedProcessorTime: {(_process.HasExited ? "N/A" : _process.PrivilegedProcessorTime.ToString())}");

            await Task.CompletedTask;
        }

        public void Dispose()
        {
            _process.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}