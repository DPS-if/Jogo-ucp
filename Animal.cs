using Godot;

public partial class Animal : StaticBody3D
{
	[Export] public string NomeAnimal = "Animal";
	[Export] public bool IsNativo = true;

	public override void _Ready()
	{
		// Adiciona ao grupo para o RayCast do Player detectar
		AddToGroup("Animal");
	}
}
