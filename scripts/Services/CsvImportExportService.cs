using Godot;
using IFeelDumpQuiz.Repositories;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace IFeelDumpQuiz.Services;

public static class CsvImportExportService
{
    private const string ResourceQuestionsPath = "res://data/questions.csv";
    private const string ResourceSeedQuestionsPath = "res://data/questions_seed.txt";
    private const string UserQuestionsPath = "user://data/questions.csv";
    private const string CsvHeader = "question;answer_a;answer_b;answer_c;answer_d;correct;category;explanation;difficulty";

    public static bool TryImportQuestions(string sourcePath, out int importedCount, out string message)
    {
        return TryImportQuestions(sourcePath, true, out importedCount, out _, out message);
    }

    public static bool TryImportQuestions(string sourcePath, bool replaceExisting, out int importedCount, out int skippedCount, out string message)
    {
        importedCount = 0;
        skippedCount = 0;
        if (!TryLoadQuestionsFromAbsolutePath(sourcePath, out var questions, out var errorMessage))
        {
            message = errorMessage;
            return false;
        }

        var repository = new QuestionDataRepository();
        List<QuestionData> questionsToStore;
        if (replaceExisting)
        {
            questionsToStore = questions;
        }
        else
        {
            var existingQuestions = repository.LoadActiveQuestions();
            questionsToStore = MergeQuestions(existingQuestions, questions, out skippedCount);
        }

        if (!repository.ReplaceQuestions(questionsToStore, out message))
        {
            return false;
        }

        importedCount = questions.Count - skippedCount;
        message = replaceExisting
            ? $"{importedCount} Frage(n) aus CSV importiert und Bestand ersetzt."
            : $"{importedCount} Frage(n) importiert, {skippedCount} Dublette(n) uebersprungen.";
        return true;
    }

    public static bool ExportQuestions(string targetPath, string? categoryFilter, out string message)
    {
        try
        {
            var repository = new QuestionDataRepository();
            var questions = repository.LoadActiveQuestions();
            var exportedQuestions = string.IsNullOrWhiteSpace(categoryFilter) || string.Equals(categoryFilter, "Alle", StringComparison.OrdinalIgnoreCase)
                ? questions
                : questions.Where(question => string.Equals(question.Category, categoryFilter, StringComparison.OrdinalIgnoreCase)).ToList();

            File.WriteAllText(targetPath, BuildCsv(exportedQuestions), Encoding.UTF8);
            message = $"{exportedQuestions.Count} Frage(n) exportiert nach {targetPath}.";
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
            File.WriteAllText(targetPath, CsvHeader + System.Environment.NewLine, Encoding.UTF8);
            message = $"CSV-Vorlage gespeichert nach {targetPath}.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Vorlage konnte nicht gespeichert werden: {ex.Message}";
            return false;
        }
    }

    public static bool TryImportInitialQuestionsFromLegacyCsv(out string message)
    {
        if (TryLoadQuestionsFromGodotPath(UserQuestionsPath, out var userQuestions, out _) && userQuestions.Count > 0)
        {
            return new QuestionDataRepository().ReplaceQuestions(userQuestions, out message);
        }

        if (TryLoadQuestionsFromGodotPath(ResourceQuestionsPath, out var resourceQuestions, out _) && resourceQuestions.Count > 0)
        {
            return new QuestionDataRepository().ReplaceQuestions(resourceQuestions, out message);
        }

        if (TryLoadQuestionsFromGodotPath(ResourceSeedQuestionsPath, out var seedQuestions, out _) && seedQuestions.Count > 0)
        {
            return new QuestionDataRepository().ReplaceQuestions(seedQuestions, out message);
        }

        message = "Kein Legacy-CSV fuer den Erstimport gefunden.";
        return false;
    }

    private static List<QuestionData> MergeQuestions(List<QuestionData> existingQuestions, List<QuestionData> importedQuestions, out int skippedCount)
    {
        var mergedQuestions = existingQuestions
            .Select(CloneQuestion)
            .ToList();
        var knownKeys = new HashSet<string>(mergedQuestions.Select(BuildQuestionKey), StringComparer.Ordinal);
        skippedCount = 0;

        foreach (var importedQuestion in importedQuestions)
        {
            var key = BuildQuestionKey(importedQuestion);
            if (!knownKeys.Add(key))
            {
                skippedCount++;
                continue;
            }

            mergedQuestions.Add(CloneQuestion(importedQuestion));
        }

        return mergedQuestions;
    }

    private static QuestionData CloneQuestion(QuestionData question)
    {
        return new QuestionData
        {
            Id = question.Id,
            Category = question.Category,
            Text = question.Text,
            Answers = question.Answers.ToList(),
            CorrectIndex = question.CorrectIndex,
            Explanation = question.Explanation,
            Difficulty = question.Difficulty,
            Media = question.Media.Select(media => new QuestionMediaData
            {
                Id = media.Id,
                QuestionId = media.QuestionId,
                MediaType = media.MediaType,
                Timing = media.Timing,
                StoredPath = media.StoredPath,
                OriginalFileName = media.OriginalFileName
            }).ToList()
        };
    }

    private static string BuildQuestionKey(QuestionData question)
    {
        var answers = question.Answers
            .Take(4)
            .Concat(Enumerable.Repeat(string.Empty, 4))
            .Take(4)
            .Select(NormalizeKeyPart);

        return string.Join("|", new[]
        {
            NormalizeKeyPart(question.Category),
            NormalizeKeyPart(question.Text),
            NormalizeKeyPart(question.CorrectIndex.ToString(CultureInfo.InvariantCulture))
        }.Concat(answers));
    }

