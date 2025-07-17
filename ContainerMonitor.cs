using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace NodaStack
{
    public class ContainerMonitor : IDisposable
    {
        private readonly Dictionary<string, ContainerInfo> containers = new();
        private Timer? monitoringTimer;
        private readonly Action<string, ContainerInfo> onContainerUpdated;
        private bool disposed = false;

        public ContainerMonitor(Action<string, ContainerInfo> onContainerUpdated)
        {
            this.onContainerUpdated = onContainerUpdated;
            StartMonitoring();
        }

        private void StartMonitoring()
        {
            try
            {
                monitoringTimer = new Timer(MonitorContainers, null, 1000, 3000);
            }
            catch (Exception ex)
            {
                var errorInfo = new ContainerInfo { Name = "Monitor", Error = ex.Message };
                onContainerUpdated?.Invoke("Monitor", errorInfo);
            }
        }

        private async void MonitorContainers(object? state)
        {
            if (disposed) return;

            await Task.Run(() =>
            {
                try
                {
                    var containerNames = new[] { "nodastack_apache", "nodastack_php", "nodastack_mysql", "nodastack_phpmyadmin" };

                    foreach (var containerName in containerNames)
                    {
                        if (disposed) return;

                        var info = GetContainerInfo(containerName);

                        if (!containers.ContainsKey(containerName) || !containers[containerName].Equals(info))
                        {
                            containers[containerName] = info;
                            onContainerUpdated?.Invoke(containerName, info);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var errorInfo = new ContainerInfo { Name = "Monitor", Error = ex.Message };
                    onContainerUpdated?.Invoke("Monitor", errorInfo);
                }
            });
        }

        private ContainerInfo GetContainerInfo(string containerName)
        {
            var info = new ContainerInfo { Name = containerName };

            try
            {
                info.IsRunning = IsContainerRunning(containerName);

                if (info.IsRunning)
                {
                    info.Status = "Running";
                    info.CpuUsage = GetCpuUsage(containerName);
                    info.MemoryUsage = GetMemoryUsage(containerName);
                    info.Uptime = GetContainerUptime(containerName);
                }
                else
                {
                    info.Status = "Stopped";
                    info.Uptime = "Not running";
                }
            }
            catch (Exception ex)
            {
                info.Error = ex.Message;
                info.Status = "Error";
            }

            return info;
        }

        private bool IsContainerRunning(string containerName)
        {
            try
            {
                var output = RunDockerCommand($"ps -q --filter \"name=^{containerName}$\"");
                return !string.IsNullOrWhiteSpace(output);
            }
            catch
            {
                return false;
            }
        }

        private double GetCpuUsage(string containerName)
        {
            try
            {
                var output = RunDockerCommand($"stats --no-stream --format \"table {{{{.CPUPerc}}}}\" {containerName}");
                if (string.IsNullOrEmpty(output)) return 0.0;

                var lines = output.Split('\n');
                if (lines.Length < 2) return 0.0;

                var cpuStr = lines[1].Trim().Replace("%", "");
                return double.TryParse(cpuStr, out var cpu) ? cpu : 0.0;
            }
            catch
            {
                return 0.0;
            }
        }

        private long GetMemoryUsage(string containerName)
        {
            try
            {
                var output = RunDockerCommand($"stats --no-stream --format \"table {{{{.MemUsage}}}}\" {containerName}");
                if (string.IsNullOrEmpty(output)) return 0;

                var lines = output.Split('\n');
                if (lines.Length < 2) return 0;

                var memStr = lines[1].Split('/')[0].Trim();
                if (memStr.EndsWith("MiB"))
                {
                    memStr = memStr.Replace("MiB", "");
                    return long.TryParse(memStr, out var mem) ? mem : 0;
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private string GetContainerUptime(string containerName)
        {
            try
            {
                var output = RunDockerCommand($"ps --filter \"name=^{containerName}$\" --format \"{{{{.RunningFor}}}}\"");
                return string.IsNullOrEmpty(output) ? "Unknown" : output.Trim();
            }
            catch
            {
                return "Unknown";
            }
        }

        private string RunDockerCommand(string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return "";

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return process.ExitCode == 0 ? output : "";
            }
            catch
            {
                return "";
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                monitoringTimer?.Dispose();
                monitoringTimer = null;
            }
        }
    }

}