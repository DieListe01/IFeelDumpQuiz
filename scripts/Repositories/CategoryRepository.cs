using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IFeelDumpQuiz.Repositories;

public sealed class CategoryRepository
{
    public List<string> GetActiveCategoryNames()
    {
        Services.DatabaseMigrationService.EnsureInitialized();

        using var connection = Services.Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT name
FROM categories
WHERE is_active = 1
ORDER BY sort_order, name;";

        using var reader = command.ExecuteReader();
        var result = new List<string>();
        while (reader.Read())
        {
            result.Add(reader.GetString(0));
        }

        return result;
    }

    public Dictionary<string, long> ReplaceCategories(SqliteConnection connection, SqliteTransaction transaction, IEnumerable<string> categories)
    {
        var result = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        var sortOrder = 0;

        foreach (var category in categories.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x))
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
INSERT INTO categories (name, sort_order, is_active)
VALUES ($name, $sort_order, 1);
SELECT last_insert_rowid();";
            command.Parameters.AddWithValue("$name", category);
            command.Parameters.AddWithValue("$sort_order", sortOrder++);
            result[category] = (long)(command.ExecuteScalar() ?? 0L);
        }

        return result;
    }
}
