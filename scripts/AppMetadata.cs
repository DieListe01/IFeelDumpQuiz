using System;
using System.IO;

namespace IFeelDumpQuiz;

public static class AppMetadata
{
    public static readonly string Version = LoadVersion();
    public static readonly string BaseDirectory = AppContext.BaseDirectory;
    public const string GitHubOwner = "DieListe01";
    public const string GitHubRepo = "IFeelDumpQuiz";
    public const string ReleaseApiUrl = "https://api.github.com/repos/DieListe01/IFeelDumpQuiz/releases/latest";
    public const string WindowsZipAssetName = "IFeelDumpQuiz-win64.zip";
    public const string WindowsExecutableName = "IFeelDumpQuiz.exe";

    public static bool IsEditorBuild => BaseDirectory.Contains(".godot", StringComparison.OrdinalIgnoreCase);

    public static bool IsPackagedBuild
    {
        get
        {
            if (IsEditorBuild)
            {
                return false;
            }

            try
            {
                return File.Exists(Path.Combine(BaseDirectory, WindowsExecutableName))
                    && File.Exists(Path.Combine(BaseDirectory, "VERSION"));
            }
            catch
            {
                return false;
            }
        }
    }

    private static string LoadVersion()
    {
        try
        {
            var versionPath = Path.Combine(AppContext.BaseDirectory, "VERSION");
            if (File.Exists(versionPath))
            {
                var value = File.ReadAllText(versionPath).Trim();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }
        catch
        {
        }

        return "0.1.0";
    }
}
