using Godot;

public partial class GameTimer : Control
{
	private const float DurationSeconds = 300f;

	private float _timeLeft = DurationSeconds;
	private Label _label;
	private DeadMenu _deadMenu;
	private bool _expired;

	public override void _Ready()
	{
		_label = GetNode<Label>("TimerLabel");
		_deadMenu = GetParent().GetNode<DeadMenu>("DeadMenu");
		UpdateLabel();
	}

	public override void _Process(double delta)
	{
		if (_expired || GetTree().Paused)
		{
			return;
		}

		_timeLeft -= (float)delta;
		if (_timeLeft <= 0f)
		{
			_timeLeft = 0f;
			_expired = true;
			UpdateLabel();
			_deadMenu.ShowDeadMenu();
			return;
		}

		UpdateLabel();
	}

	private void UpdateLabel()
	{
		int totalSeconds = Mathf.CeilToInt(_timeLeft);
		int minutes = totalSeconds / 60;
		int seconds = totalSeconds % 60;
		_label.Text = $"{minutes:00}:{seconds:00}";
	}
}
