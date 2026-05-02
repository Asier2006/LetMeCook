// Punto de entrada de la app: crea la ventana principal y restaura la sesión guardada si existe.


namespace MiniTFG
{
    public partial class App : Application
    {
        public const string RememberUserIdKey = "remember_user_id";

        // UsuarioTemporal se usa durante el registro multipantalla.
        public static Usuario UsuarioTemporal { get; set; }

        // UsuarioActual representa la sesión activa. Si es null, el usuario navega como invitado.
        public static Usuario UsuarioActual { get; set; }

        public App()
        {
            InitializeComponent();
        }

        // Crea AppShell y lanza la restauración de sesión después de inicializar la ventana.
        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = new AppShell();
            var window = new Window(shell);

            shell.Dispatcher.Dispatch(async () =>
            {
                await RestaurarSesionRecordadaAsync(shell);
            });

            return window;
        }

        // Si existe un id guardado en Preferences, recupera el usuario completo desde MySQL.
        private static async Task RestaurarSesionRecordadaAsync(Shell shell)
        {
            int savedUserId = Preferences.Get(RememberUserIdKey, 0);

            if (savedUserId <= 0)
            {
                await shell.GoToAsync("//login");
                return;
            }

            try
            {
                var api = new DatabaseService();
                var usuario = await api.GetUsuarioByIdAsync(savedUserId);

                if (usuario == null)
                {
                    LimpiarSesionRecordada();
                    await shell.GoToAsync("//login");
                    return;
                }

                UsuarioActual = usuario;
                await shell.GoToAsync("//home");
            }
            catch
            {
                LimpiarSesionRecordada();
                await shell.GoToAsync("//login");
            }
        }

        // Solo se guarda el Id: el resto de datos se vuelven a consultar para evitar sesiones obsoletas.
        public static void GuardarSesionRecordada(Usuario usuario)
        {
            Preferences.Set(RememberUserIdKey, usuario.Id);
        }

        // Limpia tanto la clave nueva como claves antiguas que podían quedar de versiones previas.
        public static void LimpiarSesionRecordada()
        {
            Preferences.Remove(RememberUserIdKey);
            Preferences.Remove("userId");
            Preferences.Remove("userName");
            Preferences.Remove("userCorreo");
        }
    }
}
