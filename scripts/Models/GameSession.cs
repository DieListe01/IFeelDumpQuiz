namespace IFeelDumpQuiz.Models;

public sealed class GameSession
{
    public Guid SessionId { get; } = Guid.NewGuid();
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAtUtc { get; set; }
    public GameSettings Settings { get; set; } = new();
    public List<Player> Players { get; set; } = new();
    public List<Question> Questions { get; set; } = new();
    public List<PlayerAnswer> Answers { get; set; } = new();
    public int CurrentQuestionIndex { get; set; }

    public bool IsFinished => CurrentQuestionIndex >= Questions.Count;
}
