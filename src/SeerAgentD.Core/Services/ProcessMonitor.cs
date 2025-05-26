using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SeerAgentD.Core.Interfaces;
using SeerAgentD.Core.Models;

namespace SeerAgentD.Core.Services
{
    public class ProcessMonitor : IProcessMonitor, IDisposable
    {
        private readonly ILogger<ProcessMonitor> _logger;
        private readonly ConcurrentDictionary<string, ProcessInfo> _processes;
        private readonly ConcurrentDictionary<string, Process> _systemProcesses;
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _monitoringTokens;
        private bool _disposed;

        public event EventHandler<ProcessInfo>? ProcessStatusChanged;

        public ProcessMonitor(ILogger<ProcessMonitor> logger)
        {
            _logger = logger;
            _processes = new ConcurrentDictionary<string, ProcessInfo>();
            _systemProcesses = new ConcurrentDictionary<string, Process>();
            _monitoringTokens = new ConcurrentDictionary<string, CancellationTokenSource>();
        }

        public async Task StartMonitoringAsync(string processName, string executablePath, string arguments = "", 
            string workingDirectory = "", CancellationToken cancellationToken = default)
        {
            try
            {
                if (_processes.ContainsKey(processName))
                {
                    throw new InvalidOperationException($"Process {processName} is already being monitored.");
                }

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                var process = new Process { StartInfo = processStartInfo };
                var processInfo = new ProcessInfo
                {
                    Name = processName,
                    ExecutablePath = executablePath,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    Status = ProcessStatus.Starting
                };

                _processes.TryAdd(processName, processInfo);
                
                process.Start();
                processInfo.ProcessId = process.Id;
                processInfo.StartTime = process.StartTime;
                processInfo.Status = ProcessStatus.Running;

                _systemProcesses.TryAdd(processName, process);

                var monitoringToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _monitoringTokens.TryAdd(processName, monitoringToken);

                _ = MonitorProcessAsync(processName, monitoringToken.Token);

                OnProcessStatusChanged(processInfo);
                _logger.LogInformation($"Started monitoring process {processName} with ID {process.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to start monitoring process {processName}");
                throw;
            }
        }

        public async Task StopMonitoringAsync(string processName, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_processes.TryGetValue(processName, out var processInfo))
                {
                    processInfo.Status = ProcessStatus.Stopping;
                    OnProcessStatusChanged(processInfo);

                    if (_systemProcesses.TryRemove(processName, out var process))
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                            await process.WaitForExitAsync(cancellationToken);
                        }
                        process.Dispose();
                    }

                    if (_monitoringTokens.TryRemove(processName, out var cts))
                    {
                        cts.Cancel();
                        cts.Dispose();
                    }

                    _processes.TryRemove(processName, out _);
                    _logger.LogInformation($"Stopped monitoring process {processName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to stop monitoring process {processName}");
                throw;
            }
        }

        public Task<ProcessInfo?> GetProcessInfoAsync(string processName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_processes.TryGetValue(processName, out var processInfo) ? processInfo : null);
        }

        public Task<IEnumerable<ProcessInfo>> GetAllProcessesInfoAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_processes.Values.AsEnumerable());
        }

        private async Task MonitorProcessAsync(string processName, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (_systemProcesses.TryGetValue(processName, out var process) && 
                        _processes.TryGetValue(processName, out var processInfo))
                    {
                        if (process.HasExited)
                        {
                            processInfo.Status = ProcessStatus.Failed;
                            OnProcessStatusChanged(processInfo);
                            _logger.LogWarning($"Process {processName} has exited unexpectedly");
                            break;
                        }

                        process.Refresh();
                        processInfo.CpuUsage = process.TotalProcessorTime.TotalMilliseconds;
                        processInfo.MemoryUsage = process.WorkingSet64;
                        OnProcessStatusChanged(processInfo);
                    }

                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation, do nothing
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error monitoring process {processName}");
            }
        }

        protected virtual void OnProcessStatusChanged(ProcessInfo processInfo)
        {
            ProcessStatusChanged?.Invoke(this, processInfo);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                foreach (var processName in _processes.Keys.ToList())
                {
                    StopMonitoringAsync(processName).GetAwaiter().GetResult();
                }
            }

            _disposed = true;
        }
    }
} 