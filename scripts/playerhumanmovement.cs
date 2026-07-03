using Godot;
using System.Linq;

public partial class playerhumanmovement : CharacterBody2D
{
	private static readonly StringName BigBushName = "BigBush";

	private AnimatedSprite2D _animatedSprite;
	private Area2D _collisionChecker;
	public const float Speed = 300.0f;
	public int SpeedModifier = 1;
	public enum SpeedModifierPredefined
	{
		Still = 0,
		Default,
		Double
	}
	public bool IsHiding;

	public override void _Ready()
	{
		AddToGroup("player");
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_collisionChecker = GetNode<Area2D>("PlayerCollisionChecker");
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Velocity = direction * Speed * SpeedModifier;

		if (direction != Vector2.Zero)
		{
			_animatedSprite.Play("walk");
		}
		else
		{
			_animatedSprite.Play("idle");
		}

		if (direction.X > 0)
		{
			_animatedSprite.FlipH = false;
		}
		else if (direction.X < 0)
		{
			_animatedSprite.FlipH = true;
		}

		MoveAndSlide();

		if (!Input.IsActionJustPressed("interact"))
		{
			return;
		}

		if (IsHiding)
		{
			StopHiding();
			return;
		}

		if (IsInBigBush())
		{
			StartHiding();
		}
	}

	private bool IsInBigBush()
	{
		foreach (Area2D area in _collisionChecker.GetOverlappingAreas())
		{
			if (area.Name == BigBushName || area.IsInGroup("big_bush"))
			{
				return true;
			}
		}

		return false;
	}

	private void StartHiding()
	{
		SpeedModifier = (int)SpeedModifierPredefined.Still;
		IsHiding = true;
		_animatedSprite.Visible = false;
		GetNode<CollisionShape2D>("CollisionShape2D").Disabled = true;
	}

	private void StopHiding()
	{
		SpeedModifier = (int)SpeedModifierPredefined.Default;
		IsHiding = false;
		_animatedSprite.Visible = true;
		GetNode<CollisionShape2D>("CollisionShape2D").Disabled = false;
	}
}
