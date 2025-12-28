using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NodaStack.Services
{
    public class ConfigurationManager
    {
        private const string CONFIG_FILE = "nodastack-config.json";
        private string configPath;

        public NodaStackConfiguration Configuration { get; private set; } = new NodaStackConfiguration();

        public ConfigurationManager()
        {
            // Use AppData/Roaming to ensure write permissions even when installed in Program Files
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configDir = Path.Combine(appData, "NodaStack");
            
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            configPath = Path.Combine(configDir, CONFIG_FILE);
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
                Console.WriteLine("Error saving configuration. Please check file permissions or disk space.");
            }
        }

        public NodaStackConfiguration GetConfiguration()
        {
            return Configuration;
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

        public void UpdateNgrokToken(string token)
        {
            Configuration.NgrokAuthToken = token;
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

}