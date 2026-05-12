using Godot;

public partial class MenuPrincipal : Control
{
	private Control _painelSplash;
	private Control _comoJogar;

	private Button _btnJogar;
	private Button _btnComoJogar;
	private Button _btnSair;
	private Button _btnVoltar;

	private const string CenaJogo = "res://mapa.tscn";

	public override void _Ready()
	{
		// Nomes exatos dos nós na sua cena
		_painelSplash = GetNode<Control>("PainelSplash");
		_comoJogar    = GetNode<Control>("ComoJogar");

		_btnJogar     = GetNode<Button>("PainelSplash/VBoxContainer/jogar");
		_btnComoJogar = GetNode<Button>("PainelSplash/VBoxContainer/ComoJogar");
		_btnSair      = GetNode<Button>("PainelSplash/VBoxContainer/Sair");
		_btnVoltar    = GetNode<Button>("ComoJogar/voltar");

		_btnJogar.Pressed     += OnBtnJogarPressed;
		_btnComoJogar.Pressed += OnBtnComoJogarPressed;
		_btnSair.Pressed      += OnBtnSairPressed;
		_btnVoltar.Pressed    += OnBtnVoltarPressed;

		// Começa mostrando só o menu principal
		_painelSplash.Show();
		_comoJogar.Hide();
	}

	private void OnBtnJogarPressed()
	{
		GetTree().ChangeSceneToFile(CenaJogo);
	}

	private void OnBtnComoJogarPressed()
	{
		_painelSplash.Hide();
		_comoJogar.Show();
	}

	private void OnBtnSairPressed()
	{
		GetTree().Quit();
	}

	private void OnBtnVoltarPressed()
	{
		_comoJogar.Hide();
		_painelSplash.Show();
	}
}
