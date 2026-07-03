using Godot;
using System.Linq;

public partial class playerhumanmovement : CharacterBody2D
{
	private static readonly StringName BigBushName = "BigBush";

	private AnimatedSprite2D _animatedSprite;
	private ProgressBar _fearBar;
	private Area2D _collisionChecker;
	private Area2D _fearCollisionChecker;
	private Cumpfire? _campfireNearest;
	public const float Speed = 200.0f;
	public int SpeedModifier = 1;
	public enum SpeedModifierPredefined { Still = 0, Default, Double }
	public bool IsHiding;
	private float _fear = 0.0f;
	private float depletionRate = 20f;
	private float fearIncreaseRate = 3f;
	private float _timer = 0.0f;
	private Area2D _campfireArea;
	
	private void _areaEntered(Area2D area)
	{
		if (area.Name == "plus_sainiyty_area")
			_campfireArea = area;
	}

	public override void _Ready()
	{
		AddToGroup("player");
		_fearBar = GetNode<ProgressBar>("FearBar");
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_collisionChecker = GetNode<Area2D>("PlayerCollisionChecker");
		_fearCollisionChecker = GetNode<Area2D>("FearCollisionChecker");
		_fearCollisionChecker.AreaEntered += _areaEntered;
		_fearBar.MaxValue = 100f;
	}

	public override void _PhysicsProcess(double delta)
	{
		float deltaF = (float)delta;
		_fear += fearIncreaseRate * deltaF;
		_fear = Mathf.Min(_fear, 100f);
		if (_campfireArea != null)
		{
			_campfireNearest = _campfireArea.GetParent<Cumpfire>();
			if (_campfireNearest != null && _campfireNearest.fire && _fear > 0f)
			{
				_fear -= depletionRate * deltaF;
				_fear = Mathf.Max(_fear, 0f);
			}
		}
		_fearBar.Value = _fear;
		Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Velocity = direction * Speed * SpeedModifier;
		if (direction != Vector2.Zero)
			_animatedSprite.Play("walk");
		else
			_animatedSprite.Play("idle");
		if (direction.X > 0)
			_animatedSprite.FlipH = false;
		else if (direction.X < 0)
			_animatedSprite.FlipH = true;
		MoveAndSlide();
	}

	public bool IsInBigBush()
	{
		var overlappingAreas = _collisionChecker.GetOverlappingAreas();
		return overlappingAreas.OfType<Area2D>().Any(a => a.Name == BigBushName || a.IsInGroup("big_bush"));
	}

	public void StartHiding()
	{
		IsHiding = true;
		SpeedModifier = (int)SpeedModifierPredefined.Still;
	}

	public void StopHiding()
	{
		IsHiding = false;
		SpeedModifier = (int)SpeedModifierPredefined.Default;
	}
}
