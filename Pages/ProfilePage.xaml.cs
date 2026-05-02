using Microsoft.Maui.Controls.Shapes;
using System.Collections.ObjectModel;

namespace MiniTFG;

/// <summary>
/// Muestra el perfil del usuario conectado y sus recetas publicadas.
/// </summary>
public partial class ProfilePage : ContentPage
{
    public ObservableCollection<Receta> MisRecetas { get; set; } = new();
	public ProfilePage()
	{
		InitializeComponent();
		UsernameLabel.BindingContext = App.UsuarioActual;
		ListaMisRecetas.BindingContext = this;
	}

    // Cada vez que se abre el perfil se recargan datos frescos desde MySQL.
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (App.UsuarioActual == null)
        {
            await Shell.Current.GoToAsync("//login");
            return;
        }

        var api = new DatabaseService();
        App.UsuarioActual = await api.GetUsuarioByIdAsync(App.UsuarioActual.Id);

        if (App.UsuarioActual == null)
        {
            await Shell.Current.GoToAsync("//login");
            return;
        }

        // El XAML enlaza etiquetas e imágenes al usuario actual mediante BindingContext.
        UsernameLabel.BindingContext = App.UsuarioActual;
        MostrarEstrellas(App.UsuarioActual.ValoracionMedia);

        // Cargar recetas primero (provoca cambios de layout en la CollectionView)
        await CargarRecetas();

        // Poner imágenes AL FINAL, después de que el layout esté estable
        ProfileImage.Source = await api.GetImageSourceAsync(App.UsuarioActual.Foto, "user.png");
        BannerImage.Source = await api.GetImageSourceAsync(App.UsuarioActual.Banner, "opbanner.jpg");
    }

    private async void InicioClicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//home");
	}
	private async void RecetasClicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//recipes");
    }

    private async void PerfilClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//profile");
    }

	private async void AbrirTiendaClicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("shop");
    }

    private async void AbrirAjustesClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("settings");
    }

    // Carga solo las recetas publicadas por el usuario conectado.
    private async Task CargarRecetas()
    {
        // Solo se cargan las recetas cuyo UsuarioId coincide con el usuario conectado.
        var api = new DatabaseService();
        var lista = await api.GetRecetasAsync();

        if (lista == null)
            return;

        MisRecetas.Clear();
        foreach (var mia in lista.Where(r => r.UsuarioId == App.UsuarioActual.Id))
        {
            mia.ImagenSource = await api.GetImageSourceAsync(mia.Imagen, "recipes.png");
            MisRecetas.Add(mia);
        }
    }

    // Construye una estrella vacía y superpone una estrella llena recortada para soportar decimales.
    private Grid CrearEstrella(double porcentaje)
    {
        // Mantiene las estrellas con tamaño fijo para que el recorte proporcional sea correcto.
        const double STAR_SIZE = 30; // Debe coincidir con WidthRequest/HeightRequest
        
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
        // Dibuja 5 estrellas usando la media del usuario; los decimales se muestran como estrellas parcialmente rellenas.
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