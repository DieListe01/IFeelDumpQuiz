using Godot;

public partial class TestScene : Node
{
    public override void _Ready()
    {
        GD.Print("TestScene._Ready: Hello from test scene!");
        GetTree().Quit();
    }
}