namespace MiniTFG;

/// <summary>
/// Permite marcar preferencias alimentarias y termina el alta en la base de datos.
/// </summary>
public partial class PreferencesPage : ContentPage
{
    public List<AlergenosPreferencias> Preferencias { get; set; }

    public PreferencesPage()
    {
        InitializeComponent();

        Preferencias = new List<AlergenosPreferencias>
        {
            new AlergenosPreferencias { Nombre = "Vegetariano", Seleccion = false },
            new AlergenosPreferencias { Nombre = "Vegano", Seleccion = false }
        };

        BindingContext = this;
    }

    // Completa el UsuarioTemporal y lo inserta en la tabla Usuarios.
    private async void FinishClicked(object sender, EventArgs e)
    {
        if (App.UsuarioTemporal == null)
        {
            await DisplayAlertAsync("Error", "No hay datos de registro. Vuelve a empezar.", "OK");
            await Shell.Current.GoToAsync("//login");
            return;
        }

        foreach (var item in Preferencias)
        {
            switch (item.Nombre)
            {
                case "Vegetariano":
                    App.UsuarioTemporal.Vegetariano = item.Seleccion;
                    break;
                case "Vegano":
                    App.UsuarioTemporal.Vegano = item.Seleccion;
                    break;
            }
        }

        try
        {
            var api = new DatabaseService();
            var creado = await api.PostUsuarioAsync(App.UsuarioTemporal);

            if (creado == null)
            {
                await DisplayAlertAsync("Error", "No se pudo crear la cuenta.", "OK");
                return;
            }

            App.UsuarioActual = creado;
            App.UsuarioTemporal = null;
            Preferences.Set("last_login_email", creado.Correo);

            await Shell.Current.GoToAsync("//home");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudo crear la cuenta: {ex.Message}", "OK");
        }
    }
}
