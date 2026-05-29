using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace MiniTFG;

public partial class ProfilePage : ContentPage
{
    public ObservableCollection<Receta> MisRecetas { get; set; } = new();

    private bool menuAbierto = false;

    private HashSet<int> _likesUsuario = new();
    private HashSet<int> _usuariosValorados = new();

    public ProfilePage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (App.UsuarioActual == null)
        {
            await Shell.Current.GoToAsync("//login");
            return;
        }

        var api = new DatabaseService();

        UsernameLabel.Text = App.UsuarioActual.Nombre;
        MostrarEstrellas(App.UsuarioActual.ValoracionMedia);

        SobreMiDescripcionLabel.Text = App.UsuarioActual.Descripcion ?? string.Empty;

        ProfileImage.Source = await api.GetImageSourceAsync(App.UsuarioActual.Foto, "user.png");
        BannerImage.Source = await api.GetImageSourceAsync(App.UsuarioActual.Banner, "opbanner.jpg");

        await CargarRecetasAsync();

        ActivarBoton(BtnRecetasTab);
        MostrarRecetas();
    }

    private async Task CargarRecetasAsync()
    {
        var api = new DatabaseService();
        int usuarioId = App.UsuarioActual.Id;

        MisRecetas.Clear();
        _likesUsuario.Clear();
        _usuariosValorados.Clear();

        var lista = await api.GetRecetasAsync();
        if (lista == null)
            return;

        // Solo recetas del usuario actual
        var propias = lista.Where(r => r.UsuarioId == usuarioId).ToList();

        // Likes y valoraciones del usuario actual
        try
        {
            var likesUsuario = await api.GetLikesUsuarioAsync(usuarioId);
            _likesUsuario = likesUsuario != null
                ? new HashSet<int>(likesUsuario.Select(l => l.RecetaId))
                : new HashSet<int>();

            var valoracionesHechas = await api.GetValoracionesPorUsuarioAsync(usuarioId);
            _usuariosValorados = valoracionesHechas != null
                ? new HashSet<int>(valoracionesHechas.Select(v => v.UsuarioValoradoId))
                : new HashSet<int>();
        }
        catch
        {
            _likesUsuario = new HashSet<int>();
            _usuariosValorados = new HashSet<int>();
        }

        var likesPorReceta = await api.GetLikesPorRecetaAsync(propias.Select(r => r.Id));

        foreach (var r in propias)
        {
            // Creador = usuario actual
            r.CreadorNombre = App.UsuarioActual.Nombre;
            r.CreadorFoto = App.UsuarioActual.Foto ?? "user.png";
            r.CreadorFotoSource = await api.GetImageSourceAsync(r.CreadorFoto, "user.png");

            r.Likes = likesPorReceta.TryGetValue(r.Id, out int totalLikes) ? totalLikes : 0;

            r.UsuarioHaDadoLike = _likesUsuario.Contains(r.Id);
            r.UsuarioHaValorado = _usuariosValorados.Contains(r.UsuarioId);

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

    // ---- NAVEGACIÓN BARRA INFERIOR ----

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

    // ---- MENÚ LATERAL ----

    private async void AbrirAjustesClicked(object sender, EventArgs e)
    {
        if (!menuAbierto)
            await AbrirMenu();
        else
            await CerrarMenu();
    }

    private async Task AbrirMenu()
    {
        menuAbierto = true;

        SideMenu.IsVisible = true;
        Overlay.IsVisible = true;

        SideMenu.TranslationX = 300;
        Overlay.Opacity = 0;

        await Task.WhenAll(
            SideMenu.TranslateTo(0, 0, 250, Easing.CubicOut),
            Overlay.FadeTo(1, 250)
        );
    }

    private async Task CerrarMenu()
    {
        menuAbierto = false;

        await Task.WhenAll(
            SideMenu.TranslateTo(300, 0, 250, Easing.CubicIn),
            Overlay.FadeTo(0, 250)
        );

        SideMenu.IsVisible = false;
        Overlay.IsVisible = false;
    }

    private async void CerrarMenuTapped(object sender, TappedEventArgs e)
    {
        if (menuAbierto)
            await CerrarMenu();
    }

    // ---- FUNCIONALIDADES DE LAS TARJETAS (IGUAL QUE HOME) ----

    private async void OnCreatorClicked(object sender, TappedEventArgs e)
    {
        if (e.Parameter is int creadorId)
            await Shell.Current.GoToAsync($"other?usuarioId={creadorId}");
    }

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
                await SafeRefreshLikes(api, usuarioId);
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
                await SafeRefreshLikes(api, usuarioId);
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

    private async Task SafeRefreshLikes(DatabaseService api, int usuarioId)
    {
        try
        {
            var likesFromServer = await api.GetLikesUsuarioAsync(usuarioId);
            _likesUsuario = new HashSet<int>(likesFromServer.Select(l => l.RecetaId));
        }
        catch { }
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
