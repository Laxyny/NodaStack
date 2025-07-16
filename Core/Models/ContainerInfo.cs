using System;

namespace NodaStack;

public class ContainerInfo
{
    public string Name { get; set; } = "";
    public bool IsRunning { get; set; }
    public double CpuUsage { get; set; }
    public long MemoryUsage { get; set; }
    public string Status { get; set; } = "";
    public string Uptime { get; set; } = "";
    public string Error { get; set; } = "";

    public override bool Equals(object? obj)
    {
        if (obj is not ContainerInfo other) return false;

        return Name == other.Name &&
               IsRunning == other.IsRunning &&
               Math.Abs(CpuUsage - other.CpuUsage) < 0.5 &&
               Math.Abs(MemoryUsage - other.MemoryUsage) < 10 &&
               Status == other.Status;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, IsRunning, CpuUsage, MemoryUsage, Status);
    }
}
