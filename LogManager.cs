using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NodaStack
{
    public class LogManager
    {
        private readonly string logDirectory;
        private readonly List<LogEntry> logEntries = new();
        private readonly object lockObject = new();

        public event Action<LogEntry>? OnLogAdded;

        public LogManager()
        {
            logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            Directory.CreateDirectory(logDirectory);
        }

        public void Log(string message, LogLevel level = LogLevel.Info, string service = "System")
        {
            try
            {
                var entry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Message = message ?? "",
                    Level = level,
                    Service = service ?? "System"
                };

                lock (lockObject)
                {
                    logEntries.Add(entry);

                    if (logEntries.Count > 1000)
                    {
                        logEntries.RemoveRange(0, 100);
                    }
                }

                OnLogAdded?.Invoke(entry);

                Task.Run(() => WriteToFile(entry));
            }
            catch
            {
                // Ignore logging errors
            }
        }

        private void WriteToFile(LogEntry entry)
        {
            try
            {
                var fileName = $"nodastack_{DateTime.Now:yyyy-MM-dd}.log";
                var filePath = Path.Combine(logDirectory, fileName);

                var logLine = $"[{entry.Timestamp:HH:mm:ss}] [{entry.Level}] [{entry.Service}] {entry.Message}";

                File.AppendAllText(filePath, logLine + Environment.NewLine);
            }
            catch
            {
                // Ignore file write errors
            }
        }

        public List<LogEntry> GetLogs(LogLevel? filterLevel = null, string? filterService = null)
        {
            try
            {
                lock (lockObject)
                {
                    return logEntries.Where(entry =>
                        (filterLevel == null || entry.Level == filterLevel) &&
                        (filterService == null || entry.Service.Equals(filterService, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }
            }
            catch
            {
                return new List<LogEntry>();
            }
        }

        public void ClearLogs()
        {
            try
            {
                lock (lockObject)
                {
                    logEntries.Clear();
                }
            }
            catch
            {
                // Ignore clear errors
            }
        }

        public void CleanupOldLogs(int retentionDays = 7)
        {
            Task.Run(() =>
            {
                try
                {
                    var cutoffDate = DateTime.Now.AddDays(-retentionDays);
                    var files = Directory.GetFiles(logDirectory, "nodastack_*.log");

                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.CreationTime < cutoffDate)
                        {
                            File.Delete(file);
                        }
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            });
        }
    }

}