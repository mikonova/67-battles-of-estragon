using Godot;

public partial class PauseMenu : Control
{
	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		Visible = false;

		GetNode<Button>("Panel/Mainmeny/Button").Pressed += HidePause;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!@event.IsActionPressed("ui_cancel"))
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

	private void ShowPause()
	{
		Visible = true;
		GetTree().Paused = true;
	}

	private void HidePause()
	{
		Visible = false;
		GetTree().Paused = false;
	}
}
