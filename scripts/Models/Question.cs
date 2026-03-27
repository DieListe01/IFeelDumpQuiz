namespace IFeelDumpQuiz.Models;

public sealed class Question
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public List<string> Answers { get; set; } = new();
    public int CorrectAnswer { get; set; }
    public string ExplanationText { get; set; } = string.Empty;
}
