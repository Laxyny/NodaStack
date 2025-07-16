using System;

namespace NodaStack;

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = "";
    public LogLevel Level { get; set; }
    public string Service { get; set; } = "";
}

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}
