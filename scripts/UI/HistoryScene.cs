using Godot;

namespace IFeelDumpQuiz.UI;

public partial class HistoryScene : Control
{
    public void OnBackPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
    }

    public void OnDeleteAllPressed()
    {
        GD.Print("Alle historischen Spiele löschen.");
    }
}
