using Godot;
using System;

public partial class playerhumanmovement : CharacterBody2D
{
	private AnimatedSprite2D _animatedSprite;
	public const float Speed = 300.0f;
	public const float JumpVelocity = -400.0f;

	public override void _Ready()
	{
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
	}
	public override void _PhysicsProcess(double delta)
	{
		Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Velocity = direction * Speed;
		if (direction != Vector2.Zero)
			_animatedSprite.Play("walk");
		else
			_animatedSprite.Play("idle");
		if (direction.X > 0)
		{
			_animatedSprite.FlipH = false;
		}
		else if (direction.X < 0)
		{
			_animatedSprite.FlipH = true;
		}
		MoveAndSlide();
	}
}
