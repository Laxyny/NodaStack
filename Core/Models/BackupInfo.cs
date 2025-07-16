using System;

namespace NodaStack;

public class BackupInfo
{
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public BackupType Type { get; set; }
    public long Size { get; set; }
    public string FilePath { get; set; } = "";
    public long FileSize { get; set; }
}

public enum BackupType
{
    Full,
    Configuration,
    Projects,
    Database,
    Unknown
}
