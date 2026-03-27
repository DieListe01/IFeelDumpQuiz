using Godot;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace IFeelDumpQuiz.Services;

public sealed class UpdateService
{
    private static readonly System.Net.Http.HttpClient Client = CreateClient();

    public async Task<UpdateInfo?> CheckForUpdateAsync()
    {
        using var response = await Client.GetAsync(AppMetadata.ReleaseApiUrl);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var root = document.RootElement;

        var tag = root.GetProperty("tag_name").GetString() ?? string.Empty;
        var normalizedTag = tag.Trim().TrimStart('v', 'V');
        if (!Version.TryParse(normalizedTag, out var latestVersion) || !Version.TryParse(AppMetadata.Version, out var currentVersion))
        {
            return null;
        }

        if (latestVersion <= currentVersion)
        {
            return null;
        }

        foreach (var asset in root.GetProperty("assets").EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString() ?? string.Empty;
            if (!string.Equals(name, AppMetadata.WindowsZipAssetName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return new UpdateInfo
            {
                Version = normalizedTag,
                DownloadUrl = asset.GetProperty("browser_download_url").GetString() ?? string.Empty,
                Notes = root.TryGetProperty("body", out var body) ? body.GetString() ?? string.Empty : string.Empty
            };
        }

        return null;
    }

    public async Task<string> DownloadUpdateAsync(UpdateInfo updateInfo)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "IFeelDumpQuiz", updateInfo.Version);
        Directory.CreateDirectory(tempDir);
        var zipPath = Path.Combine(tempDir, AppMetadata.WindowsZipAssetName);

        await using var source = await Client.GetStreamAsync(updateInfo.DownloadUrl);
        await using var target = File.Create(zipPath);
        await source.CopyToAsync(target);
        return zipPath;
    }

    public void StartWindowsUpdater(string zipPath)
    {
        var installDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var exePath = Path.Combine(installDir, AppMetadata.WindowsExecutableName);
        var scriptPath = Path.Combine(Path.GetTempPath(), "IFeelDumpQuiz", "run-update.ps1");
        Directory.CreateDirectory(Path.GetDirectoryName(scriptPath)!);
        File.WriteAllText(scriptPath, BuildUpdaterScript());

        var pid = System.Environment.ProcessId;
        OS.CreateProcess("powershell", new[]
        {
            "-ExecutionPolicy",
            "Bypass",
            "-File",
            scriptPath,
            "-ZipPath",
            zipPath,
            "-InstallDir",
            installDir,
            "-ExecutablePath",
            exePath,
            "-ProcessId",
            pid.ToString()
        });
    }

    private static System.Net.Http.HttpClient CreateClient()
    {
        var client = new System.Net.Http.HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("IFeelDumpQuiz", AppMetadata.Version));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }

    private static string BuildUpdaterScript()
    {
        return "param([string]$ZipPath,[string]$InstallDir,[string]$ExecutablePath,[int]$ProcessId)\n" +
               "$ErrorActionPreference = 'Stop'\n" +
               "try { Wait-Process -Id $ProcessId -Timeout 30 -ErrorAction SilentlyContinue } catch {}\n" +
               "$extractDir = Join-Path ([System.IO.Path]::GetDirectoryName($ZipPath)) 'extracted'\n" +
               "if (Test-Path $extractDir) { Remove-Item $extractDir -Recurse -Force }\n" +
               "Expand-Archive -Path $ZipPath -DestinationPath $extractDir -Force\n" +
               "$sourceDir = $extractDir\n" +
               "$entries = Get-ChildItem -Path $extractDir\n" +
               "if ($entries.Count -eq 1 -and $entries[0].PSIsContainer) { $sourceDir = $entries[0].FullName }\n" +
               "Copy-Item -Path (Join-Path $sourceDir '*') -Destination $InstallDir -Recurse -Force\n" +
               "Start-Process -FilePath $ExecutablePath\n";
    }
}

public sealed class UpdateInfo
{
    public string Version { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
