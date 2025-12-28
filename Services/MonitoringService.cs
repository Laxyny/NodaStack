using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualBasic.Devices;
using NodaStack.Core.Models;

namespace NodaStack.Services
{
    public class MonitoringService : IDisposable
    {
        private readonly Dictionary<string, ServiceStatus> services = new();
        private readonly List<PortStatus> ports = new();
        private readonly List<MonitoringLogEntry> logs = new();
        private readonly SystemMetrics systemMetrics = new();
        private readonly Timer? monitoringTimer;
        private readonly Timer? metricsTimer;
        private readonly Timer? logTimer;
        private bool disposed = false;

        public event Action<ServiceStatus>? ServiceUpdated;
        public event Action<PortStatus>? PortUpdated;
        public event Action<MonitoringLogEntry>? LogAdded;
        public event Action<SystemMetrics>? SystemMetricsUpdated;

        public MonitoringService()
        {
            InitializeServices();
            InitializePorts();
            
            AddLog("Info", "Monitoring", "Monitoring service started");
            
            // Lister tous les conteneurs pour déboguer
            ListAllRunningContainers();
            
            monitoringTimer = new Timer(MonitorServices, null, 0, 2000);
            metricsTimer = new Timer(UpdateSystemMetrics, null, 0, 5000);
            logTimer = new Timer(CollectLogs, null, 0, 10000);
        }

        private void InitializeServices()
        {
            var serviceConfigs = new[]
            {
                new { Name = "Apache", Port = "8080", Container = "nodastack_apache" },
                new { Name = "PHP", Port = "8000", Container = "nodastack_php" },
                new { Name = "MySQL", Port = "3306", Container = "nodastack_mysql" },
                new { Name = "phpMyAdmin", Port = "8081", Container = "nodastack_phpmyadmin" }
            };

            foreach (var config in serviceConfigs)
            {
                services[config.Name] = new ServiceStatus
                {
                    Name = config.Name,
                    Port = config.Port,
                    Status = "Unknown",
                    LastUpdated = DateTime.Now
                };
            }
        }

        private void InitializePorts()
        {
            var portConfigs = new[]
            {
                new { Port = 8080, Service = "Apache", Description = "Web Server" },
                new { Port = 8000, Service = "PHP", Description = "PHP Development Server" },
                new { Port = 3306, Service = "MySQL", Description = "Database Server" },
                new { Port = 8081, Service = "phpMyAdmin", Description = "Database Management" }
            };

            foreach (var config in portConfigs)
            {
                ports.Add(new PortStatus
                {
                    Port = config.Port,
                    Service = config.Service,
                    Description = config.Description,
                    LastChecked = DateTime.Now
                });
            }
        }

        private async void MonitorServices(object? state)
        {
            if (disposed) return;

            await Task.Run(() =>
            {
                try
                {
                    foreach (var service in services.Values)
                    {
                        UpdateServiceStatus(service);
                        AddLog("Debug", "Monitoring", $"Invoking ServiceUpdated event for {service.Name} with status: {service.Status}");
                        ServiceUpdated?.Invoke(service);
                    }

                    // Mettre à jour les ports en même temps
                    foreach (var port in ports)
                    {
                        UpdatePortStatus(port);
                        AddLog("Debug", "Monitoring", $"Invoking PortUpdated event for port {port.Port} with status: {port.Status}");
                        PortUpdated?.Invoke(port);
                    }
                }
                catch (Exception ex)
                {
                    AddLog("Error", "Monitoring", $"Error monitoring services: {ex.Message}");
                }
            });
        }

        private void UpdatePortStatus(PortStatus port)
        {
            try
            {
                var isOpen = IsServiceHealthy(GetServiceForPort(port.Port), port.Port.ToString());
                port.Status = isOpen ? "Open" : "Closed";
                port.IsOpen = isOpen;
                port.LastChecked = DateTime.Now;
                
                AddLog("Debug", "Monitoring", $"Port {port.Port} ({port.Service}): {(isOpen ? "Open" : "Closed")}");
            }
            catch (Exception ex)
            {
                port.Status = "Error";
                port.LastChecked = DateTime.Now;
                AddLog("Error", "Monitoring", $"Error checking port {port.Port}: {ex.Message}");
            }
        }

