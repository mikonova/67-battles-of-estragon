using Godot;

public partial class PauseMenu : Control
{
	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		Visible = false;
		VisibilityChanged += OnVisibilityChanged;

		GetNode<Button>("Panel/Mainmeny/Button").Pressed += HidePause;
		GetNode<Button>("Panel/Mainmeny/Button2").Pressed += OnMainMenuPressed;
		GetNode<Button>("Panel/Mainmeny/Button3").Pressed += OnExitPressed;
	}

	private void OnVisibilityChanged()
	{
		GetTree().Paused = Visible;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!@event.IsActionPressed("ui_cancel"))
		{
			return;
		}

		if (IsDeadMenuOpen())
		{
			return;
		}

		if (Visible)
		{
			HidePause();
		}
		else
		{
			ShowPause();
		}

		GetViewport().SetInputAsHandled();
	}

	private bool IsDeadMenuOpen()
	{
		DeadMenu deadMenu = GetParent().GetNodeOrNull<DeadMenu>("DeadMenu");
		return deadMenu != null && deadMenu.Visible;
	}

	private void ShowPause()
	{
		Visible = true;
	}

	private void HidePause()
	{
		Visible = false;
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
