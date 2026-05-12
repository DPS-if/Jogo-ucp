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

	// Referências da Interface
	private RayCast3D _cameraRay;
	private CanvasLayer _photoUI;
	private TextureRect _photoDisplay;
	private Button _btnSim;
	private Button _btnNao;
	private Label _scoreLabel;

	// === NOVAS VARIÁVEIS DO SISTEMA DE PONTOS E VALIDAÇÃO ===
	private int _especiesFotografadas = 0;
	private Animal _animalFocado; // Guarda o script do animal que está sendo fotografado agora

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
		_scoreLabel = GetNode<Label>("HUD/ScoreLabel");
		
		// Inicializa o texto
		AtualizarTextoHUD();

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
			if (_cameraRay.IsColliding())
			{
				Node colisor = (Node)_cameraRay.GetCollider();
				
				// NOVO: Em vez de verificar apenas o grupo, verificamos se o colisor
				// (ou o pai do colisor) possui o script "Animal" anexado.
				if (colisor is Animal animalDetectado)
				{
					_animalFocado = animalDetectado; // Salva o animal para usarmos nos botões
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
		GetTree().Paused = true;
		
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		Image imagemCapturada = GetViewport().GetTexture().GetImage();
		ImageTexture texturaFoto = ImageTexture.CreateFromImage(imagemCapturada);
		
		_photoDisplay.Texture = texturaFoto;
		_photoUI.Show(); 

		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	// === LÓGICA DE VALIDAÇÃO DAS RESPOSTAS ===

	private void OnBtnSimPressed()
	{
		// O jogador respondeu que SIM, É INVASOR.
		// Isso significa que para ele acertar, o animal NÃO pode ser nativo (IsNativo = false).
		if (_animalFocado != null && _animalFocado.IsNativo == false)
		{
			Acertou();
		}
		else
		{
			Errou();
		}
	}

	private void OnBtnNaoPressed()
	{
		// O jogador respondeu que NÃO É INVASOR.
		// Isso significa que para ele acertar, o animal DEVE ser nativo (IsNativo = true).
		if (_animalFocado != null && _animalFocado.IsNativo == true)
		{
			Acertou();
		}
		else
		{
			Errou();
		}
	}

	private void AtualizarTextoHUD()
	{
		_scoreLabel.Text = $"Espécies: " + _especiesFotografadas + "/6";
	}
	
	private void Acertou()
	{
		if(_especiesFotografadas < 6)
		{
			_especiesFotografadas++;
		}
		GD.Print($"Correto! Espécies fotografadas: {_especiesFotografadas}");
		FecharTelaDeFoto();
		AtualizarTextoHUD();
	}

	private void Errou()
	{
		GD.Print("Resposta Errada! Reiniciando o jogo...");
		// Despausar é essencial antes de recarregar a cena, senão o jogo volta congelado!
		GetTree().Paused = false; 
		GetTree().ReloadCurrentScene();
	}

	private void FecharTelaDeFoto()
	{
		_photoUI.Hide();
		GetTree().Paused = false;
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}
}
