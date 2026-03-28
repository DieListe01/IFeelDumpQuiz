using IFeelDumpQuiz.Repositories;
using IFeelDumpQuiz.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IFeelDumpQuiz;

public static class QuestionRepository
{
	private static readonly QuestionDataRepository QuestionDataRepository = new();

	public static List<QuestionData> LoadQuestions()
	{
		return QuestionDataRepository.LoadActiveQuestions();
	}

	public static bool TryLoadQuestions(out List<QuestionData> questions, out string errorMessage)
	{
		try
		{
			questions = LoadQuestions();
			errorMessage = string.Empty;
			return true;
		}
		catch (Exception ex)
		{
			questions = new List<QuestionData>();
			errorMessage = $"Fehler beim Laden der Fragen: {ex.Message}";
			return false;
		}
	}

	public static bool SaveQuestions(List<QuestionData> questions, out string message)
	{
		return QuestionDataRepository.ReplaceQuestions(questions, out message);
	}

	public static bool SaveAllQuestions(List<QuestionData> questions, out string message)
	{
		return QuestionDataRepository.ReplaceQuestions(questions, out message);
	}

	public static bool TryImportQuestions(string sourcePath, out int importedCount, out string message)
	{
		return CsvImportExportService.TryImportQuestions(sourcePath, out importedCount, out message);
	}

	public static bool ExportQuestions(string targetPath, string? categoryFilter, out string message)
	{
		return CsvImportExportService.ExportQuestions(targetPath, categoryFilter, out message);
	}

	public static bool ExportTemplate(string targetPath, out string message)
	{
		return CsvImportExportService.ExportTemplate(targetPath, out message);
	}

	public static bool TryImportInitialQuestionsFromLegacyCsv(out string message)
	{
		return CsvImportExportService.TryImportInitialQuestionsFromLegacyCsv(out message);
	}
}
