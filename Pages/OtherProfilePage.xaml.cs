using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace MiniTFG;

[QueryProperty(nameof(UsuarioId), "usuarioId")]
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

    private HashSet<int> _likesUsuario = new();
    private HashSet<int> _usuariosValorados = new();

    public OtherProfilePage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    private async void CargarPerfil(int usuarioId)
    {
        var api = new DatabaseService();
        var usuario = await api.GetUsuarioByIdAsync(usuarioId);

        if (usuario == null)
            return;

        UsernameLabel.Text = usuario.Nombre;
        SobreMiDescripcionLabel.Text = usuario.Descripcion ?? string.Empty;

        MostrarEstrellas(usuario.ValoracionMedia);

        await CargarRecetasAsync(usuarioId);

        ProfileImage.Source = await api.GetImageSourceAsync(usuario.Foto, "user.png");
        BannerImage.Source = await api.GetImageSourceAsync(usuario.Banner, "opbanner.jpg");

        ActivarBoton(BtnRecetasTab);
        MostrarRecetas();
    }

    private async Task CargarRecetasAsync(int usuarioId)
    {
        var api = new DatabaseService();

        MisRecetas.Clear();
        _likesUsuario.Clear();
        _usuariosValorados.Clear();

        // 🔥 Recuperamos el usuario visitado (necesario para CreadorNombre y CreadorFoto)
        var usuario = await api.GetUsuarioByIdAsync(usuarioId);

        var lista = await api.GetRecetasAsync();
        if (lista == null)
            return;

        var recetasUsuario = lista.Where(r => r.UsuarioId == usuarioId).ToList();

        var likesPorReceta = await api.GetLikesPorRecetaAsync(recetasUsuario.Select(r => r.Id));

        foreach (var r in recetasUsuario)
        {
            // 🔶 El creador es SIEMPRE el usuario visitado
            r.CreadorNombre = usuario.Nombre;
            r.CreadorFoto = usuario.Foto;
            r.CreadorFotoSource = await api.GetImageSourceAsync(usuario.Foto, "user.png");

            // 🔶 Likes
            r.Likes = likesPorReceta.TryGetValue(r.Id, out int totalLikes) ? totalLikes : 0;

            r.UsuarioHaDadoLike = _likesUsuario.Contains(r.Id);
            r.UsuarioHaValorado = _usuariosValorados.Contains(r.UsuarioId);

            // 🔶 Imagen de la receta
            r.ImagenSource = await api.GetImageSourceAsync(r.Imagen, "recipes.png");

            MisRecetas.Add(r);
        }
    }

    private void MostrarEstrellas(double media)
    {
        EstrellasContainer.Children.Clear();

        for (int i = 1; i <= 5; i++)
        {
            double porcentaje = media >= i ? 1 :
                                media <= i - 1 ? 0 :
                                media - (i - 1);

            EstrellasContainer.Children.Add(CrearEstrella(porcentaje));
        }
    }

    private Grid CrearEstrella(double porcentaje)
    {
        const double STAR_SIZE = 30;

        var grid = new Grid
        {
            WidthRequest = STAR_SIZE,
            HeightRequest = STAR_SIZE
        };

        var empty = new Image { Source = "starempty.png", Aspect = Aspect.Fill };
        var full = new Image { Source = "starfull.png", Aspect = Aspect.Fill };

        full.Clip = new RectangleGeometry
        {
            Rect = new Rect(0, 0, STAR_SIZE * porcentaje, STAR_SIZE)
        };

        grid.Children.Add(empty);
        grid.Children.Add(full);

        return grid;
    }

    // ---- TABS ----

    private void ActivarBoton(Button btn)
    {
        BtnRecetasTab.BackgroundColor = Color.FromArgb("#E09A3F");
        BtnSobreMi.BackgroundColor = Color.FromArgb("#E09A3F");

        btn.BackgroundColor = Color.FromArgb("#C87F2F");
    }

    private void MostrarRecetas()
    {
        RecetasView.IsVisible = true;
        SobreMiView.IsVisible = false;
    }

    private void MostrarSobreMi()
    {
        RecetasView.IsVisible = false;
        SobreMiView.IsVisible = true;
    }

    private void RecetasTabClicked(object sender, EventArgs e)
    {
        ActivarBoton(BtnRecetasTab);
        MostrarRecetas();
    }

    private void SobreMiTabClicked(object sender, EventArgs e)
    {
        ActivarBoton(BtnSobreMi);
        MostrarSobreMi();
    }

    // ---- BARRA INFERIOR ----

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

    // ---- FUNCIONALIDADES TARJETAS ----

    private async void LikeClicked(object sender, EventArgs e)
    {
        var api = new DatabaseService();
        var img = (Image)sender;
        var receta = (Receta)img.BindingContext;

        if (App.UsuarioActual == null)
        {
            await DisplayAlert("Debe iniciar sesión", "Inicie sesión para dar like.", "OK");
            return;
        }

        int usuarioId = App.UsuarioActual.Id;
        int recetaId = receta.Id;

        if (!receta.UsuarioHaDadoLike)
        {
            try
            {
                await api.PostLikeAsync(new Like { UsuarioId = usuarioId, RecetaId = recetaId });
                receta.UsuarioHaDadoLike = true;
                receta.Likes++;
                _likesUsuario.Add(recetaId);
            }
            catch
            {
                receta.UsuarioHaDadoLike = _likesUsuario.Contains(recetaId);
            }
        }
        else
        {
            try
            {
                await api.DeleteLikeAsync(usuarioId, recetaId);
                receta.UsuarioHaDadoLike = false;
                receta.Likes = Math.Max(0, receta.Likes - 1);
                _likesUsuario.Remove(recetaId);
            }
            catch
            {
                receta.UsuarioHaDadoLike = _likesUsuario.Contains(recetaId);
            }
        }

        img.Source = receta.IconoLike;
    }

    private async void PuntuarClicked(object sender, EventArgs e)
    {
        var img = (Image)sender;
        var receta = (Receta)img.BindingContext;

        if (App.UsuarioActual == null)
        {
            await DisplayAlert("Debe iniciar sesión", "Inicie sesión para valorar.", "OK");
            return;
        }

        var popup = new StarRatingPopup(receta.UsuarioId);
        var resultado = await this.ShowPopupAsync(popup);

        if (resultado is int estrellas)
        {
            receta.UsuarioHaValorado = true;
            _usuariosValorados.Add(receta.UsuarioId);
            img.Source = receta.IconoEstrella;
            await DisplayAlert("Gracias", $"Has valorado con {estrellas} estrellas", "OK");
        }
    }

    private async void OnRecetaTapped(object sender, EventArgs e)
    {
        var border = (Border)sender;
        var receta = (Receta)border.BindingContext;

        var popup = new RecipeDetailPopup(receta);
        var resultado = await this.ShowPopupAsync(popup);

        if (resultado is int[] ids && ids.Length == 2)
            await Shell.Current.GoToAsync($"recipesteps?recetaId={ids[0]}&usuarioId={ids[1]}");
    }
}
