using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MiniTFG;

/// <summary>
/// Gestiona la tienda de fotos y banners canjeables por puntos.
/// </summary>
public partial class ShopPage : ContentPage, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private int _puntosTotales;

    public int PuntosTotales
    {
        get => _puntosTotales;
        set
        {
            if (_puntosTotales != value)
            {
                _puntosTotales = value;
                OnPropertyChanged();
            }
        }
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
    }


    // Carga skins, separa banners/fotos, marca compras y calcula puntos gastables.
    private async Task LoadStoreAsync()
    {
        if (App.UsuarioActual == null) return;

        var api = new DatabaseService();
        await api.EnsureDefaultSkinsAsync();
        var skins = await api.GetSkinsAsync(); // List<Skin>

        // 2. Separar banners y fotos (según tu criterio)
        var banners = skins.Where(s => s.Nombre.Contains("banner", StringComparison.OrdinalIgnoreCase)).ToList();
        var fotos = skins.Where(s => s.Nombre.Contains("foto", StringComparison.OrdinalIgnoreCase)).ToList();

        // 3. Obtener skins compradas
        var owned = await api.GetPurchasedUserSkinsAsync(App.UsuarioActual.Id); // List<int>
        var ownedSet = new HashSet<int>(owned);

        // 4. Marcar skins compradas
        foreach (var s in skins)
            s.Comprado = ownedSet.Contains(s.Id);

        // 5. Calcular puntos disponibles. Los puntos se guardan en Usuarios.Puntos.
        PuntosTotales = await api.GetPuntosUsuarioAsync(App.UsuarioActual.Id);
        LikesLabel.Text = $"Puntos: {PuntosTotales}";

        // 6. Pintar en pantalla
        BannersContainer.Children.Clear();
        foreach (var b in banners)
            BannersContainer.Children.Add(CrearItemSkin(b, true));

        FotosContainer.Children.Clear();
        foreach (var f in fotos)
            FotosContainer.Children.Add(CrearItemSkin(f, false));
    }

    // Crea la tarjeta visual de una skin y conecta el botón Comprar/Usar.
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
            Text = skin.Comprado ? "Usar" : "Comprar",
            BackgroundColor = skin.Comprado ? Color.FromArgb("#4CAF50") : Color.FromArgb("#9C40F7"),
            TextColor = Colors.White,
            CornerRadius = 10,
            HeightRequest = 40,
            WidthRequest = 120
        };

        // Si Skin implementa INotifyPropertyChanged, actualizamos el botón cuando cambie Comprado
        if (skin is INotifyPropertyChanged npc)
        {
            npc.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Skin.Comprado))
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        boton.Text = skin.Comprado ? "Usar" : "Comprar";
                        boton.BackgroundColor = skin.Comprado ? Color.FromArgb("#4CAF50") : Color.FromArgb("#9C40F7");
                    });
                }
            };
        }

        boton.Clicked += async (s, e) =>
        {
            if (App.UsuarioActual == null)
            {
                await DisplayAlertAsync("Inicia sesión", "Debes iniciar sesión para comprar o usar skins.", "OK");
                return;
            }

            if (!skin.Comprado)
                await ComprarSkinAsync(skin);
            else
                await UsarSkinAsync(skin, esBanner);
        };

        return new VerticalStackLayout
        {
            Spacing = 5,
            Children = { imagen, precio, boton }
        };
    }

    // Comprueba saldo de puntos e inserta la compra en UserSkins.
    private async Task ComprarSkinAsync(Skin skin)
    {
        // Antes de comprar se valida que los puntos disponibles cubren el precio.
        if (PuntosTotales < skin.Precio)
        {
            await DisplayAlertAsync("Puntos insuficientes", 
                $"Necesitas {skin.Precio} puntos, tienes {PuntosTotales}", "OK");
            return;
        }

        var api = new DatabaseService();
        bool ok = await api.PurchaseSkinAsync(App.UsuarioActual.Id, skin.Id);

        if (ok)
        {
            skin.Comprado = true;
            
            // Se descuenta localmente para que la UI responda inmediatamente.
            await api.SumarPuntosUsuarioAsync(App.UsuarioActual.Id, -skin.Precio);
            PuntosTotales = await api.GetPuntosUsuarioAsync(App.UsuarioActual.Id);
            LikesLabel.Text = $"Puntos: {PuntosTotales}";
            
            await DisplayAlertAsync("Comprado", $"Has comprado {skin.Nombre}. Te quedan {PuntosTotales} puntos.", "OK");
            
            // La próxima entrada en tienda volverá a recalcular el saldo desde MySQL.
        }
        else
        {
            await DisplayAlertAsync("Error", "No se pudo completar la compra. Intenta de nuevo.", "OK");
            
            // Si falla, no se cambia el estado local de la skin.
        }
    }

    // Activa la skin elegida como foto o banner del usuario actual.
    private async Task UsarSkinAsync(Skin skin, bool esBanner)
    {
        var api = new DatabaseService();
        bool ok = await api.ActivateSkinAsync(App.UsuarioActual.Id, skin.Id, esBanner ? "banner" : "foto");

        if (!ok)
        {
            await DisplayAlertAsync("Error", "No se pudo activar la skin.", "OK");
            return;
        }

        // Actualiza App.UsuarioActual para que la UI del perfil muestre la skin
        // Decide si guardas SkinId o Image en Usuario.Foto/Banner. Aquí usamos Image (nombre de archivo).
        if (esBanner)
            App.UsuarioActual.Banner = skin.Imagen;
        else
            App.UsuarioActual.Foto = skin.Imagen;

        // Si Usuario implementa INotifyPropertyChanged, la UI se actualizará.
        await DisplayAlertAsync("Activada", "Skin activada correctamente.", "OK");
    }

}