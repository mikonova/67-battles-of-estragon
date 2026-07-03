using Godot;
using System.Collections.Generic;

public partial class Monster : CharacterBody2D
{
	public enum State
	{
		Enter,
		Wander,
		Chase,
		Sweep,
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

	[ExportGroup("Mode")]
	[Export] public bool EncounterMode = true;

	[ExportGroup("Movement")]
	[Export] public float EnterSpeed = 180f;
	[Export] public float WanderSpeed = 90f;
	[Export] public float ChaseSpeed = 320f;
	[Export] public float SweepSpeed = 240f;
	[Export] public float LeaveSpeed = 280f;
	[Export] public float ArriveDistance = 12f;
	[Export] public bool RotateTowardMovement = false;

	[ExportGroup("Spawn")]
	[Export] public MapSide EntrySide = MapSide.Top;
	[Export] public float EntryOffset = 80f;
	[Export] public float EncounterSpawnDistance = 580f;
	[Export] public float EncounterLeaveDistance = 650f;

	[ExportGroup("Wander")]
	[Export] public Rect2 WanderArea = new(100, 100, 400, 300);
	[Export] public NodePath WanderBoundsPath;
	[Export] public float WanderWaitTime = 1.5f;
	[Export] public float WanderEdgeMargin = 24f;

	[ExportGroup("Sweep")]
	[Export] public float SweepRadius = 300f;
	[Export] public int SweepWaypointCount = 6;
	[Export] public float NearbyDistance = 400f;
	[Export] public float EncounterMaxDuration = 28f;
	[Export] public float SweepWaypointTimeout = 2.5f;

	[ExportGroup("Vision")]
	[Export] public float VisionRange = 280f;
	[Export] public float LosePlayerRange = 420f;
	[Export] public StringName PlayerGroup = "player";
	[Export] public float CampfireAvoidDistance = 170f;

	[ExportGroup("Leave")]
	[Export] public MapSide LeaveSide = MapSide.Right;
	[Export] public float LeaveOffset = 100f;

	[ExportGroup("Attack")]
	[Export] public float BiteDistance = 45f;
	[Export] public float BiteCooldown = 1.2f;

	private AnimatedSprite2D _sprite;
	private CollisionShape2D _collisionShape;
	private bool _isBiting;
	private float _biteTimer;

	private State _state = State.Enter;
	private Vector2 _wanderTarget;
	private float _wanderTimer;
	private Node2D _player;
	private Vector2 _leaveTarget;
	private Vector2 _encounterCenter;
	private readonly List<Vector2> _sweepWaypointOffsets = new();
	private readonly List<Cumpfire> _campfires = new();
	private int _sweepIndex;
	private float _encounterTimer;
	private float _sweepWaypointTimer;
	private Vector2 _sweepCheckPosition;

	public State GetState() => _state;

	public bool IsNearbyPlayer(Node2D player)
	{
		if (player == null || !Visible)
		{
			return false;
		}

		return GlobalPosition.DistanceTo(player.GlobalPosition) <= NearbyDistance;
	}

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		_sprite.AnimationFinished += OnAnimationFinished;
		PlayAnim("idle");

		if (EncounterMode)
		{
			SetEncounterDormant();
			return;
		}

		RefreshWanderAreaFromNode();
		GlobalPosition = GetSpawnPositionOutside();
		_leaveTarget = GetLeavePositionOutside();
		PickWanderTarget();
	}

	public override void _PhysicsProcess(double delta)
	{
		float deltaF = (float)delta;

		if (_biteTimer > 0f)
		{
			_biteTimer -= deltaF;
		}

		if (EncounterMode && _state != State.Leave && _encounterTimer >= EncounterMaxDuration)
		{
			StartLeaving();
			return;
		}

		if (EncounterMode && _state != State.Leave)
		{
			_encounterTimer += deltaF;
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
			case State.Sweep:
				ProcessSweep();
				break;
			case State.Leave:
				ProcessLeave();
				break;
		}
	}

	public void BeginEncounter(Vector2 center, MapSide entrySide)
	{
		_encounterCenter = center;
		EntrySide = entrySide;
		LeaveSide = GetOppositeSide(entrySide);
		_encounterTimer = 0f;
		_sweepWaypointTimer = 0f;
		_sweepCheckPosition = center;
		BuildSweepWaypoints();
		_sweepIndex = 0;
		_player = null;
		_leaveTarget = GetLeavePositionFromCenter();
		GlobalPosition = GetSpawnPositionNearCenter();
		SetEncounterActive();
		SetState(State.Enter);
	}

