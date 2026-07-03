using Godot;

public partial class DeadMenu : Control
{
	public override void _Ready()
	{
		AddToGroup("dead_menu");
		ProcessMode = ProcessModeEnum.Always;
		Visible = false;
		VisibilityChanged += OnVisibilityChanged;

		GetNode<Button>("Panel/Mainmeny/Button").Pressed += OnRetryPressed;
		GetNode<Button>("Panel/Mainmeny/Button2").Pressed += OnMainMenuPressed;
		GetNode<Button>("Panel/Mainmeny/Button3").Pressed += OnExitPressed;
	}

	public void ShowDeadMenu()
	{
		Visible = true;
	}

	private void OnVisibilityChanged()
	{
		GetTree().Paused = Visible;
	}

	private void OnRetryPressed()
	{
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile("res://scenes/mainstage.tscn");
	}

	private void OnMainMenuPressed()
	{
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile("res://scenes/main_menu.tscn");
	}

	private void OnExitPressed()
	{
		GetTree().Quit();
	}
}
