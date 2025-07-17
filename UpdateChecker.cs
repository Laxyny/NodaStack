using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Net;
using System.Net.Security;
using System.Linq;

namespace NodaStack
{
    public class UpdateInfo
    {
        public string? Version { get; set; }
        public string? ReleaseNotesUrl { get; set; }
        public string? DownloadUrl { get; set; }
        public string? CurrentVersion { get; set; }
        public DateTime ReleaseDate { get; set; }
        public bool IsUpdateAvailable { get; set; }
    }

    public class UpdateChecker
    {
        private const string UpdateJsonUrl = "https://nodasys.com/Nodasys-Softwares/NodaStack/latest.json";

        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

                Debug.WriteLine("UpdateChecker: Checking for updates...");

                using var httpClient = new HttpClient(new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                });

                httpClient.DefaultRequestHeaders.Add("User-Agent", "NodaStack Update Checker");
                httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache, no-store");
                httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");

                string url = $"{UpdateJsonUrl}?t={DateTime.Now.Ticks}";

                Debug.WriteLine($"Requesting URL: {url}");
                var json = await httpClient.GetStringAsync(url);

                Debug.WriteLine($"JSON server: {json}");

                var updateInfo = JsonSerializer.Deserialize<UpdateInfo>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (updateInfo == null || string.IsNullOrEmpty(updateInfo.Version))
                {
                    Debug.WriteLine("UpdateChecker: Désérialisation JSON a échoué ou version manquante");
                    return null;
                }

                var assembly = Assembly.GetExecutingAssembly();
                var versionAttribute = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
                var currentVersion = versionAttribute?.Version ?? "0.0.0.0";
                updateInfo.CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

                Debug.WriteLine($"UpdateChecker: Version actuelle: {updateInfo.CurrentVersion}");
                Debug.WriteLine($"UpdateChecker: Version disponible: {updateInfo.Version}");

                try
                {
                    string normalizedServerVersion = updateInfo.Version;
                    string normalizedCurrentVersion = currentVersion;

                    if (updateInfo.Version.StartsWith("v"))
                    {
                        var parts = updateInfo.Version.TrimStart('v').Split('-');
                        normalizedServerVersion = parts[0];

                        if (normalizedServerVersion.Count(c => c == '.') > 2)
                        {
                            var dateComponents = normalizedServerVersion.Split('.');
                            if (dateComponents.Length >= 3)
                            {
                                normalizedServerVersion = string.Join(".", dateComponents.Take(3));
                            }
                        }
                    }

                    var serverVersion = new Version(updateInfo.Version.TrimStart('v').Split('-')[0]);
                    var appVersion = new Version(updateInfo.CurrentVersion);
                    updateInfo.IsUpdateAvailable = serverVersion > appVersion;

                    Debug.WriteLine($"UpdateChecker: Version serveur normalisée: {normalizedServerVersion}");
                    Debug.WriteLine($"UpdateChecker: Version application normalisée: {normalizedCurrentVersion}");
                    Debug.WriteLine($"UpdateChecker: Mise à jour disponible: {updateInfo.IsUpdateAvailable}");

                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error comparing versions: {ex.Message}");
                    updateInfo.IsUpdateAvailable = false;
                }

                return updateInfo;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking for updates: {ex.Message}");
                return null;
            }
        }
    }
}