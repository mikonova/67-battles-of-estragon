using Godot;

public partial class StressTransformController : Node
{
	[Export] public NodePath PlayerPath;
	[Export] public NodePath EncounterControllerPath;
	[Export] public NodePath DeadMenuPath;
	[Export] public NodePath BottomBarrierPath;

	[Export] public PackedScene MonsterPlayerScene;
	[Export] public PackedScene HumanNpcScene;

	[Export] public float DarkenDuration = 0.45f;
	[Export] public float CatchDistance = 48f;
	[Export] public float MonsterSpawnOffsetY = -75f;
	[Export] public Vector2 FleeingHumanIdleDirection = Vector2.Down;
	[Export] public float FleeingHumanWalkSpeed = 140f;
	[Export] public float FleeingHumanRunSpeed = 300f;
	[Export] public float FearAfterCatch = 0f;
	[Export] public float MapBottomTriggerOffset = 80f;

	private playerhumanmovement _player;
	private MonsterEncounterController _encounterController;
	private DeadMenu _deadMenu;
	private CanvasLayer _overlayLayer;
	private ColorRect _overlay;
	private MonsterPlayer _monsterPlayer;
	private HumanNpc _fleeingHuman;
	private camera2d _followCamera;
	private Tween _tween;
	private float _mapBottomY;
	private bool _transformed;
	private bool _transforming;
	private bool _monsterPhaseEnded;

	public override void _Ready()
	{
		_player = GetNodeOrNull<playerhumanmovement>(PlayerPath);
		if (_player == null)
		{
			GD.PushError("StressTransformController: не найден игрок.");
			return;
		}

		_encounterController = GetNodeOrNull<MonsterEncounterController>(EncounterControllerPath);
		_deadMenu = GetNodeOrNull<DeadMenu>(DeadMenuPath);
		_followCamera = _player.GetNode<camera2d>("Camera2D");
		CacheMapBottom();
		PreloadScenes();
		CallDeferred(MethodName.EnsureOverlay);

		_player.FearMaxed += OnPlayerFearMaxed;
	}

	private void PreloadScenes()
	{
		if (MonsterPlayerScene == null)
		{
			MonsterPlayerScene = GD.Load<PackedScene>("res://scenes/Monster_player.tscn");
		}

		if (HumanNpcScene == null)
		{
			HumanNpcScene = GD.Load<PackedScene>("res://scenes/human_npc.tscn");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_transformed || _transforming || _monsterPhaseEnded || _monsterPlayer == null || _fleeingHuman == null)
		{
			return;
		}

		if (!GodotObject.IsInstanceValid(_monsterPlayer) || !GodotObject.IsInstanceValid(_fleeingHuman))
		{
			return;
		}

		if (_monsterPlayer.GlobalPosition.DistanceTo(_fleeingHuman.GlobalPosition) <= CatchDistance)
		{
			OnHumanCaught();
		}
	}

	private void OnPlayerFearMaxed()
	{
		if (_transforming || _transformed)
		{
			return;
		}

		BeginTransform();
	}

	private void BeginTransform()
	{
		_transforming = true;
		_monsterPhaseEnded = false;

		_encounterController?.StopEncounters();
		HidePlayer();

		EnsureOverlay();
		_overlay.Color = new Color(0f, 0f, 0f, 0f);
		_overlayLayer.Visible = true;

		_tween?.Kill();
		_tween = CreateTween();
		_tween.TweenProperty(_overlay, "color", new Color(0f, 0f, 0f, 1f), DarkenDuration)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		_tween.TweenCallback(Callable.From(SwapToMonsterPhase));
		_tween.TweenProperty(_overlay, "color", new Color(0f, 0f, 0f, 0f), DarkenDuration)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		_tween.TweenCallback(Callable.From(FinishTransform));
	}

	private void HidePlayer()
	{
		_player.GetNode<CanvasLayer>("FearHud").Visible = false;
		_player.GetNode<AnimatedSprite2D>("AnimatedSprite2D").Visible = false;
		_player.GetNode<CollisionShape2D>("CollisionShape2D").Disabled = true;
		_player.ProcessMode = ProcessModeEnum.Disabled;
	}

