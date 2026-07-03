using Godot;

public partial class Monster : CharacterBody2D
{
	public enum State
	{
		Enter,
		Wander,
		Chase,
		Leave
	}

	public enum MapSide
	{
		Top,
		Bottom,
		Left,
		Right
	}

	[Signal]
	public delegate void StateChangedEventHandler(State newState);

	[Signal]
	public delegate void LeftMapEventHandler();

	[ExportGroup("Movement")]
	[Export] public float EnterSpeed = 180f;
	[Export] public float WanderSpeed = 90f;
	[Export] public float ChaseSpeed = 320f;
	[Export] public float LeaveSpeed = 220f;
	[Export] public float ArriveDistance = 12f;
	[Export] public bool RotateTowardMovement = false;

	[ExportGroup("Spawn")]
	[Export] public MapSide EntrySide = MapSide.Top;
	[Export] public float EntryOffset = 80f;

	[ExportGroup("Wander")]
	[Export] public Rect2 WanderArea = new(100, 100, 400, 300);
	[Export] public NodePath WanderBoundsPath;
	[Export] public float WanderWaitTime = 1.5f;
	[Export] public float WanderEdgeMargin = 24f;

	[ExportGroup("Vision")]
	[Export] public float VisionRange = 280f;
	[Export] public float LosePlayerRange = 420f;
	[Export] public StringName PlayerGroup = "player";

	[ExportGroup("Leave")]
	[Export] public MapSide LeaveSide = MapSide.Right;
	[Export] public float LeaveOffset = 100f;
	
	[ExportGroup("Attack")]
	[Export] public float BiteDistance = 45f;
	[Export] public float BiteCooldown = 1.2f;

	private AnimatedSprite2D _sprite;
	private bool _isBiting = false;
	private float _biteTimer = 0f;

	private State _state = State.Enter;
	private Vector2 _wanderTarget;
	private float _wanderTimer;
	private Node2D _player;
	private Vector2 _leaveTarget;

	public State GetState() => _state;

	public override void _Ready()
	{
		RefreshWanderAreaFromNode();
		GlobalPosition = GetSpawnPositionOutside();
		_leaveTarget = GetLeavePositionOutside();
		PickWanderTarget();
		
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.AnimationFinished += OnAnimationFinished;
		PlayAnim("idle");
	}

	public override void _PhysicsProcess(double delta)
	{
		float deltaF = (float)delta;

		if (_biteTimer > 0f)
		{
			_biteTimer -= deltaF;
		}

		switch (_state)
		{
			case State.Enter:
				ProcessEnter();
				break;
			case State.Wander:
				ProcessWander(deltaF);
				break;
			case State.Chase:
				ProcessChase();
				break;
			case State.Leave:
				ProcessLeave();
				break;
		}
	}

	public void SetWanderArea(Rect2 area)
	{
		WanderArea = area;
		if (_state == State.Wander)
		{
			PickWanderTarget();
		}
	}

	public void SetWanderAreaFromNode(Area2D areaNode)
	{
		WanderArea = GlobalRectFromArea2D(areaNode);
		if (_state == State.Wander)
		{
			PickWanderTarget();
		}
	}

	public void StartLeaving(MapSide? side = null)
	{
		LeaveSide = side ?? LeaveSide;
		_leaveTarget = GetLeavePositionOutside();
		SetState(State.Leave);
	}

	private void SetState(State newState)
	{
		if (_state == newState)
		{
			return;
		}

		_state = newState;
		EmitSignal(SignalName.StateChanged, (int)newState);

		if (newState == State.Wander)
		{
			PickWanderTarget();
			_wanderTimer = 0f;
		}
	}

	private void ProcessEnter()
	{
		Vector2 entryPoint = GetEntryPointOnMap();
		MoveToward(entryPoint, EnterSpeed);

		if (GlobalPosition.DistanceTo(entryPoint) <= ArriveDistance)
		{
			SetState(State.Wander);
		}
	}

	private void ProcessWander(float delta)
	{
		_player = FindPlayerInVision();
		if (_player != null)
		{
			SetState(State.Chase);
			return;
		}

		MoveToward(_wanderTarget, WanderSpeed);

		if (GlobalPosition.DistanceTo(_wanderTarget) <= ArriveDistance)
		{
			_wanderTimer += delta;
			if (_wanderTimer >= WanderWaitTime)
			{
				PickWanderTarget();
				_wanderTimer = 0f;
			}
		}
	}

	private void ProcessChase()
	{
		if (!GodotObject.IsInstanceValid(_player) || !IsPlayerVisible(_player))
		{
			_player = FindPlayerInVision();
		}

		if (_player == null)
		{
			SetState(State.Wander);
			return;
		}

		float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);

		if (distance > LosePlayerRange)
		{
			_player = null;
			SetState(State.Wander);
			return;
		}

		if (distance <= BiteDistance)
		{
			Bite();
			return;
		}

