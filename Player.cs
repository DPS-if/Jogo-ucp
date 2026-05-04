using Godot;
using System;

public partial class Player : CharacterBody3D
{
	[Export] public float Speed = 1.0f;
	[Export] public float MouseSensitivity = 0.002f;

	private Node3D _head;
	private Camera3D _camera;

	// Obtém a gravidade das configurações do projeto
	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	public override void _Ready()
	{
		_head = GetNode<Node3D>("Head");
		_camera = GetNode<Camera3D>("Head/Camera3D");
		
		// Captura o mouse para que ele não saia da janela
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion)
		{
			// Gira o corpo horizontalmente (Eixo Y)
			RotateY(-mouseMotion.Relative.X * MouseSensitivity);
			
			// Gira a cabeça verticalmente (Eixo X) com limite (Clamp)
			Vector3 headRotation = _head.Rotation;
			headRotation.X -= mouseMotion.Relative.Y * MouseSensitivity;
			headRotation.X = Mathf.Clamp(headRotation.X, Mathf.DegToRad(-89), Mathf.DegToRad(89));
			_head.Rotation = headRotation;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// Adiciona gravidade
		if (!IsOnFloor())
			velocity.Y -= gravity * (float)delta;

		// Movimentação WASD
		Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}