        private void UpdateServiceStatus(ServiceStatus service)
        {
            try
            {
                var containerName = GetContainerName(service.Name);
                AddLog("Debug", "Monitoring", $"Updating service {service.Name} (container: {containerName})");
                
                var isRunning = IsContainerRunning(containerName);
                AddLog("Debug", "Monitoring", $"Service {service.Name} running: {isRunning}");

                service.Status = isRunning ? "Running" : "Stopped";
                service.IsHealthy = isRunning && IsServiceHealthy(service.Name, service.Port);

                if (isRunning)
                {
                    service.CpuUsage = GetCpuUsage(containerName);
                    service.MemoryUsage = GetMemoryUsage(containerName);
                    service.Uptime = GetContainerUptime(containerName);
                    service.Version = GetServiceVersion(service.Name);
                    
                    AddLog("Debug", "Monitoring", $"Service {service.Name} - CPU: {service.CpuUsage}%, Memory: {service.MemoryUsage} bytes, Uptime: {service.Uptime}");
                }
                else
                {
                    service.CpuUsage = 0;
                    service.MemoryUsage = 0;
                    service.Uptime = "00:00:00";
                    AddLog("Debug", "Monitoring", $"Service {service.Name} is stopped");
                }

                service.LastUpdated = DateTime.Now;
                UpdateServiceMetrics(service);
            }
            catch (Exception ex)
            {
                service.Status = "Error";
                service.LastError = ex.Message;
                service.LastUpdated = DateTime.Now;
                AddLog("Error", "Monitoring", $"Error updating service {service.Name}: {ex.Message}");
            }
        }

        private string GetContainerName(string serviceName)
        {
            return serviceName.ToLower() switch
            {
                "apache" => "nodastack_apache",
                "php" => "nodastack_php",
                "mysql" => "nodastack_mysql",
                "phpmyadmin" => "nodastack_phpmyadmin",
                _ => $"nodastack_{serviceName.ToLower()}"
            };
        }

        private bool IsContainerRunning(string containerName)
        {
            try
            {
                // Méthode 1: Commande Docker directe avec timeout plus long
                var output = RunDockerCommand($"ps -q --filter \"name=^{containerName}$\"");
                var isRunning = !string.IsNullOrEmpty(output?.Trim());
                
                // Log pour déboguer
                AddLog("Debug", "Monitoring", $"Container {containerName}: {(isRunning ? "Running" : "Not running")} - Output: '{output?.Trim()}'");
                
                // Méthode 2: Vérification alternative avec docker ps si la première échoue
                if (!isRunning)
                {
                    AddLog("Debug", "Monitoring", $"Trying alternative method for {containerName}");
                    var psOutput = RunDockerCommand("ps --format \"{{.Names}}\"");
                    if (psOutput != null && psOutput.Contains(containerName))
                    {
                        AddLog("Debug", "Monitoring", $"Container {containerName} found in docker ps output");
                        isRunning = true;
                    }
                    else
                    {
                        AddLog("Debug", "Monitoring", $"Container {containerName} NOT found in docker ps output: '{psOutput}'");
                    }
                }
                
                return isRunning;
            }
            catch (Exception ex)
            {
                AddLog("Error", "Monitoring", $"Error checking container {containerName}: {ex.Message}");
                return false;
            }
        }

