using Godot;
using System;
using System.Threading.Tasks;

public partial class Player : CharacterBody3D
{
	[ExportGroup("Movement")]
	[Export] public float WalkSpeed = 1.2f;
	[Export] public float SprintSpeed = 2.0f;
	[Export] public float MouseSensitivity = 0.002f;

	[ExportGroup("Camera Movement (Juice)")]
	[Export] public float BobFreq = 5.0f; 
	[Export] public float BobAmp = 0.05f; 
	[Export] public float HandShakeIntensity = 0.05f; 

	private Node3D _head;
	private Camera3D _camera;
	private RayCast3D _cameraRay;
	private FastNoiseLite _noise = new FastNoiseLite();
	
	private float _bobCycle = 0.0f;
	private float _noiseTime = 0.0f;
	private float _currentSpeed;

	// Referências da Interface (UI)
	private CanvasLayer _photoUI;
	private TextureRect _photoDisplay;
	private Button _btnSim, _btnNao;
	private Label _scoreLabel;
	
	// Referências para a Tela de Morte
	private CanvasLayer _deathUI;
	private Button _btnRecomecar;

	// === NOVAS REFERÊNCIAS PARA A TELA DE VITÓRIA ===
	private CanvasLayer _victoryUI;
	private Button _btnSair;

	private int _especiesFotografadas = 0;
	private Animal _animalFocado;

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

		_deathUI = GetNode<CanvasLayer>("DeathUI");
		_btnRecomecar = GetNode<Button>("DeathUI/BtnRecomecar");

		// === CAPTURA OS NÓS DA VITÓRIA ===
		_victoryUI = GetNode<CanvasLayer>("VictoryUI");
		_btnSair = GetNode<Button>("VictoryUI/BtnSair");

		_noise.Seed = (int)GD.Randi();
		_noise.Frequency = 0.5f;
		_noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;

		// Esconde as telas no início
		_photoUI.Hide();
		_deathUI.Hide();
		_victoryUI.Hide(); // Garante que a tela de vitória comece escondida
		
		// Conexões
		_btnSim.Pressed += OnBtnSimPressed;
		_btnNao.Pressed += OnBtnNaoPressed;
		_btnRecomecar.Pressed += OnBtnRecomecarPressed;
		_btnSair.Pressed += OnBtnSairPressed; // Conecta o botão de sair da vitória

