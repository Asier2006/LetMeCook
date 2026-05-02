namespace MiniTFG;

/// <summary>
/// Actualiza el nombre de usuario en MySQL y en la sesión local.
/// </summary>
public partial class UpdateUsernamePage : ContentPage
{
    public UpdateUsernamePage()
    {
        InitializeComponent();
        UsernameEntry.Text = App.UsuarioActual?.Nombre;
    }

    private async void GuardarUsernameClicked(object sender, EventArgs e)
    {
        if (App.UsuarioActual == null)
        {
            await Shell.Current.GoToAsync("//login");
            return;
        }

        string newUsername = UsernameEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(newUsername))
        {
            await DisplayAlertAsync("Error", "El nombre no puede estar vacío.", "OK");
            return;
        }

        try
        {
            var api = new DatabaseService();
            if (await api.UpdateNombreUsuarioAsync(App.UsuarioActual.Id, newUsername))
            {
                App.UsuarioActual.Nombre = newUsername;
                await DisplayAlertAsync("Actualizado", "Nombre de usuario actualizado.", "OK");
                await Shell.Current.GoToAsync("//profile");
            }
            else
            {
                await DisplayAlertAsync("Error", "No se pudo actualizar el nombre.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudo actualizar el nombre: {ex.Message}", "OK");
        }
    }
}
