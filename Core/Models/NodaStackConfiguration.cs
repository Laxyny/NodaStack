using System.Collections.Generic;

namespace NodaStack;

public class NodaStackConfiguration
{
    public Dictionary<string, int> Ports { get; set; } = new Dictionary<string, int>
    {
        { "Apache", 8080 },
        { "PHP", 8000 },
        { "MySQL", 3306 },
        { "phpMyAdmin", 8081 }
    };

    public Dictionary<string, string> Versions { get; set; } = new Dictionary<string, string>
    {
        { "PHP", "8.2" },
        { "MySQL", "8.0" },
        { "Apache", "2.4" }
    };

    public NodaStackSettings Settings { get; set; } = new NodaStackSettings();
    public string NgrokAuthToken { get; set; } = "";

    public int ApachePort => Ports.TryGetValue("Apache", out var port) ? port : 8080;
    public int PhpPort => Ports.TryGetValue("PHP", out var port) ? port : 8000;
    public int MySqlPort => Ports.TryGetValue("MySQL", out var port) ? port : 3306;
    public int PhpMyAdminPort => Ports.TryGetValue("phpMyAdmin", out var port) ? port : 8081;
}

public class NodaStackSettings
{
    public bool AutoStartServices { get; set; } = false;
    public bool ShowNotifications { get; set; } = true;
    public bool EnableLogging { get; set; } = true;
    public bool DarkMode { get; set; } = false;
    public string DefaultBrowser { get; set; } = "default";
    public bool AutoRefreshProjects { get; set; } = true;
    public int LogRetentionDays { get; set; } = 7;
    public string ProjectsPath { get; set; } = "www";
    public bool EnableSsl { get; set; } = false;
    public string MySqlPassword { get; set; } = "";
    public string MySqlDefaultDatabase { get; set; } = "nodastack";
    public bool AutoCheckUpdates { get; set; } = true;
    public bool AutoInstallUpdates { get; set; } = false;
    public string Language { get; set; } = "en";
}
