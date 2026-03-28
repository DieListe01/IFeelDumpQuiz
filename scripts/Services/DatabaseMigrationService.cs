using Godot;
using Microsoft.Data.Sqlite;

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
    FOREIGN KEY (question_id) REFERENCES questions(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_questions_category_id ON questions(category_id);
CREATE INDEX IF NOT EXISTS idx_questions_is_active ON questions(is_active);
CREATE INDEX IF NOT EXISTS idx_question_media_question_id ON question_media(question_id);";
        command.ExecuteNonQuery();

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
}
