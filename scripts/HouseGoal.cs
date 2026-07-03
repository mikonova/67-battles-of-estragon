using Godot;

public partial class HouseGoal : Area2D
{
	private bool _triggered;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (_triggered || body is not playerhumanmovement)
		{
			return;
		}

		_triggered = true;
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile("res://scenes/main_menu.tscn");
	}
}