	public void SetKnownCampfires(IEnumerable<Cumpfire> campfires)
	{
		_campfires.Clear();

		if (campfires == null)
		{
			return;
		}

		foreach (Cumpfire campfire in campfires)
		{
			if (GodotObject.IsInstanceValid(campfire))
			{
				_campfires.Add(campfire);
			}
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
		if (_state == State.Leave)
		{
			return;
		}

		_player = null;

		if (EncounterMode)
		{
			UpdateEncounterCenter();
		}

		LeaveSide = side ?? LeaveSide;
		_leaveTarget = EncounterMode
			? GetLeavePositionFromCenter()
			: GetLeavePositionOutside();
		SetState(State.Leave);
	}

	private void SetEncounterDormant()
	{
		Visible = false;
		SetProcess(false);
		SetPhysicsProcess(false);
	}

	private void SetEncounterActive()
	{
		Visible = true;
		SetProcess(true);
		SetPhysicsProcess(true);
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

		if (newState == State.Leave)
		{
			SetGhostMovement(true);
		}
	}

	private void SetGhostMovement(bool enabled)
	{
		CollisionLayer = 0;
		CollisionMask = 0;

		if (_collisionShape != null)
		{
			_collisionShape.Disabled = enabled;
		}
	}

	private void ProcessEnter()
	{
		if (EncounterMode)
		{
			UpdateEncounterCenter();

			_player = FindPlayerInVision();
			if (_player != null)
			{
				SetState(State.Chase);
				return;
			}
		}

		Vector2 entryPoint = EncounterMode
			? GetEncounterEntryPoint()
			: GetEntryPointOnMap();
		MoveToward(entryPoint, EnterSpeed);

		if (GlobalPosition.DistanceTo(entryPoint) <= ArriveDistance)
		{
			SetState(EncounterMode ? State.Sweep : State.Wander);
		}
	}

	private void ProcessSweep()
	{
		UpdateEncounterCenter();

		_player = FindPlayerInVision();
		if (_player != null)
		{
			_sweepWaypointTimer = 0f;
			SetState(State.Chase);
			return;
		}

		if (_sweepWaypointOffsets.Count == 0)
		{
			StartLeaving();
			return;
		}

		Vector2 target = GetSweepWaypoint(_sweepIndex);
		MoveToward(target, SweepSpeed);

		_sweepWaypointTimer += (float)GetPhysicsProcessDeltaTime();
		bool arrived = GlobalPosition.DistanceTo(target) <= ArriveDistance;
		bool stalled = _sweepWaypointTimer >= SweepWaypointTimeout
			&& GlobalPosition.DistanceTo(_sweepCheckPosition) < 20f;

		if (arrived || stalled)
		{
			_sweepIndex++;
			_sweepWaypointTimer = 0f;
			_sweepCheckPosition = GlobalPosition;

			if (_sweepIndex >= _sweepWaypointOffsets.Count)
			{
				StartLeaving();
			}
		}
	}

	private void UpdateEncounterCenter()
	{
		Node2D player = FindPlayerInVision();
		if (player != null)
		{
			_encounterCenter = player.GlobalPosition;
		}
	}

	private Vector2 GetSweepWaypoint(int index)
	{
		return AdjustTargetAwayFromCampfires(_encounterCenter + _sweepWaypointOffsets[index]);
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
			SetState(GetIdleStateAfterChase());
			return;
		}

		if (IsPositionInBurningCampfireZone(_player.GlobalPosition))
		{
			_player = null;
			SetState(GetIdleStateAfterChase());
			return;
		}

		float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);

		if (distance > LosePlayerRange)
		{
			_player = null;
			SetState(GetIdleStateAfterChase());
			return;
		}

		if (distance <= BiteDistance)
		{
			Bite();
			return;
		}

