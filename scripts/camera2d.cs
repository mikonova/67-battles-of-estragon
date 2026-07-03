using Godot;
using System;

public partial class camera2d : Camera2D
{
	private Node2D _target;
	private Vector2 _cameraPos;
	private float _cameraPosY;
	private float _deadZone = 100.0f;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_target = GetParent<Node2D>();
		_cameraPos = _target.GlobalPosition;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Mathf.Abs(_target.GlobalPosition.Y - _cameraPosY) > _deadZone)
		{
			_cameraPosY = _target.GlobalPosition.Y;
		}
		GlobalPosition = new Vector2(GlobalPosition.X, _cameraPosY);
	}
}
