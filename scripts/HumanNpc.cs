using Godot;
using System;

public partial class HumanNpc : CharacterBody2D
{
	[Signal]
	public delegate void EscapedToBottomEventHandler();

	[Export] public float WalkSpeed = 80.0f;
	[Export] public float RunAwaySpeed = 220.0f;
	[Export] public Vector2 IdleMoveDirection = Vector2.Down;
	[Export] public float EscapeBottomY;

	private AnimatedSprite2D _animatedSprite;
	private Node2D _monster;
	
	[Export] public float RunAfterLoseTime = 1.0f;

	private float _runAfterLoseTimer = 0f;
	private Vector2 _lastRunDirection = Vector2.Down;
	private bool _escaped;

	public override void _Ready()
	{
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

		Area2D canSeeArea = GetNode<Area2D>("Can_see_area");
		canSeeArea.BodyEntered += OnBodyEntered;
		canSeeArea.BodyExited += OnBodyExited;

		PlayAnim("walk");
	}

	public override void _PhysicsProcess(double delta)
	{
		float deltaF = (float)delta;

		if (_monster != null && GodotObject.IsInstanceValid(_monster))
		{
			RunAwayFromMonster();
		}
		else if (_runAfterLoseTimer > 0f)
		{
			_runAfterLoseTimer -= deltaF;
			Velocity = _lastRunDirection * RunAwaySpeed;
		}
		else
		{
			MoveIdle();
		}

		UpdateAnimation();
		MoveAndSlide();
		CheckEscape();
	}

	private void CheckEscape()
	{
		if (_escaped)
		{
			return;
		}

		if (EscapeBottomY > 0f && GlobalPosition.Y >= EscapeBottomY)
		{
			TriggerEscape();
			return;
		}

		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			KinematicCollision2D collision = GetSlideCollision(i);
			Node collider = collision.GetCollider() as Node;
			if (collider == null)
			{
				continue;
			}

			bool isMapCollision = collider.Name == "Collisions"
				|| collider.GetParent()?.Name == "Collisions";

			if (!isMapCollision)
			{
				continue;
			}

			if (Velocity.Y > 1f || collision.GetNormal().Y < -0.1f)
			{
				TriggerEscape();
				return;
			}
		}
	}

	private void TriggerEscape()
	{
		_escaped = true;
		Velocity = Vector2.Zero;
		EmitSignal(SignalName.EscapedToBottom);
	}

	private void MoveIdle()
	{
		Vector2 direction = IdleMoveDirection;
		if (direction.LengthSquared() < 0.01f)
		{
			direction = Vector2.Down;
		}

		Velocity = direction.Normalized() * WalkSpeed;
	}

	private void RunAwayFromMonster()
	{
		Vector2 awayDirection = GlobalPosition - _monster.GlobalPosition;

		if (awayDirection.Length() < 1.0f)
		{
			awayDirection = _lastRunDirection;
		}

		_lastRunDirection = awayDirection.Normalized();
		Velocity = _lastRunDirection * RunAwaySpeed;
	}

	private void OnBodyEntered(Node body)
	{
		if (body is MonsterPlayer monster)
		{
			_monster = monster;
		}
	}

	private void OnBodyExited(Node body)
	{
		if (body == _monster)
		{
			GD.Print("NPC потерял монстра, но ещё бежит");
			_monster = null;
			_runAfterLoseTimer = RunAfterLoseTime;
		}
	}

	private void UpdateAnimation()
	{
		if (Velocity.Length() > 5.0f)
		{
			PlayAnim("walk");
		}
		else
		{
			PlayAnim("idle");
		}

		// Разворот влево/вправо по движению
		if (Mathf.Abs(Velocity.X) > 1.0f)
		{
			_animatedSprite.FlipH = Velocity.X < 0;
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
}
