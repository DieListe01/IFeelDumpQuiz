using Godot;

namespace IFeelDumpQuiz.UI;

public partial class MainMenu : Control
{
    public void OnStartLocalGamePressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/LocalGameSetup.tscn");
    }

    public void OnHistoryPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/HistoryScene.tscn");
    }

    public void OnExitPressed()
    {
        GetTree().Quit();
    }
}
