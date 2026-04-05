using IFeelDumpQuiz.Services;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace IFeelDumpQuiz.Repositories;

public sealed class QuestionDataRepository
{
    private readonly QuestionMediaRepository _mediaRepository = new();
    private readonly CategoryRepository _categoryRepository = new();

    public List<QuestionData> LoadActiveQuestions()
    {
        DatabaseMigrationService.EnsureInitialized();

        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT
    q.id,
    COALESCE(c.name, 'Allgemein'),
    q.question_text,
    q.answer_a,
    q.answer_b,
    q.answer_c,
    q.answer_d,
    q.correct_answer_index,
    COALESCE(q.explanation, ''),
    q.difficulty,
    qm.id,
    COALESCE(qm.media_type, ''),
    COALESCE(qm.timing, ''),
    COALESCE(qm.stored_path, ''),
    COALESCE(qm.original_filename, ''),
    COALESCE(qm.mime_type, ''),
    qm.media_blob
FROM questions q
LEFT JOIN categories c ON c.id = q.category_id
LEFT JOIN question_media qm ON qm.question_id = q.id
WHERE q.is_active = 1
ORDER BY c.sort_order, c.name, q.id, qm.sort_order, qm.id;";

        using var reader = command.ExecuteReader();
        var questions = new List<QuestionData>();
        var byId = new Dictionary<int, QuestionData>();

        while (reader.Read())
        {
            var id = reader.GetInt32(0);
            if (!byId.TryGetValue(id, out var question))
            {
                question = new QuestionData
                {
                    Id = id,
                    Category = reader.GetString(1),
                    Text = reader.GetString(2),
                    Answers = new List<string> { reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6) },
                    CorrectIndex = reader.GetInt32(7),
                    Explanation = reader.GetString(8),
                    Difficulty = Math.Clamp(reader.GetInt32(9), 1, 5)
                };
                byId[id] = question;
                questions.Add(question);
            }

            if (!reader.IsDBNull(10))
            {
                question.Media.Add(new QuestionMediaData
                {
                    Id = reader.GetInt32(10),
                    QuestionId = id,
                    MediaType = reader.GetString(11),
                    Timing = reader.GetString(12),
                    StoredPath = reader.GetString(13),
                    OriginalFileName = reader.GetString(14),
                    MimeType = reader.GetString(15),
                    BinaryData = reader.IsDBNull(16) ? Array.Empty<byte>() : (byte[])reader[16]
                });
            }
        }

        return questions;
    }

    public bool ReplaceQuestions(List<QuestionData> questions, out string message)
    {
        DatabaseMigrationService.EnsureInitialized();

        try
        {
            using var connection = Database.OpenConnection();
            using var transaction = connection.BeginTransaction();

            foreach (var sql in new[] { "DELETE FROM question_media;", "DELETE FROM questions;", "DELETE FROM categories;" })
            {
                using var deleteCommand = connection.CreateCommand();
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText = sql;
                deleteCommand.ExecuteNonQuery();
            }

            var categoryIds = _categoryRepository.ReplaceCategories(connection, transaction, questions.Select(q => q.Category));

            foreach (var question in questions)
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
INSERT INTO questions (
    category_id, question_text, answer_a, answer_b, answer_c, answer_d,
    correct_answer_index, explanation, difficulty, is_active, created_at, updated_at)
VALUES (
    $category_id, $question_text, $answer_a, $answer_b, $answer_c, $answer_d,
    $correct_answer_index, $explanation, $difficulty, 1, $created_at, $updated_at);
SELECT last_insert_rowid();";
                command.Parameters.AddWithValue("$category_id", categoryIds[question.Category]);
                command.Parameters.AddWithValue("$question_text", question.Text);
                command.Parameters.AddWithValue("$answer_a", question.Answers.ElementAtOrDefault(0) ?? string.Empty);
                command.Parameters.AddWithValue("$answer_b", question.Answers.ElementAtOrDefault(1) ?? string.Empty);
                command.Parameters.AddWithValue("$answer_c", question.Answers.ElementAtOrDefault(2) ?? string.Empty);
                command.Parameters.AddWithValue("$answer_d", question.Answers.ElementAtOrDefault(3) ?? string.Empty);
                command.Parameters.AddWithValue("$correct_answer_index", question.CorrectIndex);
                command.Parameters.AddWithValue("$explanation", question.Explanation);
                command.Parameters.AddWithValue("$difficulty", Math.Clamp(question.Difficulty, 1, 5));
                command.Parameters.AddWithValue("$created_at", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$updated_at", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
                var questionId = (long)(command.ExecuteScalar() ?? 0L);

                _mediaRepository.InsertForQuestion(connection, transaction, questionId, question.Media);
            }

            transaction.Commit();
            message = $"{questions.Count} Frage(n) in der Datenbank gespeichert.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Speichern in SQLite fehlgeschlagen: {ex.Message}";
            return false;
        }
    }
}
