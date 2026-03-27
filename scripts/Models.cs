using System;
using System.Collections.Generic;

namespace IFeelDumpQuiz;

public class QuestionData
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<string> Answers { get; set; } = new();
    public int CorrectIndex { get; set; }
    public string Explanation { get; set; } = string.Empty;
}

public class PlayerState
{
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }
    public int CorrectAnswers { get; set; }
    public int WrongAnswers { get; set; }
}

public class AnswerRecord
{
    public int QuestionId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int SelectedIndex { get; set; }
    public bool IsCorrect { get; set; }
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
}

public class GameConfig
{
    public List<string> PlayerNames { get; set; } = new();
    public int QuestionCount { get; set; } = 10;
    public string Category { get; set; } = "Alle";
    public int TimePerQuestionSeconds { get; set; } = 20;
}

public class GameHistoryEntry
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Category { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public int TimePerQuestionSeconds { get; set; }
    public int DurationSeconds { get; set; }
    public List<GameHistoryPlayerEntry> Players { get; set; } = new();
}

public class GameHistoryPlayerEntry
{
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }
    public int CorrectAnswers { get; set; }
    public int WrongAnswers { get; set; }
    public double Accuracy { get; set; }
}
