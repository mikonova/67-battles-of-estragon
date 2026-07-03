using Godot;
using System;

public partial class camera2d : Camera2D
{
	private Node2D _target;
	private Vector2 _cameraPos;
	private float _cameraPosY;
	private float _defaultCameraX;
	private float _deadZone = 100.0f;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_target = GetParent<Node2D>();
		_cameraPos = _target.GlobalPosition;
		_defaultCameraX = _target.GlobalPosition.X;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		float targetY = _target.GlobalPosition.Y;

		if (targetY > _cameraPosY + _deadZone)
			_cameraPosY = targetY - _deadZone;
		else if (targetY < _cameraPosY - _deadZone)
			_cameraPosY = targetY + _deadZone;

		GlobalPosition = new Vector2(_defaultCameraX, _cameraPosY);
	}
}