		Vector2 chaseTarget = _player.GlobalPosition;
		if (IsPositionInBurningCampfireZone(chaseTarget)
			|| IsPositionInBurningCampfireZone(AdjustTargetAwayFromCampfires(chaseTarget)))
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			PlayAnim("idle");
			return;
		}

		MoveToward(chaseTarget, ChaseSpeed);
	}

	private State GetIdleStateAfterChase()
	{
		return EncounterMode ? State.Sweep : State.Wander;
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
		MoveTowardGhost(_leaveTarget, LeaveSpeed);

		float distance = GlobalPosition.DistanceTo(_leaveTarget);
		if (distance <= ArriveDistance || distance <= LeaveSpeed * (float)GetPhysicsProcessDeltaTime() * 2f)
		{
			EmitSignal(SignalName.LeftMap);
			QueueFree();
		}
	}

	private void MoveTowardGhost(Vector2 target, float speed)
	{
		Vector2 offset = target - GlobalPosition;

		if (offset.Length() <= ArriveDistance)
		{
			Velocity = Vector2.Zero;

			if (!_isBiting)
			{
				PlayAnim("idle");
			}

			return;
		}

		float step = speed * (float)GetPhysicsProcessDeltaTime();
		GlobalPosition += offset.Normalized() * Mathf.Min(step, offset.Length());
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

	private void MoveToward(Vector2 target, float speed)
	{
		if (_state != State.Leave)
		{
			target = AdjustTargetAwayFromCampfires(target);
		}

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
			Vector2 direction = offset.Normalized();
			float step = speed * (float)GetPhysicsProcessDeltaTime();
			Vector2 nextPosition = GlobalPosition + direction * Mathf.Min(step, offset.Length());

			if (_state != State.Leave && IsPositionInBurningCampfireZone(nextPosition))
			{
				Velocity = Vector2.Zero;

				if (!_isBiting)
				{
					PlayAnim("idle");
				}

				MoveAndSlide();
				return;
			}

			Velocity = direction * speed;

			if (RotateTowardMovement)
			{
				Rotation = direction.Angle();
			}

			if (!_isBiting)
			{
				PlayAnim("run");
			}
		}

		MoveAndSlide();
	}

	private void BuildSweepWaypoints()
	{
		_sweepWaypointOffsets.Clear();

		for (int i = 0; i < SweepWaypointCount; i++)
		{
			Vector2 offset = Vector2.Zero;

			for (int attempt = 0; attempt < 12; attempt++)
			{
				float angle = (float)GD.RandRange(0f, Mathf.Tau);
				float radius = (float)GD.RandRange(SweepRadius * 0.45f, SweepRadius);
				offset = Vector2.FromAngle(angle) * radius;

				if (!IsPositionInBurningCampfireZone(_encounterCenter + offset))
				{
					break;
				}
			}

			_sweepWaypointOffsets.Add(offset);
		}
	}

	private void PickWanderTarget()
	{
		Rect2 inner = GetInnerWanderRect();
		_wanderTarget = new Vector2(
			(float)GD.RandRange(inner.Position.X, inner.End.X),
			(float)GD.RandRange(inner.Position.Y, inner.End.Y)
		);
	}

	private bool IsPlayerVisible(Node2D player)
	{
		if (player is playerhumanmovement human && human.IsHiding)
		{
			return false;
		}

		return !IsPositionInBurningCampfireZone(player.GlobalPosition);
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

	private Vector2 AdjustTargetAwayFromCampfires(Vector2 target)
	{
		Vector2 adjusted = target;

		foreach (Cumpfire campfire in _campfires)
		{
			if (!IsBurningCampfire(campfire))
			{
				continue;
			}

			Vector2 campfirePos = campfire.GlobalPosition;
			Vector2 offset = adjusted - campfirePos;
			float distance = offset.Length();

			if (distance >= CampfireAvoidDistance)
			{
				continue;
			}

			adjusted = distance <= 0.01f
				? campfirePos + Vector2.Right * CampfireAvoidDistance
				: campfirePos + offset.Normalized() * CampfireAvoidDistance;
		}

		return adjusted;
	}

	private bool IsPositionInBurningCampfireZone(Vector2 position)
	{
		foreach (Cumpfire campfire in _campfires)
		{
			if (!IsBurningCampfire(campfire))
			{
				continue;
			}

			if (position.DistanceTo(campfire.GlobalPosition) < CampfireAvoidDistance)
			{
				return true;
			}
		}

		return false;
	}

	private static bool IsBurningCampfire(Cumpfire campfire)
	{
		return GodotObject.IsInstanceValid(campfire) && campfire.fire;
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

	private Vector2 GetSpawnPositionNearCenter()
	{
		Vector2 sideOffset = GetSideOffset(EntrySide, EncounterSpawnDistance);
		float jitter = (float)GD.RandRange(-120f, 120f);

		return EntrySide switch
		{
			MapSide.Top or MapSide.Bottom => _encounterCenter + sideOffset + new Vector2(jitter, 0f),
			_ => _encounterCenter + sideOffset + new Vector2(0f, jitter)
		};
	}

	private Vector2 GetEncounterEntryPoint()
	{
		Vector2 direction = (_encounterCenter - GlobalPosition).Normalized();
		if (direction == Vector2.Zero)
		{
			direction = -GetSideOffset(EntrySide, 1f).Normalized();
		}

		return _encounterCenter - direction * SweepRadius * 0.85f;
	}

	private Vector2 GetLeavePositionFromCenter()
	{
		Vector2 sideOffset = GetSideOffset(LeaveSide, EncounterLeaveDistance);
		float jitter = (float)GD.RandRange(-100f, 100f);

		return LeaveSide switch
		{
			MapSide.Top or MapSide.Bottom => _encounterCenter + sideOffset + new Vector2(jitter, 0f),
			_ => _encounterCenter + sideOffset + new Vector2(0f, jitter)
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

	private static Vector2 GetSideOffset(MapSide side, float distance)
	{
		return side switch
		{
			MapSide.Top => new Vector2(0f, -distance),
			MapSide.Bottom => new Vector2(0f, distance),
			MapSide.Left => new Vector2(-distance, 0f),
			MapSide.Right => new Vector2(distance, 0f),
			_ => Vector2.Zero
		};
	}

	private static MapSide GetOppositeSide(MapSide side)
	{
		return side switch
		{
			MapSide.Top => MapSide.Bottom,
			MapSide.Bottom => MapSide.Top,
			MapSide.Left => MapSide.Right,
			MapSide.Right => MapSide.Left,
			_ => MapSide.Right
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
