using Godot;

public partial class camera2d : Camera2D
{
	private Node2D _target;
	private float _cameraPosY;
	private float _defaultCameraX;
	private float _deadZone = 100.0f;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		Retarget(GetParent<Node2D>());
	}

	public void Retarget(Node2D target)
	{
		if (target == null || !GodotObject.IsInstanceValid(target))
		{
			return;
		}

		_target = target;
		_defaultCameraX = target.GlobalPosition.X;
		_cameraPosY = target.GlobalPosition.Y;
		SnapToTarget();
	}

	public override void _Process(double delta)
	{
		if (_target == null || !GodotObject.IsInstanceValid(_target))
		{
			return;
		}

		float targetY = _target.GlobalPosition.Y;

		if (targetY > _cameraPosY + _deadZone)
		{
			_cameraPosY = targetY - _deadZone;
		}
		else if (targetY < _cameraPosY - _deadZone)
		{
			_cameraPosY = targetY + _deadZone;
		}

		ApplyFollowPosition();
	}

	private void SnapToTarget()
	{
		if (_target == null || !GodotObject.IsInstanceValid(_target))
		{
			return;
		}

		_defaultCameraX = _target.GlobalPosition.X;
		_cameraPosY = _target.GlobalPosition.Y;
		ApplyFollowPosition();
	}

	private void ApplyFollowPosition()
	{
		if (_target == GetParent())
		{
			Position = Vector2.Zero;
			return;
		}

		GlobalPosition = new Vector2(_defaultCameraX, _cameraPosY);
	}
}
