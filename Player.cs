using Godot;
using System;

public partial class Player : CharacterBody3D
{
	[Export] public float WalkSpeed = 0.8f;
	[Export] public float SprintSpeed = 1.7f;
	[Export] public float MouseSensitivity = 0.002f;

	private Node3D _head;
	private Camera3D _camera;
	private float _currentSpeed;

	// Referências para a Mecânica de Foto
	private RayCast3D _cameraRay;
	private CanvasLayer _photoUI;
	private TextureRect _photoDisplay;
	private ColorRect _flash;
	private Button _btnSim;
	private Button _btnNao;

	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	public override void _Ready()
	{
		_head = GetNode<Node3D>("Head");
		_camera = GetNode<Camera3D>("Head/Camera3D");
		
		// Pegando as referências dos nós de foto
		_cameraRay = GetNode<RayCast3D>("Head/Camera3D/RayCast3D");
		_photoUI = GetNode<CanvasLayer>("PhotoUI");
		_photoDisplay = GetNode<TextureRect>("PhotoUI/PhotoDisplay");
		_flash = GetNode<ColorRect>("PhotoUI/Flash");
		_btnSim = GetNode<Button>("PhotoUI/BtnSim");
		_btnNao = GetNode<Button>("PhotoUI/BtnNao");

		// Esconde a UI de foto no início
		_photoUI.Hide();
		_flash.Hide();

		// Conecta os botões da UI através do código
		_btnSim.Pressed += OnBtnSimPressed;
		_btnNao.Pressed += OnBtnNaoPressed;

		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Input(InputEvent @event)
	{
		// Se o jogo estiver pausado, ignoramos os inputs de mouse da câmera
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

	public override async void _Process(double delta)
	{
		// Quando o jogador apertar Espaço (ui_accept por padrão) e o jogo não estiver pausado
		if (Input.IsActionJustPressed("ui_accept") && !GetTree().Paused)
		{
			await TirarFoto();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GetTree().Paused) return; // Não move se o jogo estiver pausado

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

	// === LÓGICA DA FOTOGRAFIA ===

	private async System.Threading.Tasks.Task TirarFoto()
	{
		// 1. Verifica se tem um animal no centro da câmera
		bool fotografouAnimal = false;
		if (_cameraRay.IsColliding())
		{
			Node colisor = (Node)_cameraRay.GetCollider();
			if (colisor.IsInGroup("Animal"))
			{
				fotografouAnimal = true;
				GD.Print("Um animal foi fotografado!");
			}
		}

		// Opcional: Se quiser que a tela de foto só abra SE acertar um animal,
		// você pode colocar um "if (!fotografouAnimal) return;" aqui.

		// 2. Captura a imagem atual da tela
		// Esperamos o fim do frame de renderização para garantir uma imagem limpa
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		Image imagemCapturada = GetViewport().GetTexture().GetImage();
		ImageTexture texturaFoto = ImageTexture.CreateFromImage(imagemCapturada);

		// 3. Aplica a foto na UI e mostra a tela
		_photoDisplay.Texture = texturaFoto;
		_photoUI.Show();

		// 4. Efeito de Flash
		_flash.Show();
		_flash.Modulate = new Color(1, 1, 1, 1); // Branco sólido
		Tween tween = GetTree().CreateTween();
		// Desvanece o alpha do flash para 0 em 0.5 segundos
		tween.TweenProperty(_flash, "modulate:a", 0.0f, 0.5f); 

		// 5. Pausa o jogo e libera o mouse para clicar nos botões
		GetTree().Paused = true;
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	// === RESPOSTAS DOS BOTÕES ===

	private void OnBtnSimPressed()
	{
		GD.Print("Jogador respondeu: SIM, é invasor.");
		FecharTelaDeFoto();
	}

	private void OnBtnNaoPressed()
	{
		GD.Print("Jogador respondeu: NÃO, não é invasor.");
		FecharTelaDeFoto();
	}

	private void FecharTelaDeFoto()
	{
		_photoUI.Hide();
		GetTree().Paused = false; // Despausa o jogo
		Input.MouseMode = Input.MouseModeEnum.Captured; // Prende o mouse novamente
	}
}
