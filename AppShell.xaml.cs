// Registra las rutas de navegación usadas por Shell.


namespace MiniTFG
{

    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Rutas del flujo de registro.
            Routing.RegisterRoute("name", typeof(NamePage));
            Routing.RegisterRoute("allergies", typeof(AllergiesPage));
            Routing.RegisterRoute("preferences", typeof(PreferencesPage));

            // Rutas funcionales de la aplicación.
            Routing.RegisterRoute("shop", typeof(ShopPage));
            Routing.RegisterRoute("other", typeof(OtherProfilePage));
            Routing.RegisterRoute("settings", typeof(SettingsPage));
            Routing.RegisterRoute("recipesteps", typeof(RecipeStepsPage));

            // Rutas de ajustes de cuenta.
            Routing.RegisterRoute("updateusername", typeof(UpdateUsernamePage));
            Routing.RegisterRoute("updateemail", typeof(UpdateEmailPage));
            Routing.RegisterRoute("updatepassword", typeof(UpdatePasswordPage));
            Routing.RegisterRoute("updatepreferences", typeof(UpdatePreferencesPage));
        }
    }
}
