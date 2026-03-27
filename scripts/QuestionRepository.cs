using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.IO;

namespace IFeelDumpQuiz;

public static class QuestionRepository
{
    private const string QuestionsPath = "res://data/questions.csv";
    private const string TemplatePath = "res://data/questions_template.csv";
    private const string CsvHeader = "question;answer_a;answer_b;answer_c;answer_d;correct;category;explanation";

    public static List<QuestionData> LoadQuestions()
    {
        var loadResult = TryLoadQuestions(out var questions, out var errorMessage);
        if (!loadResult)
        {
            GD.PushError(errorMessage);
        }

        return questions;
    }

    public static bool TryLoadQuestions(out List<QuestionData> questions, out string errorMessage)
    {
        return TryLoadQuestionsFromPath(QuestionsPath, true, out questions, out errorMessage);
    }

    public static bool TryImportQuestions(string sourcePath, out int importedCount, out string message)
    {
        importedCount = 0;
        if (!TryLoadQuestionsFromPath(sourcePath, false, out var questions, out var errorMessage))
        {
            message = errorMessage;
            return false;
        }

        try
        {
            SaveQuestionsToProject(questions);
            importedCount = questions.Count;
            message = $"{importedCount} Frage(n) aus CSV importiert.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Import fehlgeschlagen: {ex.Message}";
            return false;
        }
    }

    public static bool ExportQuestions(string targetPath, string? categoryFilter, out string message)
    {
        try
        {
            var questions = LoadQuestions();
            var exportedQuestions = string.IsNullOrWhiteSpace(categoryFilter) || string.Equals(categoryFilter, "Alle", StringComparison.OrdinalIgnoreCase)
                ? questions
                : questions.Where(question => string.Equals(question.Category, categoryFilter, StringComparison.OrdinalIgnoreCase)).ToList();

            File.WriteAllText(targetPath, BuildCsv(exportedQuestions), Encoding.UTF8);
            message = string.IsNullOrWhiteSpace(categoryFilter) || string.Equals(categoryFilter, "Alle", StringComparison.OrdinalIgnoreCase)
                ? $"{exportedQuestions.Count} Frage(n) exportiert nach {targetPath}."
                : $"{exportedQuestions.Count} Frage(n) der Kategorie '{categoryFilter}' exportiert nach {targetPath}.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Export fehlgeschlagen: {ex.Message}";
            return false;
        }
    }

    public static bool ExportTemplate(string targetPath, out string message)
    {
        try
        {
            if (!Godot.FileAccess.FileExists(TemplatePath))
            {
                throw new FileNotFoundException("CSV-Vorlage wurde nicht gefunden.");
            }

            using var file = Godot.FileAccess.Open(TemplatePath, Godot.FileAccess.ModeFlags.Read);
            File.WriteAllText(targetPath, file.GetAsText(), Encoding.UTF8);
            message = $"CSV-Vorlage gespeichert nach {targetPath}.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Vorlage konnte nicht gespeichert werden: {ex.Message}";
            return false;
        }
    }

    private static bool TryLoadQuestionsFromPath(string path, bool isResourcePath, out List<QuestionData> questions, out string errorMessage)
    {
        questions = new List<QuestionData>();
        errorMessage = string.Empty;

        try
        {
            string csv;
            if (isResourcePath)
            {
                if (!Godot.FileAccess.FileExists(path))
                {
                    errorMessage = $"Fragedatei nicht gefunden: {path}";
                    return false;
                }

                using var resourceFile = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
                csv = resourceFile.GetAsText();
            }
            else
            {
                if (!File.Exists(path))
                {
                    errorMessage = $"CSV-Datei nicht gefunden: {path}";
                    return false;
                }

                csv = File.ReadAllText(path, Encoding.UTF8);
            }

            questions = ParseCsv(csv);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Fehler beim Laden der Fragen: {ex.Message}";
            return false;
        }
    }

