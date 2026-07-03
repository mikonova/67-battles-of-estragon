using Godot;
using System;

public partial class MonsterPlayer : CharacterBody2D
{
	public const float Speed = 350.0f;
	public const float StopDistance = 8.0f;

	public const float Acceleration = 520.0f;
	public const float Friction = 35.0f;

	public const float DashSpeed = 850.0f;
	public const float DashTime = 0.15f;

	private AnimatedSprite2D _animatedSprite;

	private bool _isDashing = false;
	private float _dashTimer = 0f;
	private Vector2 _dashDirection = Vector2.Zero;

	public override void _Ready()
	{
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

		if (_animatedSprite != null)
		{
			_animatedSprite.AnimationFinished += OnAnimationFinished;
			PlayAnim("idle");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		float deltaF = (float)delta;

		Vector2 mousePosition = GetGlobalMousePosition();

		if (Input.IsActionJustPressed("ui_accept") && !_isDashing)
		{
			StartDash(mousePosition);
		}

		if (_isDashing)
		{
			_dashTimer -= deltaF;
			Velocity = _dashDirection * DashSpeed;

			if (_dashTimer <= 0f)
			{
				_isDashing = false;
			}

			MoveAndSlide();
			return;
		}

		Vector2 direction = mousePosition - GlobalPosition;

		if (mousePosition.X < GlobalPosition.X)
		{
			_animatedSprite.FlipH = true;
		}
		else
		{
			_animatedSprite.FlipH = false;
		}
		if (direction.Length() > StopDistance)
		{
			Vector2 targetVelocity = direction.Normalized() * Speed;
			Velocity = Velocity.MoveToward(targetVelocity, Acceleration * deltaF);
		}
		else
		{
			Velocity = Velocity.MoveToward(Vector2.Zero, Friction * deltaF);
		}

		UpdateMoveAnimation();

		MoveAndSlide();
	}

	private void StartDash(Vector2 mousePosition)
	{
		Vector2 direction = mousePosition - GlobalPosition;

		if (direction.Length() <= 1f)
			return;

		_isDashing = true;
		_dashTimer = DashTime;
		_dashDirection = direction.Normalized();

		PlayAnim("dash");
	}

	private void UpdateMoveAnimation()
	{
		if (Velocity.Length() > 10.0f)
		{
			PlayAnim("run");
		}
		else
		{
			PlayAnim("idle");
		}
	}

	private void PlayAnim(string animName)
	{
		if (_animatedSprite == null)
			return;

		if (_animatedSprite.SpriteFrames == null)
			return;

		if (!_animatedSprite.SpriteFrames.HasAnimation(animName))
			return;

		if (_animatedSprite.Animation == animName && _animatedSprite.IsPlaying())
			return;

		_animatedSprite.Play(animName);
	}

	private void OnAnimationFinished()
	{
		if (_animatedSprite.Animation == "dash")
		{
			_isDashing = false;
		}
	}
}
