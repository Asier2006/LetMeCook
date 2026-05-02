namespace MiniTFG;

/// <summary>
/// Controla login normal, login como invitado y creación de cuenta.
/// </summary>
public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
        emailEntry.Text = Preferences.Get("last_login_email", string.Empty);
        chkRemember.IsChecked = Preferences.Get(App.RememberUserIdKey, 0) > 0;
    }

    // Valida credenciales contra MySQL y actualiza la sesión global.
    private async void SessionClicked(object sender, EventArgs e)
    {
        var api = new DatabaseService();

        var email = emailEntry.Text?.Trim();
        var password = passwordEntry.Text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlertAsync("Error", "Rellena todos los campos.", "OK");
            return;
        }

        try
        {
            var usuario = await api.LoginAsync(email, password);

            if (usuario == null)
            {
                await DisplayAlertAsync("Error", "Correo o contraseña incorrectos.", "OK");
                return;
            }

            App.UsuarioActual = usuario;
            Preferences.Set("last_login_email", usuario.Correo);

            if (chkRemember.IsChecked)
                App.GuardarSesionRecordada(usuario);
            else
                App.LimpiarSesionRecordada();

            await Shell.Current.GoToAsync("//home");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudo conectar con la base de datos: {ex.Message}", "OK");
        }
    }

    // Entra sin usuario; las páginas protegidas volverán a pedir login cuando sea necesario.
    private async void GuestClicked(object sender, EventArgs e)
    {
        App.UsuarioActual = null;
        App.UsuarioTemporal = null;
        App.LimpiarSesionRecordada();
        await Shell.Current.GoToAsync("//home");
    }

    private async void CreateClicked(object sender, EventArgs e)
    {
        App.UsuarioTemporal = null;
        await Shell.Current.GoToAsync("name");
    }

    private void ShowCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        passwordEntry.IsPassword = !e.Value;
    }

    private void RememberCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (!e.Value)
            App.LimpiarSesionRecordada();
    }
}
