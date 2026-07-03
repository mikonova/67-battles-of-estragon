using Godot;
using System;

public partial class Cumpfire : Node2D
{
	private bool _playerInside = false;
	public bool fire = false;
	public bool started = false;

	private AnimatedSprite2D _sprite;
	private Timer _timer;
	
	public override void _Ready()
{
	_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
	_timer = GetNode<Timer>("Timer");

	Area2D actArea = GetNode<Area2D>("act_area");
	actArea.BodyEntered += OnBodyEntered;
	actArea.BodyExited += OnBodyExited;

	_sprite.AnimationFinished += OnAnimationFinished;

	// стартовое состояние — костёр потушен
	_sprite.Play("idle");
}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
			if (_playerInside && Input.IsActionJustPressed("interact") && !started)
		{
			StartFire();
		}
	}
	private void OnBodyEntered(Node body)
	{
		if (body is MonsterPlayer)
		{
			_playerInside = true;
		}
	}

	private void OnBodyExited(Node body)
	{
		if (body is MonsterPlayer)
		{
			_playerInside = false;
		}
	}

	private void _on_timer_timeout()
	{
		_sprite.Play("end");
	}
	
	private void StartFire()
	{
		started = true;
		fire = true;

		_sprite.Play("start"); // разжиг только один раз
		_timer.Start();
	}
	private void OnAnimationFinished()
	{
		if (_sprite.Animation == "start")
		{
			_sprite.Play("default"); // горение
			_timer.Start();          // таймер начинается после разжига
		}
		else if (_sprite.Animation == "end")
		{
			started = false;
			fire = false;
		}
	}
}
