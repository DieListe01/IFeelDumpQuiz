using System;
using System.IO;

namespace IFeelDumpQuiz;

public static class AppMetadata
{
    public static readonly string BaseDirectory = ResolveBaseDirectory();
    public static readonly string InstallDataDirectory = Path.Combine(BaseDirectory, "data");
    public static readonly string Version = LoadVersion();
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

    public static string GetDataDirectory()
    {
        if (IsPackagedBuild)
        {
            Directory.CreateDirectory(InstallDataDirectory);
            return InstallDataDirectory;
        }

        var userDataDirectory = Godot.ProjectSettings.GlobalizePath("user://data");
        Directory.CreateDirectory(userDataDirectory);
        return userDataDirectory;
    }

    private static string LoadVersion()
    {
        try
        {
            var versionPath = Path.Combine(BaseDirectory, "VERSION");
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

    private static string ResolveBaseDirectory()
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDirectory != null)
        {
            var exePath = Path.Combine(currentDirectory.FullName, WindowsExecutableName);
            var versionPath = Path.Combine(currentDirectory.FullName, "VERSION");
            if (File.Exists(exePath) && File.Exists(versionPath))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        return AppContext.BaseDirectory;
    }
}
