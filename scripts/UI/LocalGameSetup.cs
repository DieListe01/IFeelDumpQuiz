using Godot;

namespace IFeelDumpQuiz.UI;

public partial class LocalGameSetup : Control
{
    public override void _Ready()
    {
        GD.Print("LocalGameSetup bereit. Hier Eingabefelder mit GameSettings und Spielern verknüpfen.");
    }

    public void OnBackPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
    }

    public void OnStartPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/QuizGame.tscn");
    }
}