        private bool IsServiceHealthy(string serviceName, string port)
        {
            try
            {
                if (int.TryParse(port, out int portNumber))
                {
                    using var client = new TcpClient();
                    var result = client.BeginConnect("localhost", portNumber, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2));
                    client.EndConnect(result);
                    
                    // Log pour déboguer
                    AddLog("Debug", "Monitoring", $"Service {serviceName} on port {portNumber}: {(success ? "Healthy" : "Unhealthy")}");
                    
                    return success;
                }
                return false;
            }
            catch (Exception ex)
            {
                AddLog("Error", "Monitoring", $"Error checking service {serviceName} health: {ex.Message}");
                return false;
            }
        }

        private double GetCpuUsage(string containerName)
        {
            try
            {
                var output = RunDockerCommand($"stats {containerName} --no-stream --format \"{{{{.CPUPerc}}}}\"");
                if (output != null)
                {
                    var cpuStr = output.Replace("%", "").Trim();
                    // Gérer les valeurs décimales avec virgule ou point
                    cpuStr = cpuStr.Replace(",", ".");
                    
                    if (double.TryParse(cpuStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double cpu))
                    {
                        AddLog("Debug", "Monitoring", $"CPU for {containerName}: {cpu}%");
                        return cpu;
                    }
                }
                AddLog("Warning", "Monitoring", $"Could not parse CPU for {containerName}: '{output}'");
                return 0;
            }
            catch (Exception ex)
            {
                AddLog("Error", "Monitoring", $"Error getting CPU for {containerName}: {ex.Message}");
                return 0;
            }
        }

        private long GetMemoryUsage(string containerName)
        {
            try
            {
                var output = RunDockerCommand($"stats {containerName} --no-stream --format \"{{{{.MemUsage}}}}\"");
                if (output != null)
                {
                    var parts = output.Split('/');
                    if (parts.Length > 0)
                    {
                        var memoryStr = parts[0].Trim();
                        
                        // Gérer différents formats : MiB, MB, GiB, GB
                        long multiplier = 1024 * 1024; // Default to MB
                        if (memoryStr.Contains("GiB") || memoryStr.Contains("GB"))
                        {
                            multiplier = 1024 * 1024 * 1024;
                            memoryStr = memoryStr.Replace("GiB", "").Replace("GB", "").Trim();
                        }
                        else if (memoryStr.Contains("MiB") || memoryStr.Contains("MB"))
                        {
                            multiplier = 1024 * 1024;
                            memoryStr = memoryStr.Replace("MiB", "").Replace("MB", "").Trim();
                        }
                        else if (memoryStr.Contains("KiB") || memoryStr.Contains("KB"))
                        {
                            multiplier = 1024;
                            memoryStr = memoryStr.Replace("KiB", "").Replace("KB", "").Trim();
                        }
                        
                        // Gérer les valeurs décimales
                        memoryStr = memoryStr.Replace(",", ".");
                        
                        if (double.TryParse(memoryStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double memory))
                        {
                            var memoryBytes = (long)(memory * multiplier);
                            AddLog("Debug", "Monitoring", $"Memory for {containerName}: {memory} units ({memoryBytes} bytes)");
                            return memoryBytes;
                        }
                    }
                }
                AddLog("Warning", "Monitoring", $"Could not parse memory for {containerName}: '{output}'");
                return 0;
            }
            catch (Exception ex)
            {
                AddLog("Error", "Monitoring", $"Error getting memory for {containerName}: {ex.Message}");
                return 0;
            }
        }

        private string GetContainerUptime(string containerName)
        {
            try
            {
                var output = RunDockerCommand($"ps --filter \"name=^{containerName}$\" --format \"{{{{.RunningFor}}}}\"");
                var uptime = output?.Trim() ?? "00:00:00";
                AddLog("Debug", "Monitoring", $"Uptime for {containerName}: {uptime}");
                return uptime;
            }
            catch (Exception ex)
            {
                AddLog("Error", "Monitoring", $"Error getting uptime for {containerName}: {ex.Message}");
                return "00:00:00";
            }
        }

