namespace MiniTFG;

public partial class UpdatePreferencesPage : ContentPage
{
    public List<AlergenosPreferencias> Preferencias { get; set; }

    public UpdatePreferencesPage()
    {
        InitializeComponent();

        var u = App.UsuarioActual ?? new Usuario();

        Preferencias = new List<AlergenosPreferencias>
        {
            new AlergenosPreferencias { Nombre = "Gluten", Seleccion = u.Gluten },
            new AlergenosPreferencias { Nombre = "Leche", Seleccion = u.Lactosa },
            new AlergenosPreferencias { Nombre = "Frutos secos", Seleccion = u.FrutosSecos },
            new AlergenosPreferencias { Nombre = "Marisco", Seleccion = u.Marisco },
            new AlergenosPreferencias { Nombre = "Huevos", Seleccion = u.Huevo },
            new AlergenosPreferencias { Nombre = "Soja", Seleccion = u.Soja },
            new AlergenosPreferencias { Nombre = "Pescado", Seleccion = u.Pescado },
            new AlergenosPreferencias { Nombre = "Cacahuetes", Seleccion = u.Cacahuetes },
            new AlergenosPreferencias { Nombre = "Sésamo", Seleccion = u.Sesamo },
            new AlergenosPreferencias { Nombre = "Sulfitos", Seleccion = u.Sulfitos },
            new AlergenosPreferencias { Nombre = "Mostaza", Seleccion = u.Mostaza },
            new AlergenosPreferencias { Nombre = "Altramuces", Seleccion = u.Altramuces },
            new AlergenosPreferencias { Nombre = "Moluscos", Seleccion = u.Moluscos },
            new AlergenosPreferencias { Nombre = "Apio", Seleccion = u.Apio },
            new AlergenosPreferencias { Nombre = "Vegetariano", Seleccion = u.Vegetariano },
            new AlergenosPreferencias { Nombre = "Vegano", Seleccion = u.Vegano }
        };

        BindingContext = this;
    }

    private async void UpdateClicked(object sender, EventArgs e)
    {
        if (App.UsuarioActual == null)
        {
            await Shell.Current.GoToAsync("//login");
            return;
        }

        var usuario = App.UsuarioActual;

        foreach (var item in Preferencias)
        {
            switch (item.Nombre)
            {
                case "Gluten": usuario.Gluten = item.Seleccion; break;
                case "Leche": usuario.Lactosa = item.Seleccion; break;
                case "Frutos secos": usuario.FrutosSecos = item.Seleccion; break;
                case "Marisco": usuario.Marisco = item.Seleccion; break;
                case "Huevos": usuario.Huevo = item.Seleccion; break;
                case "Soja": usuario.Soja = item.Seleccion; break;
                case "Pescado": usuario.Pescado = item.Seleccion; break;
                case "Cacahuetes": usuario.Cacahuetes = item.Seleccion; break;
                case "Sésamo": usuario.Sesamo = item.Seleccion; break;
                case "Sulfitos": usuario.Sulfitos = item.Seleccion; break;
                case "Mostaza": usuario.Mostaza = item.Seleccion; break;
                case "Altramuces": usuario.Altramuces = item.Seleccion; break;
                case "Moluscos": usuario.Moluscos = item.Seleccion; break;
                case "Apio": usuario.Apio = item.Seleccion; break;
                case "Vegetariano": usuario.Vegetariano = item.Seleccion; break;
                case "Vegano": usuario.Vegano = item.Seleccion; break;
            }
        }

        try
        {
            var api = new DatabaseService();
            if (await api.UpdatePreferenciasUsuarioAsync(usuario))
            {
                App.UsuarioActual = usuario;
                await DisplayAlertAsync("Preferencias actualizadas", "Tus preferencias han sido actualizadas correctamente.", "OK");
                await Shell.Current.GoToAsync("//profile");
            }
            else
            {
                await DisplayAlertAsync("Error", "No se pudieron actualizar las preferencias.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudieron actualizar las preferencias: {ex.Message}", "OK");
        }
    }
}
