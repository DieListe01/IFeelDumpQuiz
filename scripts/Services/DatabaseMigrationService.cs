using Godot;
using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace IFeelDumpQuiz.Services;

public static class DatabaseMigrationService
{
    private static bool _initialized;

    public static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS categories (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE,
    sort_order INTEGER NOT NULL DEFAULT 0,
    is_active INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS questions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    category_id INTEGER,
    question_text TEXT NOT NULL,
    answer_a TEXT NOT NULL,
    answer_b TEXT NOT NULL,
    answer_c TEXT NOT NULL,
    answer_d TEXT NOT NULL,
    correct_answer_index INTEGER NOT NULL,
    explanation TEXT,
    difficulty INTEGER NOT NULL DEFAULT 3,
    is_active INTEGER NOT NULL DEFAULT 1,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    FOREIGN KEY (category_id) REFERENCES categories(id)
);

CREATE TABLE IF NOT EXISTS question_media (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    question_id INTEGER NOT NULL,
    media_type TEXT NOT NULL,
    stored_path TEXT NOT NULL,
    timing TEXT NOT NULL,
    sort_order INTEGER NOT NULL DEFAULT 0,
    original_filename TEXT,
    mime_type TEXT NOT NULL DEFAULT '',
    media_blob BLOB,
    FOREIGN KEY (question_id) REFERENCES questions(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_questions_category_id ON questions(category_id);
CREATE INDEX IF NOT EXISTS idx_questions_is_active ON questions(is_active);
CREATE INDEX IF NOT EXISTS idx_question_media_question_id ON question_media(question_id);";
        command.ExecuteNonQuery();

        EnsureColumnExists(connection, "questions", "difficulty", "INTEGER NOT NULL DEFAULT 3");
        EnsureColumnExists(connection, "question_media", "mime_type", "TEXT NOT NULL DEFAULT ''");
        EnsureColumnExists(connection, "question_media", "media_blob", "BLOB");
        MigrateStoredMediaToDatabase(connection);

        using var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM questions;";
        var count = (long)(countCommand.ExecuteScalar() ?? 0L);
        if (count == 0)
        {
            var imported = QuestionRepository.TryImportInitialQuestionsFromLegacyCsv(out var message);
            if (imported)
            {
                GD.Print(message);
            }
            else if (!string.IsNullOrWhiteSpace(message))
            {
                GD.Print(message);
            }
        }

        _initialized = true;
    }

    private static void MigrateStoredMediaToDatabase(SqliteConnection connection)
    {
        using var select = connection.CreateCommand();
        select.CommandText = @"
SELECT id, media_type, stored_path, COALESCE(original_filename, '')
FROM question_media
WHERE (media_blob IS NULL OR length(media_blob) = 0)
  AND COALESCE(stored_path, '') <> '';";

        using var reader = select.ExecuteReader();
        var pendingUpdates = new System.Collections.Generic.List<(long Id, string MediaType, string StoredPath, string OriginalFileName)>();
        while (reader.Read())
        {
            pendingUpdates.Add((reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)));
        }

        foreach (var item in pendingUpdates)
        {
            try
            {
                var absolutePath = ProjectSettings.GlobalizePath(item.StoredPath);
                if (!File.Exists(absolutePath))
                {
                    continue;
                }

                var imported = MediaStorageService.ImportQuestionMedia(absolutePath, item.MediaType);
                using var update = connection.CreateCommand();
                update.CommandText = @"
UPDATE question_media
SET media_blob = $media_blob,
    mime_type = $mime_type,
    original_filename = $original_filename,
    stored_path = ''
WHERE id = $id;";
                update.Parameters.AddWithValue("$id", item.Id);
                update.Parameters.Add("$media_blob", SqliteType.Blob).Value = imported.BinaryData;
                update.Parameters.AddWithValue("$mime_type", imported.MimeType);
                update.Parameters.AddWithValue("$original_filename", string.IsNullOrWhiteSpace(item.OriginalFileName) ? imported.OriginalFileName : item.OriginalFileName);
                update.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Medienmigration fehlgeschlagen fuer {item.StoredPath}: {ex.Message}");
            }
        }
    }

    private static void EnsureColumnExists(SqliteConnection connection, string tableName, string columnName, string columnDefinition)
    {
        using var pragma = connection.CreateCommand();
        pragma.CommandText = $"PRAGMA table_info({tableName});";
        using var reader = pragma.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};";
        alter.ExecuteNonQuery();
    }
}
