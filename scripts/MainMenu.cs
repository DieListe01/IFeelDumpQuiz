using Godot;

public partial class MainMenu : Control
{
    public override void _Ready()
    {
        GD.Print("MainMenu._Ready: Hello World!");
        // Quit the application immediately to test if the scene loads
        GetTree().Quit();
    }
}