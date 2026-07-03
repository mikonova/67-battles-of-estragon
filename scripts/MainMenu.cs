using Godot;

public partial class MainMenu : Control
{
	private Control _optionsOverlay;
	private Control _creditsOverlay;
	private HSlider _volumeSlider;

	public override void _Ready()
	{
		_optionsOverlay = GetNode<Control>("OptionsOverlay");
		_creditsOverlay = GetNode<Control>("CreditsOverlay");
		_volumeSlider = GetNode<HSlider>("OptionsOverlay/Panel/MarginContainer/VBoxContainer/VolumeSlider");

		GetNode<Button>("Panel/Mainmeny/Button").Pressed += OnStartPressed;
		GetNode<Button>("Panel/Mainmeny/Button2").Pressed += OnOptionsPressed;
		GetNode<Button>("Panel/Mainmeny/Button3").Pressed += OnCreditsPressed;
		GetNode<Button>("Panel/Mainmeny/Button4").Pressed += OnExitPressed;

		GetNode<Button>("OptionsOverlay/Panel/MarginContainer/VBoxContainer/BackButton").Pressed += HideOptions;
		GetNode<Button>("CreditsOverlay/Panel/MarginContainer/VBoxContainer/BackButton").Pressed += HideCredits;

		_volumeSlider.ValueChanged += OnVolumeChanged;
		_volumeSlider.Value = DbToSlider(AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("Master")));
	}

	private void OnStartPressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/mainstage.tscn");
	}

	private void OnOptionsPressed()
	{
		_optionsOverlay.Visible = true;
	}

	private void OnCreditsPressed()
	{
		_creditsOverlay.Visible = true;
	}

	private void OnExitPressed()
	{
		GetTree().Quit();
	}

	private void HideOptions()
	{
		_optionsOverlay.Visible = false;
	}

	private void HideCredits()
	{
		_creditsOverlay.Visible = false;
	}

	private void OnVolumeChanged(double value)
	{
		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), SliderToDb((float)value));
	}

	private static float SliderToDb(float sliderValue)
	{
		if (sliderValue <= 0.001f)
		{
			return -80f;
		}

		return Mathf.LinearToDb(sliderValue);
	}

	private static float DbToSlider(float db)
	{
		if (db <= -79f)
		{
			return 0f;
		}

		return Mathf.DbToLinear(db);
	}
}
