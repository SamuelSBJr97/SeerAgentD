using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SeerAgentD.Core.Models;

namespace SeerAgentD.Core.Interfaces
{
    public interface IProcessMonitor
    {
        Task StartMonitoringAsync(string processName, string executablePath, string arguments = "", string workingDirectory = "", CancellationToken cancellationToken = default);
        Task StopMonitoringAsync(string processName, CancellationToken cancellationToken = default);
        Task<ProcessInfo?> GetProcessInfoAsync(string processName, CancellationToken cancellationToken = default);
        Task<IEnumerable<ProcessInfo>> GetAllProcessesInfoAsync(CancellationToken cancellationToken = default);
        event EventHandler<ProcessInfo>? ProcessStatusChanged;
    }
} 