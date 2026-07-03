using Godot;
using System.Collections.Generic;

public partial class MonsterEncounterController : Node
{
	[Export] public PackedScene MonsterScene;
	[Export] public NodePath PlayerPath;
	[Export] public NodePath WanderBoundsPath;

	[ExportGroup("Timing")]
	[Export] public float FirstEncounterMinDelay = 8f;
	[Export] public float FirstEncounterMaxDelay = 15f;
	[Export] public float RespawnMinDelay = 15f;
	[Export] public float RespawnMaxDelay = 40f;

	[ExportGroup("Audio")]
	[Export] public AudioStream SkitzSound;
	[Export] public AudioStream HeartSound;
	[Export] public AudioStream BackgroundMusic;
	[Export] public float SkitzVolumeDb = -10f;
	[Export] public float HeartVolumeDb = 5f;
	[Export] public float BackgroundMusicVolumeDb = -6f;

	private Node2D _player;
	private Monster _activeMonster;
	private AudioStreamPlayer _skitzPlayer;
	private AudioStreamPlayer _heartPlayer;
	private AudioStreamPlayer _musicPlayer;
	private Timer _encounterTimer;
	private bool _isFirstEncounter = true;
	private bool _encounterInProgress;
	private readonly List<Cumpfire> _campfires = new();

	public override void _Ready()
	{
		_player = GetNodeOrNull<Node2D>(PlayerPath);
		if (_player == null)
		{
			GD.PushError("MonsterEncounterController: не найден игрок по PlayerPath.");
			return;
		}

		CacheCampfires();

		if (MonsterScene == null)
		{
			MonsterScene = GD.Load<PackedScene>("res://scenes/monster.tscn");
		}

		if (SkitzSound == null)
		{
			SkitzSound = GD.Load<AudioStream>("res://sounds/skitz.ogg");
		}

		if (HeartSound == null)
		{
			HeartSound = GD.Load<AudioStream>("res://sounds/heart.ogg");
		}

		if (BackgroundMusic == null)
		{
			BackgroundMusic = GD.Load<AudioStream>("res://sounds/anxious_1.ogg");
		}

		_skitzPlayer = CreateAudioPlayer("SkitzPlayer", SkitzSound, SkitzVolumeDb, false);
		_heartPlayer = CreateAudioPlayer("HeartPlayer", HeartSound, HeartVolumeDb, true);
		_musicPlayer = CreateAudioPlayer("BackgroundMusic", BackgroundMusic, BackgroundMusicVolumeDb, true);

		_encounterTimer = new Timer { OneShot = true };
		_encounterTimer.Timeout += OnEncounterTimerTimeout;
		AddChild(_encounterTimer);

		StartBackgroundMusic();
		ScheduleNextEncounter();
	}

	public override void _Process(double delta)
	{
		if (_activeMonster == null || !GodotObject.IsInstanceValid(_activeMonster))
		{
			return;
		}

		bool nearby = _activeMonster.IsNearbyPlayer(_player);
		if (nearby)
		{
			StartHeart();
		}
		else
		{
			StopHeart();
		}
	}

	private AudioStreamPlayer CreateAudioPlayer(string name, AudioStream stream, float volumeDb, bool loop)
	{
		var player = new AudioStreamPlayer
		{
			Name = name,
			Stream = stream,
			VolumeDb = volumeDb
		};

		if (loop && stream is AudioStreamOggVorbis ogg)
		{
			ogg.Loop = true;
		}

		AddChild(player);
		return player;
	}

	private void ScheduleNextEncounter()
	{
		float minDelay = _isFirstEncounter ? FirstEncounterMinDelay : RespawnMinDelay;
		float maxDelay = _isFirstEncounter ? FirstEncounterMaxDelay : RespawnMaxDelay;
		_encounterTimer.WaitTime = (float)GD.RandRange(minDelay, maxDelay);
		_encounterTimer.Start();
	}

	private void OnEncounterTimerTimeout()
	{
		if (_encounterInProgress)
		{
			return;
		}

		StartEncounter();
	}

	private void StartEncounter()
	{
		if (_player == null)
		{
			return;
		}

		_encounterInProgress = true;
		_isFirstEncounter = false;
		StopBackgroundMusic();

		if (_skitzPlayer.Stream != null)
		{
			_skitzPlayer.Play();
		}

		SpawnMonsterNearPlayer();
	}

	private void SpawnMonsterNearPlayer()
	{
		_activeMonster = MonsterScene.Instantiate<Monster>();
		AddChild(_activeMonster);

		Area2D wanderBounds = GetNodeOrNull<Area2D>(WanderBoundsPath);
		if (wanderBounds != null)
		{
			_activeMonster.SetWanderAreaFromNode(wanderBounds);
		}

		_activeMonster.SetKnownCampfires(_campfires);

		var entrySide = (Monster.MapSide)(int)GD.RandRange(0, 3);
		_activeMonster.BeginEncounter(_player.GlobalPosition, entrySide);
		_activeMonster.LeftMap += OnMonsterLeftMap;
	}

	private void OnMonsterLeftMap()
	{
		StopSkitz();
		StopHeart();
		_activeMonster = null;
		_encounterInProgress = false;
		StartBackgroundMusic();
		ScheduleNextEncounter();
	}

	private void StartBackgroundMusic()
	{
		if (_musicPlayer.Playing)
		{
			return;
		}

		_musicPlayer.Play();
	}

	private void StopBackgroundMusic()
	{
		if (!_musicPlayer.Playing)
		{
			return;
		}

		_musicPlayer.Stop();
	}

	private void StopSkitz()
	{
		if (!_skitzPlayer.Playing)
		{
			return;
		}

		_skitzPlayer.Stop();
	}

	private void StartHeart()
	{
		if (_heartPlayer.Playing)
		{
			return;
		}

		_heartPlayer.Play();
	}

	private void StopHeart()
	{
		if (!_heartPlayer.Playing)
		{
			return;
		}

		_heartPlayer.Stop();
	}

	private void CacheCampfires()
	{
		_campfires.Clear();
		Node sceneRoot = GetTree().CurrentScene;
		if (sceneRoot == null)
		{
			return;
		}

		CollectCampfires(sceneRoot, _campfires);
	}

	private static void CollectCampfires(Node node, List<Cumpfire> campfires)
	{
		if (node is Cumpfire campfire)
		{
			campfires.Add(campfire);
		}

		foreach (Node child in node.GetChildren())
		{
			if (ShouldSkipCampfireSearch(child))
			{
				continue;
			}

			CollectCampfires(child, campfires);
		}
	}

	private static bool ShouldSkipCampfireSearch(Node node)
	{
		return node.Name == "Decorations"
			|| node.Name == "Trees"
			|| node.Name == "Bushes"
			|| node.Name == "PathDetails"
			|| node.Name == "Clearings";
	}
}
