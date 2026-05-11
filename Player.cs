using Godot;
using System;

public partial class Player : CharacterBody3D
{
	[Export] public float WalkSpeed = 5.0f;
	[Export] public float SprintSpeed = 8.0f;
	[Export] public float MouseSensitivity = 0.002f;

	private Node3D _head;
	private Camera3D _camera;
	private float _currentSpeed;

	// Referências para o RayCast e Interface da Foto
	private RayCast3D _cameraRay;
	private CanvasLayer _photoUI;
	private TextureRect _photoDisplay;
	private Button _btnSim;
	private Button _btnNao;

	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	public override void _Ready()
	{
		_head = GetNode<Node3D>("Head");
		_camera = GetNode<Camera3D>("Head/Camera3D");
		_cameraRay = GetNode<RayCast3D>("Head/Camera3D/RayCast3D");
		
		_photoUI = GetNode<CanvasLayer>("PhotoUI");
		_photoDisplay = GetNode<TextureRect>("PhotoUI/PhotoDisplay");
		_btnSim = GetNode<Button>("PhotoUI/BtnSim");
		_btnNao = GetNode<Button>("PhotoUI/BtnNao");

		// Estado inicial
		_photoUI.Hide();
		
		_btnSim.Pressed += OnBtnSimPressed;
		_btnNao.Pressed += OnBtnNaoPressed;

		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Input(InputEvent @event)
	{
		if (GetTree().Paused) return;

		if (@event is InputEventMouseMotion mouseMotion)
		{
			RotateY(-mouseMotion.Relative.X * MouseSensitivity);
			
			Vector3 headRotation = _head.Rotation;
			headRotation.X -= mouseMotion.Relative.Y * MouseSensitivity;
			headRotation.X = Mathf.Clamp(headRotation.X, Mathf.DegToRad(-89), Mathf.DegToRad(89));
			_head.Rotation = headRotation;
		}
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("ui_accept") && !GetTree().Paused)
		{
			// Verifica se o RayCast está colidindo com algo
			if (_cameraRay.IsColliding())
			{
				Node colisor = (Node)_cameraRay.GetCollider();
				// Só tira a foto se o objeto estiver no grupo "Animal"
				if (colisor.IsInGroup("Animal"))
				{
					_ = TirarFoto();
				}
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GetTree().Paused) return;

		Vector3 velocity = Velocity;
		if (!IsOnFloor())
			velocity.Y -= gravity * (float)delta;

		if (Input.IsActionPressed("ui_sprint") || Input.IsKeyPressed(Key.Shift))
			_currentSpeed = SprintSpeed;
		else
			_currentSpeed = WalkSpeed;

		Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * _currentSpeed;
			velocity.Z = direction.Z * _currentSpeed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, _currentSpeed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, _currentSpeed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private async System.Threading.Tasks.Task TirarFoto()
	{
		// 1. Pausa o jogo
		GetTree().Paused = true;
		
		// 2. Captura a imagem
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		Image imagemCapturada = GetViewport().GetTexture().GetImage();
		ImageTexture texturaFoto = ImageTexture.CreateFromImage(imagemCapturada);
		
		// 3. Exibe a interface com a foto
		_photoDisplay.Texture = texturaFoto;
		_photoUI.Show(); 

		// 4. Libera o mouse para a pergunta
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	private void OnBtnSimPressed() => FecharTelaDeFoto();
	private void OnBtnNaoPressed() => FecharTelaDeFoto();

	private void FecharTelaDeFoto()
	{
		_photoUI.Hide();
		GetTree().Paused = false;
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}
}
