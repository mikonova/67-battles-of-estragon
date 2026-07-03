using Godot;

public partial class playerhumanmovement : CharacterBody2D
{
	private static readonly StringName BigBushName = "BigBush";
	private static readonly StringName CampfireGroup = "campfire";

	private AnimatedSprite2D _animatedSprite;
	private Area2D _collisionChecker;
	private ProgressBar _fearBar;
	private AudioStreamPlayer _stepsPlayer;
	private DeadMenu _deadMenu;
	private float _fear;
	private bool _fearMaxed;

	public const float Speed = 150.0f;
	public int SpeedModifier = 1;
	public enum SpeedModifierPredefined
	{
		Still = 0,
		Default,
		Double
	}
	public bool IsHiding;

	[ExportGroup("Fear")]
	[Export] public float MaxFear = 100f;
	[Export] public float FearOutsideCampfirePercentPerSecond = 2f;
	[Export] public float FearDrainRate = 18f;
	[Export] public float CampfireFearDrainRate = 28f;
	[Export] public float MonsterHitFearPercent = 30f;
	[Export] public NodePath DeadMenuPath;

	[Export] public AudioStream StepSound;
	[Export] public float StepVolumeDb = -4f;

	public override void _Ready()
	{
		AddToGroup("player");
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_collisionChecker = GetNode<Area2D>("PlayerCollisionChecker");
		_fearBar = GetNode<ProgressBar>("FearHud/FearBar");
		SetupStepSound();
		SetupFear();
	}

	public override void _PhysicsProcess(double delta)
	{
		float deltaF = (float)delta;
		UpdateFear(deltaF);

		if (_fearMaxed)
		{
			Velocity = Vector2.Zero;
			_animatedSprite.Play("idle");
			UpdateStepSound(false);
			return;
		}

		Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Velocity = direction * Speed * SpeedModifier;

		if (direction != Vector2.Zero)
		{
			_animatedSprite.Play("walk");
			UpdateStepSound(true);
		}
		else
		{
			_animatedSprite.Play("idle");
			UpdateStepSound(false);
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
		UpdateStepSound(false);
	}

	private void StopHiding()
	{
		SpeedModifier = (int)SpeedModifierPredefined.Default;
		IsHiding = false;
		_animatedSprite.Visible = true;
		GetNode<CollisionShape2D>("CollisionShape2D").Disabled = false;
	}

	private void SetupFear()
	{
		_fear = 0f;
		_fearMaxed = false;
		_fearBar.MinValue = 0f;
		_fearBar.MaxValue = MaxFear;
		_fearBar.Value = 0f;
	}

	private DeadMenu ResolveDeadMenu()
	{
		if (_deadMenu != null && GodotObject.IsInstanceValid(_deadMenu))
		{
			return _deadMenu;
		}

		if (!DeadMenuPath.IsEmpty)
		{
			_deadMenu = GetNodeOrNull<DeadMenu>(DeadMenuPath);
			if (_deadMenu != null)
			{
				return _deadMenu;
			}
		}

		_deadMenu = GetTree().GetFirstNodeInGroup("dead_menu") as DeadMenu;
		return _deadMenu;
	}

	private void TriggerFearDeath()
	{
		_fearMaxed = true;
		_fear = MaxFear;
		_fearBar.Value = MaxFear;

		DeadMenu deadMenu = ResolveDeadMenu();
		if (deadMenu != null)
		{
			deadMenu.ShowDeadMenu();
			return;
		}

		GD.PushError("playerhumanmovement: не найден DeadMenu для экрана смерти.");
	}

	public void AddFearFromMonsterHit()
	{
		if (_fearMaxed)
		{
			return;
		}

		AddFearAmount(MaxFear * MonsterHitFearPercent / 100f);
	}

	private void AddFearAmount(float amount)
	{
		_fear = Mathf.Min(MaxFear, _fear + amount);
		_fearBar.Value = _fear;

		if (_fear >= MaxFear)
		{
			TriggerFearDeath();
		}
	}

	private void UpdateFear(float delta)
	{
		if (_fearMaxed || GetTree().Paused)
		{
			return;
		}

		if (IsNearBurningCampfire())
		{
			_fear = Mathf.Max(0f, _fear - CampfireFearDrainRate * delta);
		}
		else if (IsHiding)
		{
			_fear = Mathf.Max(0f, _fear - FearDrainRate * delta);
		}
		else
		{
			_fear = Mathf.Min(MaxFear, _fear + MaxFear * FearOutsideCampfirePercentPerSecond / 100f * delta);
		}

		_fearBar.Value = _fear;

		if (_fear >= MaxFear)
		{
			TriggerFearDeath();
		}
	}

	private bool IsNearBurningCampfire()
	{
		foreach (Node node in GetTree().GetNodesInGroup(CampfireGroup))
		{
			if (node is not Cumpfire campfire || !GodotObject.IsInstanceValid(campfire) || !campfire.fire)
			{
				continue;
			}

			float range = GetCampfireCalmRange(campfire);
			if (GlobalPosition.DistanceTo(campfire.GlobalPosition) <= range)
			{
				return true;
			}
		}

		return false;
	}

	private static float GetCampfireCalmRange(Cumpfire campfire)
	{
		Area2D calmArea = campfire.GetNodeOrNull<Area2D>("plus_saniyty_area");
		CollisionShape2D shapeNode = calmArea?.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (shapeNode?.Shape is CircleShape2D circle)
		{
			return circle.Radius;
		}

		return 122f;
	}

	private void SetupStepSound()
	{
		if (StepSound == null)
		{
			StepSound = GD.Load<AudioStream>("res://sounds/steps.ogg");
		}

		_stepsPlayer = new AudioStreamPlayer
		{
			Name = "StepsPlayer",
			Stream = StepSound,
			VolumeDb = StepVolumeDb
		};

		if (StepSound is AudioStreamOggVorbis ogg)
		{
			ogg.Loop = true;
		}

		AddChild(_stepsPlayer);
	}

	private void UpdateStepSound(bool isWalking)
	{
		if (_stepsPlayer == null || IsHiding || SpeedModifier == (int)SpeedModifierPredefined.Still)
		{
			if (_stepsPlayer != null && _stepsPlayer.Playing)
			{
				_stepsPlayer.Stop();
			}

			return;
		}

		if (isWalking)
		{
			if (!_stepsPlayer.Playing)
			{
				_stepsPlayer.Play();
			}
		}
		else if (_stepsPlayer.Playing)
		{
			_stepsPlayer.Stop();
		}
	}
}
