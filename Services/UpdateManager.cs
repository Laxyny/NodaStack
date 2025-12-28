using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace NodaStack.Services
{
    public class UpdateInfo
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "";

        [JsonPropertyName("releaseDate")]
        public string ReleaseDate { get; set; } = "";

        [JsonPropertyName("releaseNotesUrl")]
        public string ReleaseNotesUrl { get; set; } = "";

        [JsonPropertyName("downloadUrl")]
        public string DownloadUrl { get; set; } = "";
    }

    public class UpdateManager
    {
        // TODO: REMPLACEZ CECI PAR L'URL RÉELLE DE VOTRE VPS
        // Exemple : https://updates.nodasys.com/NodaStack/latest.json
        private const string UPDATE_FEED_URL = "https://nodasys.com/Nodasys-Softwares/NodaStack/latest.json";
        
        private readonly HttpClient _httpClient;

        public UpdateManager()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "NodaStack-Updater");
        }

        public async Task<(bool hasUpdate, UpdateInfo? info)> CheckForUpdatesAsync()
        {
            try
            {
                var json = await _httpClient.GetStringAsync(UPDATE_FEED_URL);
                var updateInfo = JsonSerializer.Deserialize<UpdateInfo>(json);

                if (updateInfo != null && Version.TryParse(updateInfo.Version, out var remoteVersion))
                {
                    var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                    
                    // Compare : Remote > Current
                    if (remoteVersion > currentVersion)
                    {
                        return (true, updateInfo);
                    }
                }
                
                return (false, updateInfo);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update check failed: {ex.Message}");
                return (false, null);
            }
        }

        public async Task DownloadAndInstallAsync(UpdateInfo info, IProgress<double> progress)
        {
            string tempFile = Path.Combine(Path.GetTempPath(), "NodaStack-Setup.exe");

            try
            {
                using (var response = await _httpClient.GetAsync(info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var canReportProgress = totalBytes != -1 && progress != null;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        long totalRead = 0;
                        int read;

                        while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, read);
                            totalRead += read;

                            if (canReportProgress)
                            {
                                progress.Report((double)totalRead / totalBytes * 100);
                            }
                        }
                    }
                }

                // Run the installer
                StartInstaller(tempFile);
            }
            catch (Exception ex)
            {
                throw new Exception($"Update failed: {ex.Message}");
            }
        }

        private void StartInstaller(string installerPath)
        {
            try
            {
                // Run the installer
                var psi = new ProcessStartInfo
                {
                    FileName = installerPath,
                    // Arguments = "/S", // /S pour installation silencieuse (si NSIS est configuré ainsi)
                    UseShellExecute = true
                };

                Process.Start(psi);

                // Close current app
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch installer: {ex.Message}", "Update Error");
            }
        }
    }
}

