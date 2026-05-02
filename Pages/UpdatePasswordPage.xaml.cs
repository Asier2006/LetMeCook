namespace MiniTFG;

/// <summary>
/// Cambia la contraseña verificando la contraseña anterior.
/// </summary>
public partial class UpdatePasswordPage : ContentPage
{
    public UpdatePasswordPage()
    {
        InitializeComponent();
    }

    private async void GuardarPasswordClicked(object sender, EventArgs e)
    {
        if (App.UsuarioActual == null)
        {
            await Shell.Current.GoToAsync("//login");
            return;
        }

        var currPass = CurrentPasswordEntry.Text;
        var newPass = NewPasswordEntry.Text;
        var confirmPass = ConfirmPasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(currPass) ||
            string.IsNullOrWhiteSpace(newPass) ||
            string.IsNullOrWhiteSpace(confirmPass))
        {
            await DisplayAlertAsync("Error", "Rellena todos los campos.", "OK");
            return;
        }

        if (newPass != confirmPass)
        {
            await DisplayAlertAsync("Error", "Las contraseñas no coinciden.", "OK");
            return;
        }

        try
        {
            var api = new DatabaseService();
            if (await api.UpdatePasswordUsuarioAsync(App.UsuarioActual.Id, currPass, newPass))
            {
                App.UsuarioActual.Contrasena = newPass;
                await DisplayAlertAsync("Actualizada", "Contraseña actualizada.", "OK");
                await Shell.Current.GoToAsync("//profile");
            }
            else
            {
                await DisplayAlertAsync("Error", "La contraseña actual no es correcta.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudo actualizar la contraseña: {ex.Message}", "OK");
        }
    }
}
