using Godot;
using System;
using System.Linq;

public partial class playerhumanmovement : CharacterBody2D
{
	private AnimatedSprite2D _animatedSprite;
	private Area2D  _collisionChecker;
	private Area2D _currentArea;
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
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_collisionChecker = GetNode<Area2D>("PlayerCollisionChecker");
		_collisionChecker.AreaEntered += OnAreaEntered;
		_collisionChecker.AreaExited += OnAreaExited;
	}
	
	private void OnAreaEntered(Area2D area)
	{
		_currentArea = area;
	}
	
	private void OnAreaExited(Area2D area)
	{
		if (_currentArea == area)
		{
			_currentArea = null; 
		}
	}
	
	public override void _PhysicsProcess(double delta)
	{
		// Hiding
		if (Input.IsActionJustPressed("interact") && _currentArea?.Name == "BigBush" && !IsHiding)
		{
			SpeedModifier = (int)SpeedModifierPredefined.Still;
			IsHiding = true;
			this.Visible = false;
			foreach (CollisionShape2D collider in GetChildren().OfType<CollisionShape2D>())
			{
				collider.Disabled = true;
			}
			
		}
		else if  (Input.IsActionJustPressed("interact") && IsHiding)
		{
			SpeedModifier = (int)SpeedModifierPredefined.Default;
			IsHiding = false;
			this.Visible = true;
			foreach (CollisionShape2D collider in GetChildren().OfType<CollisionShape2D>())
			{
				collider.Disabled = false;
			}
		}
		
		// Movement
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
	}
}
