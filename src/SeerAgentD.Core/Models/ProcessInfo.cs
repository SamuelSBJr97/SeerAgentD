using System;

namespace SeerAgentD.Core.Models
{
    public class ProcessInfo
    {
        public int ProcessId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public ProcessStatus Status { get; set; }
        public double CpuUsage { get; set; }
        public long MemoryUsage { get; set; }
    }

    public enum ProcessStatus
    {
        Running,
        Stopped,
        Failed,
        Starting,
        Stopping
    }
} 