using Microsoft.Extensions.Logging;
using System;
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
    public class ManagedProcess
    {
        private readonly ManagedAppConfig _config;
        private readonly ILogger _logger;
        private Process _process;
        private CancellationTokenSource _cts;
        private StreamWriter _logWriter;

        public ManagedProcess(ManagedAppConfig config, ILogger logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

            await Task.Run(() => StartProcess(), _cts.Token);
        }

        private void StartProcess()
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
                    await RestartAsync();
                };

                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
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
                }
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
    }
}