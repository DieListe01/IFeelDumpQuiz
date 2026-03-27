namespace IFeelDumpQuiz.Models;

public sealed class GameSettings
{
    public int QuestionCount { get; set; } = 10;
    public string Category { get; set; } = "Alle";
    public int TimePerQuestionSeconds { get; set; } = 20;
}
