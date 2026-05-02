using Microsoft.Maui.Controls.Shapes;
using System.Collections.ObjectModel;

namespace MiniTFG;

[QueryProperty(nameof(UsuarioId), "usuarioId")]
/// <summary>
/// Muestra el perfil público de un creador seleccionado desde una receta.
/// </summary>
public partial class OtherProfilePage : ContentPage
{
	public ObservableCollection<Receta> MisRecetas { get; set; } = new();

	private int _usuarioId;
	public int UsuarioId
	{
		get => _usuarioId;
		set
		{
			_usuarioId = value;
			CargarPerfil(value);
		}
	}

	public OtherProfilePage()
	{
		InitializeComponent();
		ListaMisRecetas.BindingContext = this;
	}

	    // Carga datos públicos del creador seleccionado y sus recetas.
    private async void CargarPerfil(int usuarioId)
	{
		var api = new DatabaseService();
		var usuario = await api.GetUsuarioByIdAsync(usuarioId);

		if (usuario == null)
			return;

		UsernameLabel.BindingContext = usuario;
		MostrarEstrellas(usuario.ValoracionMedia);

		// Cargar recetas primero (provoca cambios de layout en la CollectionView)
		await CargarRecetas(api, usuarioId);

		// Poner imágenes AL FINAL, después de que el layout esté estable
		ProfileImage.Source = await api.GetImageSourceAsync(usuario.Foto, "user.png");
		BannerImage.Source = await api.GetImageSourceAsync(usuario.Banner, "opbanner.jpg");
	}

	private async Task CargarRecetas(DatabaseService api, int usuarioId)
	{
        // Misma lógica que el perfil propio, pero filtrando por el usuario recibido por navegación.
        var lista = await api.GetRecetasAsync();

		if (lista == null)
			return;

		MisRecetas.Clear();
		foreach (var receta in lista.Where(r => r.UsuarioId == usuarioId))
		{
            receta.ImagenSource = await api.GetImageSourceAsync(receta.Imagen, "recipes.png");
			MisRecetas.Add(receta);
		}
	}

	// Utilidades visuales compartidas conceptualmente con ProfilePage para representar medias con estrellas.
	private Grid CrearEstrella(double porcentaje)
	{
		const double STAR_SIZE = 30;

		var grid = new Grid
		{
			WidthRequest = STAR_SIZE,
			HeightRequest = STAR_SIZE
		};

		var empty = new Image
		{
			Source = "starempty.png",
			Aspect = Aspect.Fill
		};

		var full = new Image
		{
			Source = "starfull.png",
			Aspect = Aspect.Fill
		};

		full.Clip = new RectangleGeometry
		{
			Rect = new Rect(0, 0, STAR_SIZE * porcentaje, STAR_SIZE)
		};

		grid.Children.Add(empty);
		grid.Children.Add(full);

		return grid;
	}

	private void MostrarEstrellas(double media)
	{
		EstrellasContainer.Children.Clear();

		for (int i = 1; i <= 5; i++)
		{
			double porcentaje;

			if (media >= i)
				porcentaje = 1;
			else if (media <= i - 1)
				porcentaje = 0;
			else
				porcentaje = media - (i - 1);

			EstrellasContainer.Children.Add(CrearEstrella(porcentaje));
		}
	}
}