	private void SwapToMonsterPhase()
	{
		Vector2 spawnPosition = _player.GlobalPosition;

		_monsterPlayer = MonsterPlayerScene.Instantiate<MonsterPlayer>();
		_monsterPlayer.GlobalPosition = spawnPosition + new Vector2(0f, MonsterSpawnOffsetY);
		AddChild(_monsterPlayer);

		Camera2D builtInCamera = _monsterPlayer.GetNodeOrNull<Camera2D>("Camera2D");
		if (builtInCamera != null)
		{
			builtInCamera.QueueFree();
		}

		_fleeingHuman = HumanNpcScene.Instantiate<HumanNpc>();
		_fleeingHuman.WalkSpeed = FleeingHumanWalkSpeed;
		_fleeingHuman.RunAwaySpeed = FleeingHumanRunSpeed;
		_fleeingHuman.IdleMoveDirection = FleeingHumanIdleDirection;
		_fleeingHuman.EscapeBottomY = _mapBottomY;
		_fleeingHuman.EscapedToBottom += OnNpcEscaped;
		_fleeingHuman.GlobalPosition = spawnPosition;
		AddChild(_fleeingHuman);

		_followCamera?.Retarget(_monsterPlayer);
		_followCamera?.MakeCurrent();
		_encounterController?.PlayTransformMusic();
	}

	private void FinishTransform()
	{
		_transforming = false;
		_transformed = true;

		if (_overlayLayer != null)
		{
			_overlayLayer.Visible = false;
		}
	}

	private void OnHumanCaught()
	{
		if (_monsterPhaseEnded)
		{
			return;
		}

		_monsterPhaseEnded = true;
		_transforming = true;
		Vector2 humanPosition = _fleeingHuman.GlobalPosition;

		DisconnectFleeingHuman();
		CleanupMonsterPhase();

		_player.RestoreFromTransform(humanPosition, FearAfterCatch);
		_followCamera?.Retarget(_player);
		_followCamera?.MakeCurrent();

		_encounterController?.ResumeEncounters();

		_transformed = false;
		_transforming = false;
	}

	private void OnNpcEscaped()
	{
		if (_monsterPhaseEnded || _transforming)
		{
			return;
		}

		_monsterPhaseEnded = true;
		_transforming = true;

		DisconnectFleeingHuman();
		CleanupMonsterPhase();
		_encounterController?.StopMusic();
		_followCamera?.Retarget(_player);

		if (_deadMenu != null)
		{
			_deadMenu.ShowDeadMenu();
		}
		else
		{
			GD.PushError("StressTransformController: не найден DeadMenu после побега NPC.");
		}

		_transformed = false;
		_transforming = false;
	}

	private void DisconnectFleeingHuman()
	{
		if (_fleeingHuman != null && GodotObject.IsInstanceValid(_fleeingHuman))
		{
			_fleeingHuman.EscapedToBottom -= OnNpcEscaped;
		}
	}

	private void CleanupMonsterPhase()
	{
		if (_monsterPlayer != null && GodotObject.IsInstanceValid(_monsterPlayer))
		{
			_monsterPlayer.QueueFree();
			_monsterPlayer = null;
		}

		if (_fleeingHuman != null && GodotObject.IsInstanceValid(_fleeingHuman))
		{
			_fleeingHuman.QueueFree();
			_fleeingHuman = null;
		}
	}

	private void CacheMapBottom()
	{
		CollisionShape2D bottomBarrier = GetNodeOrNull<CollisionShape2D>(BottomBarrierPath);
		if (bottomBarrier != null)
		{
			float barrierHalfHeight = 0f;
			if (bottomBarrier.Shape is RectangleShape2D rectangle)
			{
				barrierHalfHeight = rectangle.Size.Y * 0.5f;
			}

			float barrierTopY = bottomBarrier.GlobalPosition.Y - barrierHalfHeight;
			_mapBottomY = barrierTopY - MapBottomTriggerOffset;
			return;
		}

		_mapBottomY = 6360f;
	}

	private void EnsureOverlay()
	{
		if (_overlayLayer != null)
		{
			return;
		}

		_overlayLayer = new CanvasLayer
		{
			Layer = 90,
			ProcessMode = ProcessModeEnum.Always,
			Visible = false
		};
		AddChild(_overlayLayer);

		_overlay = new ColorRect
		{
			Color = new Color(0f, 0f, 0f, 0f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		_overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_overlayLayer.AddChild(_overlay);
	}
}
