using System;
using System.Collections.Generic;
using System.Linq;

namespace IFeelDumpQuiz;

public static class GameSession
{
    public static GameConfig Config { get; private set; } = new();
    public static List<QuestionData> AllQuestions { get; private set; } = new();
    public static List<QuestionData> ActiveQuestions { get; private set; } = new();
    public static List<PlayerState> Players { get; private set; } = new();
    public static List<AnswerRecord> AnswerHistory { get; private set; } = new();
    public static int CurrentQuestionIndex { get; set; }
    public static DateTime StartedAt { get; private set; }
    public static DateTime? FinishedAt { get; private set; }
    public static bool HistorySaved { get; private set; }

    private static readonly Random Rng = new();
    private static Queue<int> _startPlayerQueue = new();

    public static void StartNewGame(GameConfig config, List<QuestionData> questions)
    {
        Config = config;
        AllQuestions = questions;
        ActiveQuestions = FilterAndSelectQuestions(questions, config.Category, config.QuestionCount);
        Players = config.PlayerNames
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(n => new PlayerState { Name = n.Trim() })
            .ToList();

        AnswerHistory = new List<AnswerRecord>();
        CurrentQuestionIndex = 0;
        StartedAt = DateTime.UtcNow;
        FinishedAt = null;
        HistorySaved = false;
        RefillStartPlayerQueue();
    }

    public static QuestionData? CurrentQuestion =>
        CurrentQuestionIndex >= 0 && CurrentQuestionIndex < ActiveQuestions.Count
            ? ActiveQuestions[CurrentQuestionIndex]
            : null;

    public static bool HasMoreQuestions => CurrentQuestionIndex < ActiveQuestions.Count;

    public static int GetNextStartPlayerIndex()
    {
        if (Players.Count == 0)
        {
            return 0;
        }

        if (_startPlayerQueue.Count == 0)
        {
            RefillStartPlayerQueue();
        }

        return _startPlayerQueue.Dequeue();
    }

    public static IEnumerable<int> GetQuestionOrder(int startPlayerIndex)
    {
        for (var i = 0; i < Players.Count; i++)
        {
            yield return (startPlayerIndex + i) % Players.Count;
        }
    }

    public static void RecordAnswer(string playerName, int questionId, int selectedIndex, bool isCorrect)
    {
        var player = Players.FirstOrDefault(p => string.Equals(p.Name, playerName, StringComparison.OrdinalIgnoreCase));
        if (player == null)
        {
            return;
        }

        AnswerHistory.Add(new AnswerRecord
        {
            PlayerName = playerName,
            QuestionId = questionId,
            SelectedIndex = selectedIndex,
            IsCorrect = isCorrect,
            AnsweredAt = DateTime.UtcNow
        });

        if (isCorrect)
        {
            player.Score += 1;
            player.CorrectAnswers += 1;
        }
        else
        {
            player.WrongAnswers += 1;
        }
    }

    public static void MoveToNextQuestion()
    {
        CurrentQuestionIndex++;
    }

    public static double GetAccuracy(PlayerState player)
    {
        var total = player.CorrectAnswers + player.WrongAnswers;
        return total == 0 ? 0 : (double)player.CorrectAnswers / total * 100.0;
    }

    public static List<string> GetCategories()
    {
        return AllQuestions
            .Select(q => q.Category)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList();
    }

    private static List<QuestionData> FilterAndSelectQuestions(List<QuestionData> source, string category, int count)
    {
        var filtered = category == "Alle"
            ? source.ToList()
            : source.Where(q => string.Equals(q.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();

        filtered = filtered.OrderBy(_ => Rng.Next()).ToList();
        return filtered.Take(Math.Max(1, Math.Min(count, filtered.Count))).ToList();
    }

    private static void RefillStartPlayerQueue()
    {
        var indices = Enumerable.Range(0, Players.Count)
            .OrderBy(_ => Rng.Next())
            .ToList();

        _startPlayerQueue = new Queue<int>(indices);
    }

    public static void MarkFinished()
    {
        FinishedAt ??= DateTime.UtcNow;
    }

    public static void MarkHistorySaved()
    {
        HistorySaved = true;
    }
}
