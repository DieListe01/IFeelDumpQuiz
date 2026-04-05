using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;

namespace IFeelDumpQuiz.Repositories;

public sealed class QuestionMediaRepository
{
    public void InsertForQuestion(SqliteConnection connection, SqliteTransaction transaction, long questionId, IEnumerable<QuestionMediaData> mediaItems)
    {
        foreach (var media in mediaItems.OrderBy(m => m.Id))
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
INSERT INTO question_media (question_id, media_type, stored_path, timing, sort_order, original_filename, mime_type, media_blob)
VALUES ($question_id, $media_type, $stored_path, $timing, $sort_order, $original_filename, $mime_type, $media_blob);";
            command.Parameters.AddWithValue("$question_id", questionId);
            command.Parameters.AddWithValue("$media_type", media.MediaType);
            command.Parameters.AddWithValue("$stored_path", media.StoredPath);
            command.Parameters.AddWithValue("$timing", media.Timing);
            command.Parameters.AddWithValue("$sort_order", 0);
            command.Parameters.AddWithValue("$original_filename", media.OriginalFileName);
            command.Parameters.AddWithValue("$mime_type", media.MimeType);
            command.Parameters.Add("$media_blob", SqliteType.Blob).Value = media.BinaryData;
            command.ExecuteNonQuery();
        }
    }
}
