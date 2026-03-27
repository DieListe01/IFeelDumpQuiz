using Godot;

namespace IFeelDumpQuiz.UI;

public partial class QuizGame : Control
{
    public override void _Ready()
    {
        GD.Print("QuizGame bereit. Hier Fragetext, Antwortbuttons, Timer und Moderatorbereich anschließen.");
    }

    public void OnRevealAnswerPressed()
    {
        GD.Print("Moderator löst die Antwort auf.");
    }

    public void OnNextQuestionPressed()
    {
        GD.Print("Nächste Frage.");
    }

    public void OnStatisticsPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/StatisticsScene.tscn");
    }
}
