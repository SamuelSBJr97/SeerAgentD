using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SeerAgentD.Core.Services;
using Xunit;

namespace SeerAgentD.Tests.Unit
{
    public class ProcessMonitorTests
    {
        private readonly Mock<ILogger<ProcessMonitor>> _loggerMock;
        private readonly ProcessMonitor _processMonitor;

        public ProcessMonitorTests()
        {
            _loggerMock = new Mock<ILogger<ProcessMonitor>>();
            _processMonitor = new ProcessMonitor(_loggerMock.Object);
        }

        [Fact]
        public async Task StartMonitoring_WithValidProcess_ShouldStartMonitoring()
        {
            // Arrange
            var processName = "TestProcess";
            var executablePath = "test.exe";
            var statusChangedCalled = false;

            _processMonitor.ProcessStatusChanged += (sender, info) =>
            {
                statusChangedCalled = true;
                Assert.Equal(processName, info.Name);
                Assert.Equal(executablePath, info.ExecutablePath);
            };

            try
            {
                // Act & Assert
                await Assert.ThrowsAsync<System.ComponentModel.Win32Exception>(
                    () => _processMonitor.StartMonitoringAsync(processName, executablePath)
                );

                Assert.True(statusChangedCalled, "ProcessStatusChanged event should have been raised");
            }
            finally
            {
                await _processMonitor.StopMonitoringAsync(processName);
            }
        }

        [Fact]
        public async Task StartMonitoring_DuplicateProcess_ShouldThrowException()
        {
            // Arrange
            var processName = "TestProcess";
            var executablePath = "test.exe";

            try
            {
                // Act & Assert
                await Assert.ThrowsAsync<System.ComponentModel.Win32Exception>(
                    () => _processMonitor.StartMonitoringAsync(processName, executablePath)
                );

                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _processMonitor.StartMonitoringAsync(processName, executablePath)
                );
            }
            finally
            {
                await _processMonitor.StopMonitoringAsync(processName);
            }
        }

        [Fact]
        public async Task GetProcessInfo_NonExistentProcess_ShouldReturnNull()
        {
            // Act
            var result = await _processMonitor.GetProcessInfoAsync("NonExistentProcess");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllProcessesInfo_ShouldReturnEmptyListInitially()
        {
            // Act
            var result = await _processMonitor.GetAllProcessesInfoAsync();

            // Assert
            Assert.Empty(result);
        }
    }
} 