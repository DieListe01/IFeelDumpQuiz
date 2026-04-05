using Godot;
using Microsoft.Data.Sqlite;
using System.IO;

namespace IFeelDumpQuiz.Services;

public static class Database
{
    public static string DbPath => Path.Combine(AppMetadata.GetDataDirectory(), "quiz.db");

    public static SqliteConnection OpenConnection()
    {
        MigrateLegacyDatabaseIfNeeded();

        var directory = Path.GetDirectoryName(DbPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var connection = new SqliteConnection($"Data Source={DbPath}");
        connection.Open();

        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();

        return connection;
    }

    private static void MigrateLegacyDatabaseIfNeeded()
    {
        if (!AppMetadata.IsPackagedBuild || File.Exists(DbPath))
        {
            return;
        }

        var legacyPath = ProjectSettings.GlobalizePath("user://data/quiz.db");
        if (!File.Exists(legacyPath))
        {
            return;
        }

        var directory = Path.GetDirectoryName(DbPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.Copy(legacyPath, DbPath, true);
    }
}
