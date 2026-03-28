using Godot;
using Microsoft.Data.Sqlite;
using System.IO;

namespace IFeelDumpQuiz.Services;

public static class Database
{
    public static string DbPath => ProjectSettings.GlobalizePath("user://data/quiz.db");

    public static SqliteConnection OpenConnection()
    {
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
}
