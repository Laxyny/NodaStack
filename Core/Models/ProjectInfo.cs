using System;

namespace NodaStack;

public class ProjectInfo
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool HasIndexFile { get; set; }
    public int FileCount { get; set; }
    public DateTime LastModified { get; set; }
    public string ApacheUrl { get; set; } = string.Empty;
    public string PhpUrl { get; set; } = string.Empty;
}
