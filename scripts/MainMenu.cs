using Godot;
using System.IO;

public partial class MainMenu : Control
{
    public MainMenu()
    {
        File.WriteAllText("test.txt", "Constructor called");
    }

    public override void _EnterTree()
    {
        File.WriteAllText("test.txt", "EnterTree called");
        base._EnterTree();
    }

    public override void _Ready()
    {
        File.WriteAllText("test.txt", "Ready called");
        GetTree().Quit();
    }
}