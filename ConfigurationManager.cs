using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NodaStack
{
    public class ConfigurationManager
    {
        private const string CONFIG_FILE = "nodastack-config.json";
        private string configPath;

        public NodaStackConfiguration Configuration { get; private set; } = new NodaStackConfiguration();

        public ConfigurationManager()
        {
            configPath = Path.Combine(Directory.GetCurrentDirectory(), CONFIG_FILE);
            LoadConfiguration();
        }

        public void LoadConfiguration()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    var jsonContent = File.ReadAllText(configPath);
                    Configuration = JsonSerializer.Deserialize<NodaStackConfiguration>(jsonContent) ?? new NodaStackConfiguration();
                }
                else
                {
                    Configuration = new NodaStackConfiguration();
                    SaveConfiguration();
                }
            }
            catch (Exception)
            {
                Configuration = new NodaStackConfiguration();
                SaveConfiguration();
            }
        }

        public void SaveConfiguration()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var jsonContent = JsonSerializer.Serialize(Configuration, options);
                File.WriteAllText(configPath, jsonContent);
            }
            catch (Exception)
            {
                // Handle exceptions related to file writing, e.g., log the error or notify the user
                Console.WriteLine("Error saving configuration. Please check file permissions or disk space.");
            }
        }

        public void UpdatePorts(Dictionary<string, int> newPorts)
        {
            foreach (var kvp in newPorts)
            {
                if (Configuration.Ports.ContainsKey(kvp.Key))
                {
                    Configuration.Ports[kvp.Key] = kvp.Value;
                }
            }
            SaveConfiguration();
        }

        public void UpdateVersions(Dictionary<string, string> newVersions)
        {
            foreach (var kvp in newVersions)
            {
                if (Configuration.Versions.ContainsKey(kvp.Key))
                {
                    Configuration.Versions[kvp.Key] = kvp.Value;
                }
            }
            SaveConfiguration();
        }

        public void UpdateSettings(NodaStackSettings newSettings)
        {
            Configuration.Settings = newSettings;
            SaveConfiguration();
        }

        public void ResetToDefaults()
        {
            Configuration = new NodaStackConfiguration();
            SaveConfiguration();
        }

        public bool IsPortAvailable(int port)
        {
            try
            {
                using (var tcpClient = new System.Net.Sockets.TcpClient())
                {
                    tcpClient.Connect("127.0.0.1", port);
                    return false;
                }
            }
            catch (System.Net.Sockets.SocketException)
            {
                return true;
            }
        }

        public List<int> GetAvailablePorts(int startPort = 8000, int endPort = 9000)
        {
            var availablePorts = new List<int>();
            
            for (int port = startPort; port <= endPort; port++)
            {
                if (IsPortAvailable(port))
                {
                    availablePorts.Add(port);
                }
            }
            
            return availablePorts;
        }
    }

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
        public string Language { get; set; } = "en";
    }
}