    private static List<QuestionData> ParseCsv(string csv)
    {
        var rows = csv.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (rows.Length == 0)
        {
            return new List<QuestionData>();
        }

        var headerColumns = ParseCsvLine(rows[0]);
        if (headerColumns.Count < 8 || !string.Equals(string.Join(";", headerColumns), CsvHeader, StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException("Die CSV-Kopfzeile ist ungueltig. Erwartet wird: question;answer_a;answer_b;answer_c;answer_d;correct;category;explanation");
        }

        if (rows.Length == 1)
        {
            return new List<QuestionData>();
        }

        var questions = new List<QuestionData>();
        for (var lineIndex = 1; lineIndex < rows.Length; lineIndex++)
        {
            var columns = ParseCsvLine(rows[lineIndex]);
            if (columns.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            if (columns.Count < 8)
            {
                throw new FormatException($"CSV-Zeile {lineIndex + 1} hat zu wenige Spalten.");
            }

            ValidateQuestionRow(columns, lineIndex + 1);

            var correctIndex = int.Parse(columns[5], CultureInfo.InvariantCulture);
            if (correctIndex < 0 || correctIndex > 3)
            {
                throw new FormatException($"CSV-Zeile {lineIndex + 1} hat einen ungueltigen correctIndex. Erlaubt sind 0 bis 3.");
            }

            questions.Add(new QuestionData
            {
                Id = questions.Count + 1,
                Text = columns[0],
                Answers = new List<string> { columns[1], columns[2], columns[3], columns[4] },
                CorrectIndex = correctIndex,
                Category = columns[6],
                Explanation = columns[7]
            });
        }

        return questions;
    }

    private static void ValidateQuestionRow(IReadOnlyList<string> columns, int lineNumber)
    {
        if (string.IsNullOrWhiteSpace(columns[0]))
        {
            throw new FormatException($"CSV-Zeile {lineNumber} hat keine Frage.");
        }

        for (var i = 1; i <= 4; i++)
        {
            if (string.IsNullOrWhiteSpace(columns[i]))
            {
                throw new FormatException($"CSV-Zeile {lineNumber} hat nicht vier vollstaendige Antworten.");
            }
        }

        if (!int.TryParse(columns[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
        {
            throw new FormatException($"CSV-Zeile {lineNumber} hat einen ungueltigen correct-Wert.");
        }

        if (string.IsNullOrWhiteSpace(columns[6]))
        {
            throw new FormatException($"CSV-Zeile {lineNumber} hat keine Kategorie.");
        }
    }

    private static void SaveQuestionsToProject(List<QuestionData> questions)
    {
        using var file = Godot.FileAccess.Open(QuestionsPath, Godot.FileAccess.ModeFlags.Write);
        file.StoreString(BuildCsv(questions));
    }

    private static string BuildCsv(List<QuestionData> questions)
    {
        var builder = new StringBuilder();
        builder.AppendLine(CsvHeader);

        foreach (var question in questions)
        {
            builder.AppendLine(string.Join(";", new[]
            {
                EscapeCsvValue(question.Text),
                EscapeCsvValue(question.Answers.ElementAtOrDefault(0) ?? string.Empty),
                EscapeCsvValue(question.Answers.ElementAtOrDefault(1) ?? string.Empty),
                EscapeCsvValue(question.Answers.ElementAtOrDefault(2) ?? string.Empty),
                EscapeCsvValue(question.Answers.ElementAtOrDefault(3) ?? string.Empty),
                question.CorrectIndex.ToString(CultureInfo.InvariantCulture),
                EscapeCsvValue(question.Category),
                EscapeCsvValue(question.Explanation)
            }));
        }

        return builder.ToString();
    }

    private static string EscapeCsvValue(string value)
    {
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var character = line[i];
            if (character == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (character == ';' && !inQuotes)
            {
                values.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(character);
            }
        }

        values.Add(current.ToString().Trim());
        return values.Select(UnwrapCsvValue).ToList();
    }

    private static string UnwrapCsvValue(string value)
    {
        if (value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"'))
        {
            value = value[1..^1];
        }

        return value.Replace("\"\"", "\"");
    }
}