		MoveToward(_player.GlobalPosition, ChaseSpeed);
	}
	
	private void Bite()
{
	Velocity = Vector2.Zero;
	MoveAndSlide();

	if (_biteTimer > 0f || _isBiting)
	{
		return;
	}

	_isBiting = true;
	_biteTimer = BiteCooldown;

	PlayAnim("bite");

	GD.Print("Монстр кусает игрока");
}

	private void ProcessLeave()
	{
		MoveToward(_leaveTarget, LeaveSpeed);

		if (GlobalPosition.DistanceTo(_leaveTarget) <= ArriveDistance)
		{
			EmitSignal(SignalName.LeftMap);
			QueueFree();
		}
	}

	private void MoveToward(Vector2 target, float speed)
	{
		Vector2 offset = target - GlobalPosition;

		if (offset.Length() <= ArriveDistance)
		{
			Velocity = Vector2.Zero;

			if (!_isBiting)
			{
				PlayAnim("idle");
			}
		}
		else
		{
			Velocity = offset.Normalized() * speed;

			if (RotateTowardMovement)
			{
				Rotation = offset.Angle();
			}

			if (!_isBiting)
			{
				PlayAnim("run");
			}
		}

		MoveAndSlide();
	}

	private void PickWanderTarget()
	{
		Rect2 inner = GetInnerWanderRect();
		_wanderTarget = new Vector2(
			(float)GD.RandRange(inner.Position.X, inner.End.X),
			(float)GD.RandRange(inner.Position.Y, inner.End.Y)
		);
	}

	private static bool IsPlayerVisible(Node2D player)
	{
		return player is not playerhumanmovement human || !human.IsHiding;
	}

	private Node2D FindPlayerInVision()
	{
		Node2D closest = null;
		float closestDistance = VisionRange;

		foreach (Node node in GetTree().GetNodesInGroup(PlayerGroup))
		{
			if (node is Node2D player && IsPlayerVisible(player))
			{
				float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
				if (distance <= closestDistance)
				{
					closestDistance = distance;
					closest = player;
				}
			}
		}

		return closest;
	}

	private void RefreshWanderAreaFromNode()
	{
		if (WanderBoundsPath.IsEmpty)
		{
			return;
		}

		Node node = GetNodeOrNull(WanderBoundsPath);
		if (node is Area2D area)
		{
			SetWanderAreaFromNode(area);
		}
	}

	private Rect2 GetInnerWanderRect()
	{
		Rect2 inner = WanderArea;
		inner.Position += Vector2.One * WanderEdgeMargin;
		inner.Size -= Vector2.One * WanderEdgeMargin * 2f;

		if (inner.Size.X < 1f || inner.Size.Y < 1f)
		{
			return WanderArea;
		}

		return inner;
	}

	private Vector2 GetEntryPointOnMap()
	{
		Rect2 inner = GetInnerWanderRect();
		Vector2 center = inner.GetCenter();

		return EntrySide switch
		{
			MapSide.Top => new Vector2(center.X, inner.Position.Y),
			MapSide.Bottom => new Vector2(center.X, inner.End.Y),
			MapSide.Left => new Vector2(inner.Position.X, center.Y),
			MapSide.Right => new Vector2(inner.End.X, center.Y),
			_ => center
		};
	}

	private Vector2 GetSpawnPositionOutside()
	{
		Vector2 entryPoint = GetEntryPointOnMap();

		return EntrySide switch
		{
			MapSide.Top => entryPoint + new Vector2(0f, -EntryOffset),
			MapSide.Bottom => entryPoint + new Vector2(0f, EntryOffset),
			MapSide.Left => entryPoint + new Vector2(-EntryOffset, 0f),
			MapSide.Right => entryPoint + new Vector2(EntryOffset, 0f),
			_ => entryPoint
		};
	}

	private Vector2 GetLeavePositionOutside()
	{
		Rect2 inner = GetInnerWanderRect();
		Vector2 center = inner.GetCenter();

		return LeaveSide switch
		{
			MapSide.Left => new Vector2(inner.Position.X - LeaveOffset, center.Y),
			MapSide.Right => new Vector2(inner.End.X + LeaveOffset, center.Y),
			MapSide.Top => new Vector2(center.X, inner.Position.Y - LeaveOffset),
			MapSide.Bottom => new Vector2(center.X, inner.End.Y + LeaveOffset),
			_ => GlobalPosition
		};
	}

	private Rect2 GlobalRectFromArea2D(Area2D areaNode)
	{
		CollisionShape2D shapeNode = areaNode.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (shapeNode?.Shape == null)
		{
			GD.PushWarning("Monster: у Area2D нет CollisionShape2D, используется WanderArea из инспектора.");
			return WanderArea;
		}

		Rect2 localRect = shapeNode.Shape.GetRect();
		Vector2 globalPos = shapeNode.GlobalTransform * localRect.Position;
		Vector2 globalEnd = shapeNode.GlobalTransform * localRect.End;
		return new Rect2(globalPos, globalEnd - globalPos);
	}
	
	private void PlayAnim(string animName)
	{
		if (_sprite == null)
		{
			return;
		}

		if (_sprite.SpriteFrames == null || !_sprite.SpriteFrames.HasAnimation(animName))
		{
			return;
		}

		if (_sprite.Animation == animName && _sprite.IsPlaying())
		{
			return;
		}

		_sprite.Play(animName);
	}

	private void OnAnimationFinished()
	{
		if (_sprite.Animation == "bite")
		{
			_isBiting = false;

			if (Velocity.Length() > 5f)
			{
				PlayAnim("run");
			}
			else
			{
				PlayAnim("idle");
			}
		}
	}
}