    private static string NormalizeKeyPart(string? value)
    {
        return (value ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static bool TryLoadQuestionsFromGodotPath(string godotPath, out List<QuestionData> questions, out string errorMessage)
    {
        questions = new List<QuestionData>();
        errorMessage = string.Empty;

        try
        {
            if (!Godot.FileAccess.FileExists(godotPath))
            {
                errorMessage = $"Fragedatei nicht gefunden: {godotPath}";
                return false;
            }

            using var file = Godot.FileAccess.Open(godotPath, Godot.FileAccess.ModeFlags.Read);
            if (file == null)
            {
                errorMessage = $"Fragedatei konnte nicht geoeffnet werden: {godotPath}";
                return false;
            }

            questions = ParseCsv(file.GetAsText());
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Fehler beim Laden der Fragen aus {godotPath}: {ex.Message}";
            return false;
        }
    }

    private static bool TryLoadQuestionsFromAbsolutePath(string absolutePath, out List<QuestionData> questions, out string errorMessage)
    {
        questions = new List<QuestionData>();
        errorMessage = string.Empty;

        try
        {
            if (!File.Exists(absolutePath))
            {
                errorMessage = $"CSV-Datei nicht gefunden: {absolutePath}";
                return false;
            }

            questions = ParseCsv(File.ReadAllText(absolutePath, Encoding.UTF8));
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Fehler beim Laden der CSV: {ex.Message}";
            return false;
        }
    }

    private static List<QuestionData> ParseCsv(string csv)
    {
        var normalized = csv.Replace("\r\n", "\n").Replace('\r', '\n');
        var rows = normalized.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        if (rows.Count == 0)
        {
            return new List<QuestionData>();
        }

        var actualHeader = rows[0].Trim().Trim('\uFEFF');
        var headerColumns = ParseCsvLine(actualHeader);
        var normalizedHeader = string.Join(";", headerColumns).Trim();
        var supportsDifficulty = headerColumns.Count >= 9 && string.Equals(normalizedHeader, CsvHeader, StringComparison.OrdinalIgnoreCase);
        var supportsLegacyHeader = headerColumns.Count == 8 && string.Equals(normalizedHeader, "question;answer_a;answer_b;answer_c;answer_d;correct;category;explanation", StringComparison.OrdinalIgnoreCase);
        if (!supportsDifficulty && !supportsLegacyHeader)
        {
            throw new FormatException($"Die CSV-Kopfzeile ist ungueltig. Erwartet wird: {CsvHeader}. Gefunden wurde: {normalizedHeader}");
        }

        var questions = new List<QuestionData>();
        for (var lineIndex = 1; lineIndex < rows.Count; lineIndex++)
        {
            var columns = ParseCsvLine(rows[lineIndex]);
            if (columns.Count < 8)
            {
                throw new FormatException($"CSV-Zeile {lineIndex + 1} hat zu wenige Spalten.");
            }

            ValidateQuestionRow(columns, lineIndex + 1, supportsDifficulty);
            questions.Add(new QuestionData
            {
                Id = questions.Count + 1,
                Text = columns[0],
                Answers = new List<string> { columns[1], columns[2], columns[3], columns[4] },
                CorrectIndex = int.Parse(columns[5], CultureInfo.InvariantCulture),
                Category = columns[6],
                Explanation = columns[7],
                Difficulty = supportsDifficulty ? int.Parse(columns[8], CultureInfo.InvariantCulture) : 3
            });
        }

        return questions;
    }

    private static void ValidateQuestionRow(IReadOnlyList<string> columns, int lineNumber, bool hasDifficulty)
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

        if (!int.TryParse(columns[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out var correctIndex) || correctIndex < 0 || correctIndex > 3)
        {
            throw new FormatException($"CSV-Zeile {lineNumber} hat einen ungueltigen correct-Wert.");
        }

        if (string.IsNullOrWhiteSpace(columns[6]))
        {
            throw new FormatException($"CSV-Zeile {lineNumber} hat keine Kategorie.");
        }

        if (hasDifficulty && (!int.TryParse(columns[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out var difficulty) || difficulty < 1 || difficulty > 5))
        {
            throw new FormatException($"CSV-Zeile {lineNumber} hat einen ungueltigen difficulty-Wert. Erlaubt sind 1 bis 5.");
        }
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
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
            else if (c == ';' && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString().Trim());
        return result;
    }

    private static string BuildCsv(IEnumerable<QuestionData> questions)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CsvHeader);

        foreach (var q in questions.OrderBy(x => x.Category).ThenBy(x => x.Text))
        {
            var answers = q.Answers?.ToList() ?? new List<string>();
            while (answers.Count < 4)
            {
                answers.Add(string.Empty);
            }

            sb.Append(EscapeCsv(q.Text)).Append(';')
              .Append(EscapeCsv(answers[0])).Append(';')
              .Append(EscapeCsv(answers[1])).Append(';')
              .Append(EscapeCsv(answers[2])).Append(';')
              .Append(EscapeCsv(answers[3])).Append(';')
              .Append(q.CorrectIndex.ToString(CultureInfo.InvariantCulture)).Append(';')
              .Append(EscapeCsv(q.Category)).Append(';')
              .Append(EscapeCsv(q.Explanation)).Append(';')
              .Append(Math.Clamp(q.Difficulty, 1, 5).ToString(CultureInfo.InvariantCulture))
              .AppendLine();
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        value ??= string.Empty;
        var normalized = value.Replace("\r\n", "\n").Replace('\r', '\n');
        var mustQuote = normalized.Contains(';') || normalized.Contains('"') || normalized.Contains('\n');
        return mustQuote ? '"' + normalized.Replace("\"", "\"\"") + '"' : normalized;
    }
}