		Input.MouseMode = Input.MouseModeEnum.Captured;
		AtualizarTextoHUD();
	}

	public override void _Input(InputEvent @event)
	{
		if (GetTree().Paused) return;

		if (@event is InputEventMouseMotion mouseMotion)
		{
			RotateY(-mouseMotion.Relative.X * MouseSensitivity);
			Vector3 headRot = _head.Rotation;
			headRot.X -= mouseMotion.Relative.Y * MouseSensitivity;
			headRot.X = Mathf.Clamp(headRot.X, Mathf.DegToRad(-85), Mathf.DegToRad(85));
			_head.Rotation = headRot;
		}
	}

	public override void _Process(double delta)
	{
		if (GetTree().Paused) return;

		HandleCameraEffects((float)delta);

		if (Input.IsActionJustPressed("ui_accept"))
		{
			CheckPhotoCapture();
		}
	}

	private void HandleCameraEffects(float delta)
	{
		_noiseTime += delta * 10.0f;
		Vector2 horizontalVel = new Vector2(Velocity.X, Velocity.Z);
		float speedFraction = horizontalVel.Length() / SprintSpeed;

		Vector3 targetPos = Vector3.Zero;
		if (IsOnFloor() && horizontalVel.Length() > 0.1f)
		{
			_bobCycle += delta * horizontalVel.Length() * BobFreq;
			targetPos.Y = Mathf.Sin(_bobCycle) * BobAmp;
			targetPos.X = Mathf.Cos(_bobCycle * 0.5f) * BobAmp;
		}
		else
		{
			_bobCycle = 0;
		}

		float currentShake = HandShakeIntensity * (1.0f + speedFraction);
		targetPos.X += _noise.GetNoise2D(_noiseTime, 0) * currentShake;
		targetPos.Y += _noise.GetNoise2D(0, _noiseTime) * currentShake;

		_camera.Position = _camera.Position.Lerp(targetPos, delta * 10.0f);

		float tilt = _noise.GetNoise2D(_noiseTime * 0.5f, _noiseTime * 0.5f) * currentShake * 2.0f;
		Vector3 camRot = _camera.Rotation;
		camRot.Z = Mathf.LerpAngle(camRot.Z, Mathf.DegToRad(tilt), delta * 5.0f);
		_camera.Rotation = camRot;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GetTree().Paused) return;

		Vector3 velocity = Velocity;
		if (!IsOnFloor()) velocity.Y -= gravity * (float)delta;

		bool isSprinting = Input.IsActionPressed("ui_sprint") || Input.IsKeyPressed(Key.Shift);
		_currentSpeed = isSprinting ? SprintSpeed : WalkSpeed;

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

	private void CheckPhotoCapture()
	{
		if (_cameraRay.IsColliding())
		{
			var collider = _cameraRay.GetCollider();
			if (collider is Animal animal)
			{
				_animalFocado = animal;
				_ = TirarFoto();
			}
		}
	}

	private async Task TirarFoto()
	{
		GetTree().Paused = true;
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		
		Image img = GetViewport().GetTexture().GetImage();
		_photoDisplay.Texture = ImageTexture.CreateFromImage(img);
		_photoUI.Show();
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	private void OnBtnSimPressed() { if (_animalFocado != null && !_animalFocado.IsNativo) Acertou(); else Errou(); }
	private void OnBtnNaoPressed() { if (_animalFocado != null && _animalFocado.IsNativo) Acertou(); else Errou(); }
	
	private void Acertou() 
	{ 
		_especiesFotografadas = Mathf.Min(_especiesFotografadas + 1, 6);
		AtualizarTextoHUD(); 
		FecharTelaDeFoto(); 

		// === VERIFICA SE O JOGADOR GANHOU APÓS ACERTAR ===
		if (_especiesFotografadas >= 6)
		{
			Venceu();
		}
	}
	
	// === LÓGICA DA TELA DE VITÓRIA ===
	private void Venceu()
	{
		GD.Print("Jogador venceu! Abrindo tela de vitória...");
		_victoryUI.Show(); // Mostra a interface de vitória
		
		// Pausa o jogo e libera o mouse
		GetTree().Paused = true;
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	// === FUNÇÃO DO BOTÃO SAIR (REDIRECIONA PARA O MENU) ===
	private void OnBtnSairPressed()
	{
		GD.Print("Botão Sair pressionado. Retornando ao menu...");
		GetTree().Paused = false; // Despausa antes de mudar de cena
		
		// Muda para a cena do menu principal ("control.tscn" que está na pasta raiz "res://")
		GetTree().ChangeSceneToFile("res://control.tscn");
	}

	private void Errou() 
	{ 
		GD.Print("Resposta Errada! Abrindo tela de morte...");
		_photoUI.Hide(); 
		_deathUI.Show(); 
		GetTree().Paused = true; 
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	private void OnBtnRecomecarPressed()
	{
		GD.Print("Botão Recomeçar pressionado. Reiniciando a cena...");
		_deathUI.Hide();
		GetTree().Paused = false; 
		GetTree().ReloadCurrentScene();
	}

	private void FecharTelaDeFoto() 
	{ 
		_photoUI.Hide(); 
		GetTree().Paused = false; 
		Input.MouseMode = Input.MouseModeEnum.Captured; 
	}
	
	private void AtualizarTextoHUD() 
	{ 
		_scoreLabel.Text = $"Espécies: {_especiesFotografadas}/6"; 
	}
}
