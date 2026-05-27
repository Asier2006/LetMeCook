using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MiniTFG;

public partial class ShopPage : ContentPage, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private int _puntosTotales;
    public int PuntosTotales
    {
        get => _puntosTotales;
        set { _puntosTotales = value; OnPropertyChanged(); }
    }

    public ShopPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadStoreAsync();

        // Bio solo en memoria
        BioEditor.Text = App.UsuarioActual.Descripcion ?? "";
    }

    private async Task LoadStoreAsync()
    {
        if (App.UsuarioActual == null) return;

        var api = new DatabaseService();
        await api.EnsureDefaultSkinsAsync();
        var skins = await api.GetSkinsAsync();

        var banners = skins.Where(s => s.Nombre.Contains("banner", StringComparison.OrdinalIgnoreCase)).ToList();
        var fotos = skins.Where(s => s.Nombre.Contains("foto", StringComparison.OrdinalIgnoreCase)).ToList();

        var owned = await api.GetPurchasedUserSkinsAsync(App.UsuarioActual.Id);
        var ownedSet = new HashSet<int>(owned);

        foreach (var s in skins)
            s.Comprado = ownedSet.Contains(s.Id);

        PuntosTotales = await api.GetPuntosUsuarioAsync(App.UsuarioActual.Id);
        LikesLabel.Text = $"Puntos: {PuntosTotales}";

        BannersContainer.Children.Clear();
        foreach (var b in banners)
            BannersContainer.Children.Add(CrearItemSkin(b, true));

        FotosContainer.Children.Clear();
        foreach (var f in fotos)
            FotosContainer.Children.Add(CrearItemSkin(f, false));
    }

    private View CrearItemSkin(Skin skin, bool esBanner)
    {
        var imagen = new Image
        {
            Source = skin.Imagen,
            HeightRequest = esBanner ? 120 : 80,
            WidthRequest = esBanner ? 220 : 80,
            Aspect = Aspect.AspectFill
        };

        var precio = new Label
        {
            Text = $"{skin.Precio} puntos",
            FontSize = 14,
            HorizontalOptions = LayoutOptions.Center
        };

        var boton = new Button
        {
            CornerRadius = 10,
            HeightRequest = 40,
            WidthRequest = 120,
            TextColor = Colors.White
        };

        ActualizarBotonSkin(boton, skin, esBanner);

        boton.Clicked += async (s, e) =>
        {
            if (!skin.Comprado)
                await ComprarSkinAsync(skin);
            else
                await UsarSkinAsync(skin, esBanner);

            // Actualizar todos los botones
            await LoadStoreAsync();
        };

        return new VerticalStackLayout
        {
            Spacing = 5,
            Children = { imagen, precio, boton }
        };
    }

    private void ActualizarBotonSkin(Button boton, Skin skin, bool esBanner)
    {
        bool enUso = (esBanner && App.UsuarioActual.Banner == skin.Imagen) ||
                     (!esBanner && App.UsuarioActual.Foto == skin.Imagen);

        if (enUso)
        {
            boton.Text = "En uso";
            boton.BackgroundColor = Color.FromArgb("#4CAF50");
        }
        else if (skin.Comprado)
        {
            boton.Text = "Usar";
            boton.BackgroundColor = Color.FromArgb("#C87F2F");
        }
        else
        {
            boton.Text = "Comprar";
            boton.BackgroundColor = Color.FromArgb("#E09A3F");
        }
    }

    private async Task ComprarSkinAsync(Skin skin)
    {
        if (PuntosTotales < skin.Precio)
        {
            await DisplayAlert("Puntos insuficientes",
                $"Necesitas {skin.Precio} puntos, tienes {PuntosTotales}", "OK");
            return;
        }

        var api = new DatabaseService();
        bool ok = await api.PurchaseSkinAsync(App.UsuarioActual.Id, skin.Id);

        if (ok)
        {
            skin.Comprado = true;
            await api.SumarPuntosUsuarioAsync(App.UsuarioActual.Id, -skin.Precio);
            PuntosTotales = await api.GetPuntosUsuarioAsync(App.UsuarioActual.Id);
            LikesLabel.Text = $"Puntos: {PuntosTotales}";
        }
    }

    private async Task UsarSkinAsync(Skin skin, bool esBanner)
    {
        var api = new DatabaseService();
        bool ok = await api.ActivateSkinAsync(App.UsuarioActual.Id, skin.Id, esBanner ? "banner" : "foto");

        if (!ok)
        {
            await DisplayAlert("Error", "No se pudo activar la skin.", "OK");
            return;
        }

        if (esBanner)
            App.UsuarioActual.Banner = skin.Imagen;
        else
            App.UsuarioActual.Foto = skin.Imagen;
    }

    private async void GuardarBioClicked(object sender, EventArgs e)
    {
        string bio = BioEditor.Text?.Trim() ?? "";

        if (bio.Length > 160)
        {
            await DisplayAlert("Error", "La biografía no puede superar los 160 caracteres.", "OK");
            return;
        }

        // 🔥 SQL desactivado temporalmente
        App.UsuarioActual.Descripcion = bio;

        await DisplayAlert("Guardado", "Tu biografía ha sido actualizada (solo local).", "OK");
    }
}
