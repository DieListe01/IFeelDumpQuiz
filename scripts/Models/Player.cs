namespace IFeelDumpQuiz.Models;

public sealed class Player
{
    public int LocalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }
    public int CorrectAnswers { get; set; }
    public int WrongAnswers { get; set; }

    public double HitRate => (CorrectAnswers + WrongAnswers) == 0
        ? 0
        : (double)CorrectAnswers / (CorrectAnswers + WrongAnswers);
}
