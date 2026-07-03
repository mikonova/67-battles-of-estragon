using Godot;
using System;

public partial class Item : Node2D
{
	private bool _playerInside = false;
	private Node _player;

	private Label _label;

	public override void _Ready()
	{
		_label = GetNode<Label>("Label");
		_label.Text = "E";
		_label.Visible = false;

		Area2D pickupArea = GetNode<Area2D>("PickupArea");

		pickupArea.BodyEntered += OnBodyEntered;
		pickupArea.BodyExited += OnBodyExited;
	}

	public override void _Process(double delta)
	{
		if (_playerInside && Input.IsActionJustPressed("interact"))
		{
			PickUp();
		}
	}

	private void OnBodyEntered(Node body)
	{
		if (body is MonsterPlayer)
		{
			_playerInside = true;
			_player = body;
			_label.Visible = true;
		}
	}

	private void OnBodyExited(Node body)
	{
		if (body == _player)
		{
			_playerInside = false;
			_player = null;
			_label.Visible = false;
		}
	}

	private void PickUp()
	{
		GD.Print("Предмет подобран");
		QueueFree();
	}
}