        private string GetServiceVersion(string serviceName)
        {
            try
            {
                return serviceName switch
                {
                    "Apache" => "2.4.57",
                    "PHP" => "8.2.0",
                    "MySQL" => "8.0.33",
                    "phpMyAdmin" => "5.2.1",
                    _ => "Unknown"
                };
            }
            catch
            {
                return "Unknown";
            }
        }

        private void UpdateServiceMetrics(ServiceStatus service)
        {
            var metrics = service.Metrics;
            
            // Update averages
            metrics.CpuAverage = (metrics.CpuAverage + service.CpuUsage) / 2;
            metrics.MemoryAverage = (metrics.MemoryAverage + service.MemoryUsage) / 2;

            // Add to history (keep last 50 points)
            metrics.History.Add(new MetricPoint
            {
                Timestamp = DateTime.Now,
                Value = service.CpuUsage,
                Label = "CPU"
            });

            if (metrics.History.Count > 50)
            {
                metrics.History.RemoveAt(0);
            }
        }

        private async void UpdateSystemMetrics(object? state)
        {
            if (disposed) return;

            await Task.Run(() =>
            {
                try
                {
                    UpdateSystemMetricsInternal();
                    SystemMetricsUpdated?.Invoke(systemMetrics);
                }
                catch (Exception ex)
                {
                    AddLog("Error", "System", $"Error updating system metrics: {ex.Message}");
                }
            });
        }

        private void UpdateSystemMetricsInternal()
        {
            try
            {
                // CPU Usage - Need to call NextValue() twice for accurate reading
                var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                cpuCounter.NextValue(); // First call to initialize
                Thread.Sleep(100); // Small delay for accurate measurement
                systemMetrics.TotalCpuUsage = cpuCounter.NextValue();

                // Memory - Get total physical memory
                var availableMemoryCounter = new PerformanceCounter("Memory", "Available MBytes");
                var availableMB = availableMemoryCounter.NextValue();
                systemMetrics.AvailableMemory = (long)(availableMB * 1024 * 1024);
                
                // Total memory from system info
                var totalPhysicalMemory = new ComputerInfo().TotalPhysicalMemory;
                systemMetrics.TotalMemoryUsage = (long)(totalPhysicalMemory - (ulong)systemMetrics.AvailableMemory);

                // Ensure we don't have negative values
                if (systemMetrics.TotalMemoryUsage < 0)
                {
                    systemMetrics.TotalMemoryUsage = 0;
                }

                // Get disk space
                var drive = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory) ?? "C:");
                systemMetrics.TotalDiskSpace = drive.TotalSize;
                systemMetrics.AvailableDiskSpace = drive.AvailableFreeSpace;

                // Get network connections (simplified)
                systemMetrics.ActiveConnections = 0; // Will be updated by port scanning

                systemMetrics.LastUpdated = DateTime.Now;
                
