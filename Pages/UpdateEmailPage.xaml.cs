namespace MiniTFG;

/// <summary>
/// Actualiza el correo tras validar formato y que no exista en otra cuenta.
/// </summary>
public partial class UpdateEmailPage : ContentPage
{
    public UpdateEmailPage()
    {
        InitializeComponent();
        EmailEntry.Text = App.UsuarioActual?.Correo;
    }

    private async void GuardarEmailClicked(object sender, EventArgs e)
    {
        if (App.UsuarioActual == null)
        {
            await Shell.Current.GoToAsync("//login");
            return;
        }

        string newEmail = EmailEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(newEmail) || !newEmail.Contains("@") || !newEmail.Contains("."))
        {
            await DisplayAlertAsync("Error", "Introduce un email válido.", "OK");
            return;
        }

        try
        {
            var api = new DatabaseService();

            if (await api.UsuarioExistePorCorreoAsync(newEmail, App.UsuarioActual.Id))
            {
                await DisplayAlertAsync("Error", "Ese email ya está en uso.", "OK");
                return;
            }

            if (await api.UpdateCorreoUsuarioAsync(App.UsuarioActual.Id, newEmail))
            {
                App.UsuarioActual.Correo = newEmail;
                Preferences.Set("last_login_email", newEmail);
                await DisplayAlertAsync("Actualizado", "Email actualizado.", "OK");
                await Shell.Current.GoToAsync("//profile");
            }
            else
            {
                await DisplayAlertAsync("Error", "No se pudo actualizar el email.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudo actualizar el email: {ex.Message}", "OK");
        }
    }
}
