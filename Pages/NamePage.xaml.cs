namespace MiniTFG;

/// <summary>
/// Valida los datos básicos de registro antes de pasar a alérgenos.
/// </summary>
public partial class NamePage : ContentPage
{
    public NamePage()
    {
        InitializeComponent();
    }

    private void ShowCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        passwordEntry.IsPassword = !e.Value;
        passwordRepeatEntry.IsPassword = !e.Value;
    }

    // Valida datos básicos y crea un UsuarioTemporal que se completará en los siguientes pasos.
    public async void NextClicked(object sender, EventArgs e)
    {
        var nombre = nameEntry.Text?.Trim();
        var correo = emailEntry.Text?.Trim();
        var password = passwordEntry.Text;
        var passwordRepeat = passwordRepeatEntry.Text;

        if (string.IsNullOrWhiteSpace(nombre) ||
            string.IsNullOrWhiteSpace(correo) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(passwordRepeat))
        {
            await DisplayAlertAsync("Error", "Rellena todos los campos.", "OK");
            return;
        }

        if (!correo.Contains("@") || !correo.Contains("."))
        {
            await DisplayAlertAsync("Error", "Introduce un correo válido.", "OK");
            return;
        }

        if (password != passwordRepeat)
        {
            await DisplayAlertAsync("Error", "Las contraseñas no coinciden.", "OK");
            return;
        }

        try
        {
            var api = new DatabaseService();
            if (await api.UsuarioExistePorCorreoAsync(correo))
            {
                await DisplayAlertAsync("Error", "Ya existe una cuenta con ese correo.", "OK");
                return;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudo comprobar el correo: {ex.Message}", "OK");
            return;
        }

        App.UsuarioTemporal = new Usuarios
        {
            Nombre = nombre,
            Correo = correo,
            Contrasena = password,
            Foto = "user.png",
            Banner = "opbanner.jpg"
        };

        await Shell.Current.GoToAsync("allergies");
    }
}