                AddLog("Debug", "System", $"CPU: {systemMetrics.TotalCpuUsage:F1}%, Memory: {systemMetrics.TotalMemoryUsage / (1024*1024*1024):F1}GB used, {systemMetrics.AvailableMemory / (1024*1024*1024):F1}GB available");
            }
            catch (Exception ex)
            {
                AddLog("Error", "System", $"Error getting system metrics: {ex.Message}");
            }
        }

        private async void CollectLogs(object? state)
        {
            if (disposed) return;

            await Task.Run(() =>
            {
                try
                {
                    CollectDockerLogs();
                    CollectApplicationLogs();
                }
                catch (Exception ex)
                {
                    AddLog("Error", "Monitoring", $"Error collecting logs: {ex.Message}");
                }
            });
        }

        private void CollectDockerLogs()
        {
            try
            {
                foreach (var service in services.Values)
                {
                    var containerName = GetContainerName(service.Name);
                    var logs = RunDockerCommand($"logs {containerName} --tail 5 --timestamps");
                    
                    if (!string.IsNullOrEmpty(logs))
                    {
                        var logLines = logs.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in logLines)
                        {
                            AddLog("Info", service.Name, line.Trim());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog("Error", "Docker", $"Error collecting Docker logs: {ex.Message}");
            }
        }

        private void CollectApplicationLogs()
        {
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (Directory.Exists(logPath))
                {
                    var logFiles = Directory.GetFiles(logPath, "*.log");
                    foreach (var logFile in logFiles.Take(5))
                    {
                        var lines = File.ReadLines(logFile).TakeLast(10);
                        foreach (var line in lines)
                        {
                            AddLog("Info", "Application", line.Trim());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog("Error", "Application", $"Error collecting application logs: {ex.Message}");
            }
        }

        private void AddLog(string level, string service, string message)
        {
            var logEntry = new MonitoringLogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Service = service,
                Message = message
            };

            logs.Add(logEntry);
            
            // Keep only last 1000 logs
            if (logs.Count > 1000)
            {
                logs.RemoveAt(0);
            }

            LogAdded?.Invoke(logEntry);
        }

        private string? RunDockerCommand(string arguments)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.CurrentDirectory
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                    process.WaitForExit(10000); // Augmenté le timeout
                    
                    if (process.ExitCode != 0)
                    {
                        AddLog("Error", "Docker", $"Docker command failed: {arguments} - Exit code: {process.ExitCode} - Error: {error}");
                        return null;
                    }
                    
                    AddLog("Debug", "Docker", $"Docker command: {arguments} - Output: '{output?.Trim()}' - Exit code: {process.ExitCode}");
                    return output;
                }
                return null;
            }
            catch (Exception ex)
            {
                AddLog("Error", "Docker", $"Exception running Docker command '{arguments}': {ex.Message}");
                return null;
            }
        }

        private void ListAllRunningContainers()
        {
            try
            {
                var output = RunDockerCommand("ps --format \"{{.Names}}\"");
                if (output != null)
                {
                    var containers = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    AddLog("Info", "Monitoring", $"Running containers: {string.Join(", ", containers)}");
                }
                else
                {
                    AddLog("Warning", "Monitoring", "Could not get list of running containers");
                }
            }
            catch (Exception ex)
            {
                AddLog("Error", "Monitoring", $"Error listing containers: {ex.Message}");
            }
        }

        public async Task<List<PortStatus>> ScanPortsAsync(int startPort, int endPort)
        {
            var results = new List<PortStatus>();
            
            await Task.Run(() =>
            {
                for (int port = startPort; port <= endPort; port++)
                {
                    var status = new PortStatus
                    {
                        Port = port,
                        LastChecked = DateTime.Now
                    };

                    try
                    {
                        using var client = new TcpClient();
                        var result = client.BeginConnect("localhost", port, null, null);
                        var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(500));
                        
                        if (success)
                        {
                            status.IsOpen = true;
                            status.Status = "Open";
                            status.Service = GetServiceForPort(port);
                        }
                        else
                        {
                            status.IsOpen = false;
                            status.Status = "Closed";
                        }
                        
                        client.EndConnect(result);
                    }
                    catch
                    {
                        status.IsOpen = false;
                        status.Status = "Closed";
                    }

                    results.Add(status);
                    PortUpdated?.Invoke(status);
                }
            });

            return results;
        }

        private string GetServiceForPort(int port)
        {
            return port switch
            {
                8080 => "Apache",
                8000 => "PHP",
                3306 => "MySQL",
                8081 => "phpMyAdmin",
                _ => "Unknown"
            };
        }

        public List<ServiceStatus> GetServices() => services.Values.ToList();
        public List<PortStatus> GetPorts() => ports.ToList();
        public List<MonitoringLogEntry> GetLogs() => logs.ToList();
        public SystemMetrics GetSystemMetrics() => systemMetrics;

        public void Dispose()
        {
            disposed = true;
            monitoringTimer?.Dispose();
            metricsTimer?.Dispose();
            logTimer?.Dispose();
        }
    }
} 