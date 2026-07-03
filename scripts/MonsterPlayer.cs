using Godot;
using System;

public partial class MonsterPlayer : CharacterBody2D
{
	public const float Speed = 350.0f;
public const float StopDistance = 8.0f;

public const float Acceleration = 520.0f; // меньше = больше инерции при повороте
public const float Friction = 35.0f;      // меньше = дольше скользит

public override void _PhysicsProcess(double delta)
{
	float deltaF = (float)delta;

	Vector2 mousePosition = GetGlobalMousePosition();
	Vector2 direction = mousePosition - GlobalPosition;

	if (direction.Length() > StopDistance)
	{
		Vector2 targetVelocity = direction.Normalized() * Speed;

		// Плавно меняет скорость, поэтому есть инерция
		Velocity = Velocity.MoveToward(targetVelocity, Acceleration * deltaF);
	}
	else
	{
		// Не останавливается моментально, а чуть скользит
		Velocity = Velocity.MoveToward(Vector2.Zero, Friction * deltaF);
	}

	LookAt(mousePosition);
	MoveAndSlide();
}
}
