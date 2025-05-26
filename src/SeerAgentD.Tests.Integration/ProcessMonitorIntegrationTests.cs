using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SeerAgentD.Core.Models;
using SeerAgentD.Core.Services;
using Xunit;

namespace SeerAgentD.Tests.Integration
{
    public class ProcessMonitorIntegrationTests : IDisposable
    {
        private readonly ILogger<ProcessMonitor> _logger;
        private readonly ProcessMonitor _processMonitor;
        private readonly string _testExecutablePath;

        public ProcessMonitorIntegrationTests()
        {
            _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ProcessMonitor>();
            _processMonitor = new ProcessMonitor(_logger);
            _testExecutablePath = CreateTestExecutable();
        }

        [Fact]
        public async Task FullProcessLifecycle_ShouldWorkCorrectly()
        {
            // Arrange
            var processName = "IntegrationTest";
            ProcessInfo statusChangedInfo = null;
            var statusChanged = false;

            _processMonitor.ProcessStatusChanged += (sender, info) =>
            {
                statusChangedInfo = info;
                statusChanged = true;
            };

            try
            {
                // Act - Start Process
                await _processMonitor.StartMonitoringAsync(processName, _testExecutablePath);

                // Assert - Process Started
                Assert.True(statusChanged);
                Assert.NotNull(statusChangedInfo);
                Assert.Equal(processName, statusChangedInfo.Name);
                Assert.Equal(ProcessStatus.Running, statusChangedInfo.Status);

                // Wait for some monitoring data
                await Task.Delay(2000);

                // Get Process Info
                var processInfo = await _processMonitor.GetProcessInfoAsync(processName);
                Assert.NotNull(processInfo);
                Assert.True(processInfo.CpuUsage >= 0);
                Assert.True(processInfo.MemoryUsage > 0);

                // Stop Process
                await _processMonitor.StopMonitoringAsync(processName);

                // Assert - Process Stopped
                var stoppedProcess = await _processMonitor.GetProcessInfoAsync(processName);
                Assert.Null(stoppedProcess);
            }
            finally
            {
                await _processMonitor.StopMonitoringAsync(processName);
            }
        }

        [Fact]
        public async Task GetAllProcesses_ShouldReturnAllMonitoredProcesses()
        {
            // Arrange
            var process1Name = "IntegrationTest1";
            var process2Name = "IntegrationTest2";

            try
            {
                // Act
                await _processMonitor.StartMonitoringAsync(process1Name, _testExecutablePath);
                await _processMonitor.StartMonitoringAsync(process2Name, _testExecutablePath);

                await Task.Delay(1000); // Wait for processes to start

                var allProcesses = await _processMonitor.GetAllProcessesInfoAsync();

                // Assert
                Assert.Equal(2, allProcesses.Count());
                Assert.Contains(allProcesses, p => p.Name == process1Name);
                Assert.Contains(allProcesses, p => p.Name == process2Name);
            }
            finally
            {
                await _processMonitor.StopMonitoringAsync(process1Name);
                await _processMonitor.StopMonitoringAsync(process2Name);
            }
        }

        private string CreateTestExecutable()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "TestConsoleApp.exe");
            
            if (!File.Exists(tempPath))
            {
                // Create a simple console application that just waits
                var sourceCode = @"
                    using System;
                    using System.Threading;

                    class Program
                    {
                        static void Main()
                        {
                            while (true)
                            {
                                Thread.Sleep(100);
                            }
                        }
                    }";

                // Save source code to a temporary file
                var sourcePath = Path.Combine(Path.GetTempPath(), "TestConsoleApp.cs");
                File.WriteAllText(sourcePath, sourceCode);

                // Compile the source code
                var startInfo = new ProcessStartInfo
                {
                    FileName = "csc.exe",
                    Arguments = $"/out:\"{tempPath}\" \"{sourcePath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(startInfo);
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception("Failed to compile test executable");
                }

                File.Delete(sourcePath);
            }

            return tempPath;
        }

        public void Dispose()
        {
            _processMonitor.Dispose();
        }
    }
} 