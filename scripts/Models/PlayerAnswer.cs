namespace IFeelDumpQuiz.Models;

public sealed class PlayerAnswer
{
    public int PlayerLocalId { get; set; }
    public int QuestionId { get; set; }
    public int SelectedAnswer { get; set; }
    public bool IsCorrect { get; set; }
    public DateTime AnsweredAtUtc { get; set; } = DateTime.UtcNow;
}
