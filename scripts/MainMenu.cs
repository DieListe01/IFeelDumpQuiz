using Godot;

public partial class MainMenu : Control
{
    public MainMenu()
    {
        GD.Print("MainMenu constructor called");
        throw new Exception("Test exception from constructor");
    }

    public override void _EnterTree()
    {
        GD.Print("MainMenu._EnterTree called");
        base._EnterTree();
    }

    public override void _Ready()
    {
        GD.Print("MainMenu._Ready: Hello World!");
        // Quit the application immediately to test if the scene loads
        GetTree().Quit();
    }
}