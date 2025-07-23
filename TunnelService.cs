using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NodaStack
{
    public class TunnelService
    {
        private readonly string _authToken;
        private readonly LogManager? _logManager;
        private const string ContainerName = "nodastack_ngrok";

        public TunnelService(string authToken, LogManager? logManager = null)
        {
            _authToken = authToken;
            _logManager = logManager;
        }

        private void Log(string message, LogLevel level = LogLevel.Info)
        {
            Debug.WriteLine($"TunnelService: {message}");
            _logManager?.Log(message, level, "Ngrok");
        }

        public async Task<string> StartTunnelAsync(string name, int port, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(_authToken))
            {
                Log("Aucun token d'authentification ngrok configuré", LogLevel.Error);
                throw new InvalidOperationException("Token d'authentification ngrok requis. Veuillez configurer votre token dans les paramètres.");
            }

            Log($"Démarrage d'un tunnel pour {name} sur le port {port}", LogLevel.Info);

            try
            {
                Log("Suppression des anciens conteneurs ngrok", LogLevel.Info);
                await Process
                    .Start(new ProcessStartInfo("docker", $"rm -f {ContainerName}")
                    {
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    })!
                    .WaitForExitAsync(ct);

                Log("Vérification de l'image ngrok", LogLevel.Info);
                var pullProcess = Process.Start(new ProcessStartInfo("docker", "pull ngrok/ngrok")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                await pullProcess!.WaitForExitAsync(ct);

                Log("Lancement du conteneur ngrok", LogLevel.Info);
                var runArgs = $"run -d --rm --name {ContainerName} -p 4040:4040 " +
                  $"-e NGROK_AUTHTOKEN={_authToken} ngrok/ngrok http " +
                  $"--region=eu --host-header=rewrite localhost:{port}";
                using var runProc = Process.Start(new ProcessStartInfo("docker", runArgs)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (runProc == null)
                {
                    Log("Impossible de lancer le processus docker", LogLevel.Error);
                    throw new InvalidOperationException("Impossible de lancer le conteneur ngrok.");
                }

                var containerId = (await runProc.StandardOutput.ReadLineAsync())?.Trim();
                await runProc.WaitForExitAsync(ct);
                if (runProc.ExitCode != 0 || string.IsNullOrEmpty(containerId))
                {
                    var err = await runProc.StandardError.ReadToEndAsync();
                    Log($"Échec de docker run: {err.Trim()}", LogLevel.Error);
                    throw new InvalidOperationException("Échec de docker run: " + err.Trim());
                }

                Log($"Conteneur ngrok démarré avec l'ID: {containerId}", LogLevel.Info);

                using var psProc = Process.Start(new ProcessStartInfo("docker", $"ps --filter name={ContainerName} --format \"{{{{.Names}}}}\"")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                })!;
                var runningName = (await psProc.StandardOutput.ReadToEndAsync()).Trim();
                await psProc.WaitForExitAsync(ct);
                if (string.Compare(runningName, ContainerName, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    Log("Le conteneur ngrok ne tourne pas après création", LogLevel.Error);
                    throw new InvalidOperationException("Le conteneur ngrok ne tourne pas après création.");
                }

                Log("Attente de l'initialisation de l'API ngrok", LogLevel.Info);
                await Task.Delay(TimeSpan.FromSeconds(5), ct);

                const int maxAttempts = 8;
                var apiUrl = "http://127.0.0.1:4040/api/tunnels";
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    try
                    {
                        Log($"Tentative de connexion à l'API ngrok ({attempt + 1}/{maxAttempts})", LogLevel.Info);
                        var json = await client.GetStringAsync(apiUrl, ct);

                        Log("Réponse API reçue, analyse du JSON", LogLevel.Info);
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("tunnels", out var tunnels) &&
                            tunnels.ValueKind == JsonValueKind.Array &&
                            tunnels.GetArrayLength() > 0)
                        {
                            var first = tunnels[0];
                            if (first.TryGetProperty("public_url", out var urlProp) &&
                                !string.IsNullOrEmpty(urlProp.GetString()))
                            {
                                string url = urlProp.GetString()!;
                                Log($"Tunnel créé avec succès: {url}", LogLevel.Info);
                                return url;
                            }
                            Log("public_url manquante ou vide dans la réponse", LogLevel.Error);
                            throw new InvalidOperationException("public_url manquante ou vide.");
                        }

                        Log("Aucun tunnel disponible dans la réponse ngrok", LogLevel.Error);
                        throw new InvalidOperationException("Aucun tunnel disponible dans la réponse ngrok.");
                    }
                    catch (Exception ex) when (attempt + 1 < maxAttempts)
                    {
                        Log($"Tentative #{attempt + 1} échouée: {ex.Message}", LogLevel.Warning);
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt + 1)), ct);
                    }
                }

                Log($"Impossible de joindre l'API ngrok après {maxAttempts} tentatives", LogLevel.Error);
                throw new InvalidOperationException($"Impossible de joindre l'API ngrok après {maxAttempts} tentatives.");
            }
            catch (Exception ex)
            {
                Log($"Erreur lors de la création du tunnel: {ex.Message}", LogLevel.Error);
                StopTunnel();
                throw;
            }
        }

        public void StopTunnel()
        {
            try
            {
                Log("Arrêt du tunnel ngrok", LogLevel.Info);
                Process.Start(new ProcessStartInfo("docker", $"stop {ContainerName}")
                {
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch (Exception ex)
            {
                Log($"Erreur lors de l'arrêt du tunnel: {ex.Message}", LogLevel.Warning);
            }
        }

        public async Task<bool> TestNgrokAuthTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            try
            {
                var testProcess = Process.Start(new ProcessStartInfo("docker", $"run --rm -e NGROK_AUTHTOKEN={token} ngrok/ngrok config check")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (testProcess == null)
                    return false;

                await testProcess.WaitForExitAsync();
                var output = await testProcess.StandardOutput.ReadToEndAsync();
                var error = await testProcess.StandardError.ReadToEndAsync();

                return testProcess.ExitCode == 0 && !error.Contains("invalid token");
            }
            catch
            {
                return false;
            }
        }
    }
}