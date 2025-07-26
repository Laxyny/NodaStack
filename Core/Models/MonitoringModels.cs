using System;
using System.Collections.Generic;

namespace NodaStack.Core.Models
{
    public class ServiceStatus
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "Stopped";
        public double CpuUsage { get; set; }
        public long MemoryUsage { get; set; }
        public string Uptime { get; set; } = "00:00:00";
        public string Version { get; set; } = string.Empty;
        public string Port { get; set; } = string.Empty;
        public bool IsHealthy { get; set; }
        public string LastError { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public ServiceMetrics Metrics { get; set; } = new();
    }

    public class ServiceMetrics
    {
        public double CpuAverage { get; set; }
        public long MemoryAverage { get; set; }
        public int RequestCount { get; set; }
        public double ResponseTime { get; set; }
        public int ErrorCount { get; set; }
        public List<MetricPoint> History { get; set; } = new();
    }

    public class MetricPoint
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    public class PortStatus
    {
        public int Port { get; set; }
        public string Status { get; set; } = "Unknown";
        public string Service { get; set; } = string.Empty;
        public bool IsOpen { get; set; }
        public string Protocol { get; set; } = "TCP";
        public DateTime LastChecked { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class SystemMetrics
    {
        public double TotalCpuUsage { get; set; }
        public long TotalMemoryUsage { get; set; }
        public long AvailableMemory { get; set; }
        public long TotalDiskSpace { get; set; }
        public long AvailableDiskSpace { get; set; }
        public int ActiveConnections { get; set; }
        public double NetworkIn { get; set; }
        public double NetworkOut { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class MonitoringLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = "Info";
        public string Service { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    public class MonitoringConfig
    {
        public int RefreshInterval { get; set; } = 3000;
        public bool AutoRefresh { get; set; } = true;
        public bool ShowSystemMetrics { get; set; } = true;
        public bool ShowDetailedLogs { get; set; } = true;
        public int MaxLogEntries { get; set; } = 1000;
        public List<string> MonitoredServices { get; set; } = new();
        public List<int> MonitoredPorts { get; set; } = new();
    }
} 