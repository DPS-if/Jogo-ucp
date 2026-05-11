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

	// Referências para o Flash Global
	private CanvasLayer _flashUI;
	private ColorRect _screenFlash;

	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	public override void _Ready()
	{
		// Pega as referências dos nós. Verifique se os nomes estão corretos na sua cena!
		_head = GetNode<Node3D>("Head");
		_camera = GetNode<Camera3D>("Head/Camera3D");
		_cameraRay = GetNode<RayCast3D>("Head/Camera3D/RayCast3D");
		
		_photoUI = GetNode<CanvasLayer>("PhotoUI");
		_photoDisplay = GetNode<TextureRect>("PhotoUI/PhotoDisplay");
		_btnSim = GetNode<Button>("PhotoUI/BtnSim");
		_btnNao = GetNode<Button>("PhotoUI/BtnNao");

		// Pega as novas referências do flash global
		_flashUI = GetNode<CanvasLayer>("FlashLayer");
		_screenFlash = GetNode<ColorRect>("FlashLayer/ScreenFlash");

		// Estado inicial das interfaces
		_photoUI.Hide();
		_flashUI.Hide(); // O flash global deve começar escondido
		
		// Configura o flash para ser branco e sólido antes do primeiro uso
		_screenFlash.Modulate = new Color(1, 1, 1, 1); 

		// Conecta os botões
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
		// Quando o jogador apertar Espaço (ui_accept) e o jogo não estiver pausado
		if (Input.IsActionJustPressed("ui_accept") && !GetTree().Paused)
		{
			// CORREÇÃO: Primeiro, verifica se tem um animal no foco do RayCast
			if (_cameraRay.IsColliding())
			{
				Node colisor = (Node)_cameraRay.GetCollider();
				if (colisor.IsInGroup("Animal"))
				{
					GD.Print("Um animal foi detectado. Preparando para tirar a foto!");
					// Chamar TirarFoto como async para evitar avisos
					_ = TirarFoto();
					return; // Para aqui, a foto foi iniciada
				}
			}
			// Se o código chegou aqui, é porque não havia animal no foco
			GD.Print("Não havia nenhum animal no foco do RayCast. Foto não realizada.");
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

	// === NOVA LÓGICA DA FOTOGRAFIA ===

	private async System.Threading.Tasks.Task TirarFoto()
	{
		// 1. Pausa o jogo imediatamente para congelar a cena
		GetTree().Paused = true;
		
		// 2. Espera o final do frame de renderização para capturar a imagem limpa
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		Image imagemCapturada = GetViewport().GetTexture().GetImage();
		ImageTexture texturaFoto = ImageTexture.CreateFromImage(imagemCapturada);
		
		// 3. Aplica a foto na interface de usuário (PhotoUI)
		_photoDisplay.Texture = texturaFoto;
		
		// 4. CORREÇÃO: Mostra o flash global e a PhotoUI com a foto
		// Garantimos que o flash esteja branco e sólido antes de mostrá-lo
		_screenFlash.Modulate = new Color(1, 1, 1, 1);
		_flashUI.Show(); // Cobre TUDO (incluindo os botões Sim/Não que ainda vamos mostrar)
		
		// Mostra a PhotoUI (que contém a foto e as perguntas)
		_photoUI.Show(); 

		// 5. Cria um Tween para fazer o flash sumir (fades out)
		Tween tween = GetTree().CreateTween();
		// Diminui o alpha (opacidade) do flash para 0 em 0.4 segundos (um flash rápido)
		tween.TweenProperty(_screenFlash, "modulate:a", 0.0f, 0.4f); 
		// Quando o flash sumir completamente, escondemos o CanvasLayer do flash
		await tween.Finished;
		_flashUI.Hide();

		// 6. Libera o mouse para interagir com a PhotoUI
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

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
