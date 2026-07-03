using Godot;

public partial class MainMenu : Control
{
	public override void _Ready()
	{
		GetNode<Button>("Panel/Mainmeny/Button").Pressed += OnStartPressed;
	}

	private void OnStartPressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/mainstage.tscn");
	}
}
