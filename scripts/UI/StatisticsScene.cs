using Godot;

namespace IFeelDumpQuiz.UI;

public partial class StatisticsScene : Control
{
    public void OnBackToMenuPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
    }

    public void OnOpenHistoryPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/HistoryScene.tscn");
    }
}
