using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
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
                Debug.WriteLine("UpdateChecker: Checking for updates...");

                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                    SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
                };

                using var httpClient = new HttpClient(handler);

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
                updateInfo.CurrentVersion = versionAttribute?.Version ?? "0.0.0.0";

                Debug.WriteLine($"UpdateChecker: Version actuelle: {updateInfo.CurrentVersion}");
                Debug.WriteLine($"UpdateChecker: Version disponible: {updateInfo.Version}");

                try
                {
                    string serverVersionStr = updateInfo.Version.TrimStart('v').Split('-')[0];
                    string currentVersionStr = updateInfo.CurrentVersion.TrimStart('v').Split('-')[0];

                    var serverParts = serverVersionStr.Split('.').Select(int.Parse).ToArray();
                    var currentParts = currentVersionStr.Split('.').Select(int.Parse).ToArray();

                    bool isUpdateAvailable = false;
                    int minLength = Math.Min(serverParts.Length, currentParts.Length);

                    for (int i = 0; i < minLength; i++)
                    {
                        if (serverParts[i] > currentParts[i])
                        {
                            isUpdateAvailable = true;
                            break;
                        }
                        if (serverParts[i] < currentParts[i])
                        {
                            break;
                        }
                    }
                    
                    if (!isUpdateAvailable && serverParts.Length > currentParts.Length)
                    {
                        isUpdateAvailable = true;
                    }

                    updateInfo.IsUpdateAvailable = isUpdateAvailable;

                    Debug.WriteLine($"UpdateChecker: Version serveur normalisée: {serverVersionStr}");
                    Debug.WriteLine($"UpdateChecker: Version application normalisée: {currentVersionStr}");
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