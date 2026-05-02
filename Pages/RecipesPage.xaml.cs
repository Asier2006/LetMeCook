using System.Collections.ObjectModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace MiniTFG;

/// <summary>
/// Formulario de creación de recetas con imagen, metadatos, alérgenos, preferencias y pasos.
/// </summary>
public partial class RecipesPage : ContentPage
{
    public ObservableCollection<AlergenosPreferencias> Alergenos { get; set; }
    public ObservableCollection<AlergenosPreferencias> Preferencias { get; set; }
    double lastScrollY = 0;
    bool isBarHidden = false;
    private string imagenBase64 = null;
    private List<PasoReceta> _pasos = new();
    private int _contadorPasos = 0;
    public RecipesPage()
	{
		InitializeComponent();

        Alergenos = new ObservableCollection<AlergenosPreferencias>
        {
            new AlergenosPreferencias { Nombre = "Gluten", Seleccion = false },
            new AlergenosPreferencias { Nombre = "Leche", Seleccion = false },
            new AlergenosPreferencias { Nombre = "Frutos secos", Seleccion = false },
            new AlergenosPreferencias { Nombre = "Marisco", Seleccion = false },
            new AlergenosPreferencias { Nombre = "Huevos", Seleccion = false },
            new AlergenosPreferencias { Nombre = "Soja", Seleccion = false },
            new AlergenosPreferencias { Nombre = "Pescado", Seleccion = false },
            new AlergenosPreferencias { Nombre = "Cacahuetes", Seleccion = false },
            new AlergenosPreferencias { Nombre = "Sésamo", Seleccion = false },
            new AlergenosPreferencias { Nombre = "Sulfitos", Seleccion = false },
            new AlergenosPreferencias { Nombre = "Mostaza", Seleccion = false },
            new AlergenosPreferencias { Nombre = "Altramuces", Seleccion = false },
            new AlergenosPreferencias { Nombre = "Moluscos", Seleccion = false },
            new AlergenosPreferencias { Nombre = "Apio", Seleccion = false }
        };

        Preferencias = new ObservableCollection<AlergenosPreferencias>
        {
            new AlergenosPreferencias { Nombre = "Vegano", Seleccion = false },
            new AlergenosPreferencias { Nombre = "Vegetariano", Seleccion = false },
        };
        BindingContext = this;
    }

	private async void InicioClicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//home");
    }

    private async void RecetasClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//recipes");
    }

    private async void PerfilClicked(object sender, EventArgs e)
    {
        if (App.UsuarioActual == null)
        {
            await DisplayAlertAsync("Inicia sesión", "Para acceder a tu perfil necesitas iniciar sesión.", "OK");
            await Shell.Current.GoToAsync("//login");
            return;
        }

        await Shell.Current.GoToAsync("//profile");
    }

    // Permite elegir si la imagen de la receta viene de cámara o galería.
    private async void SeleccionarImagenClicked(object sender, EventArgs e)
    {
        string opcion = await DisplayActionSheetAsync(
        "Seleccionar imagen",
        "Cancelar",
        null,
        "Hacer foto",
        "Elegir de la galería");

        if (opcion == "Hacer foto")
        {
            await TomarFoto();
        }
        else if (opcion == "Elegir de la galería")
        {
            await ElegirDeGaleria();
        }
    }

    private async Task TomarFoto()
    {
        try
        {
            var foto = await MediaPicker.CapturePhotoAsync();

            if (foto != null)
            {
                using var stream = await foto.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                byte[] imageBytes = memoryStream.ToArray();
                imagenBase64 = Convert.ToBase64String(imageBytes);
                
                ImagenPreview.Source = ImageSource.FromFile(foto.FullPath);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", "No se pudo abrir la cámara", "OK");
        }
    }

    private async Task ElegirDeGaleria()
    {
        try
        {
            var foto = await MediaPicker.PickPhotoAsync();

            if (foto != null)
            {
                using var stream = await foto.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                byte[] imageBytes = memoryStream.ToArray();
                imagenBase64 = Convert.ToBase64String(imageBytes);
                
                ImagenPreview.Source = ImageSource.FromFile(foto.FullPath);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", "No se pudo seleccionar la imagen", "OK");
        }
    }

    // Limita el selector de archivos a formatos de vídeo soportados por cada plataforma.
    private static PickOptions GetVideoPickOptions(string titulo)
    {
        return new PickOptions
        {
            PickerTitle = titulo,
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { "video/*" } },
                { DevicePlatform.iOS, new[] { "public.movie" } },
                { DevicePlatform.MacCatalyst, new[] { "public.movie" } },
                { DevicePlatform.WinUI, new[] { ".mp4", ".mov", ".avi", ".mkv", ".webm" } }
            })
        };
    }

    // Añade dinámicamente un bloque visual para un paso y guarda descripción/vídeo en memoria.
    private const long MAX_VIDEO_SIZE_BYTES = 30L * 1024L * 1024L; // 30 mnegabytes

    private async void AgregarPasoClicked(object sender, EventArgs e)
    {
        _contadorPasos++;
        int numeroPaso = _contadorPasos;

        var paso = new PasoReceta { NumeroPaso = numeroPaso };
        _pasos.Add(paso);

        var pasoLayout = new VerticalStackLayout { Spacing = 5, Padding = new Thickness(0, 5) };

        var label = new Label
        {
            Text = $"Paso {numeroPaso}",
            FontAttributes = FontAttributes.Bold,
            FontSize = 16
        };

        var descripcionEntry = new Entry
        {
            Placeholder = $"Descripción del paso {numeroPaso}",
            FontSize = 16
        };

        descripcionEntry.TextChanged += (s, args) =>
        {
            paso.Descripcion = args.NewTextValue;
        };

        var videoLabel = new Label
        {
            Text = "Sin vídeo seleccionado",
            FontSize = 14,
            TextColor = Colors.Gray
        };

        var videoButton = new Button
        {
            Text = "Seleccionar vídeo",
            BackgroundColor = Color.FromArgb("#9C40F7"),
            TextColor = Colors.White,
            CornerRadius = 8
        };

        videoButton.Clicked += async (s, args) =>
        {
            try
            {
                var result = await FilePicker.PickAsync(
                    GetVideoPickOptions($"Selecciona el vídeo del paso {numeroPaso}")
                );

                if (result == null)
                    return;

                using var stream = await result.OpenReadAsync();

                // Si el sistema permite saber el tamaño directamente, lo comprobamos antes.
                if (stream.CanSeek && stream.Length > MAX_VIDEO_SIZE_BYTES)
                {
                    await DisplayAlertAsync(
                        "Vídeo demasiado grande",
                        "El vídeo no puede superar los 30 MB.",
                        "OK"
                    );

                    return;
                }

                using var ms = new MemoryStream();
                byte[] buffer = new byte[81920];
                long totalBytes = 0;
                int bytesRead;

                // Leemos el archivo por partes para evitar cargar vídeos enormes en memoria.
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    totalBytes += bytesRead;

                    if (totalBytes > MAX_VIDEO_SIZE_BYTES)
                    {
                        await DisplayAlertAsync(
                            "Vídeo demasiado grande",
                            "El vídeo no puede superar los 30 MB.",
                            "OK"
                        );

                        return;
                    }

                    await ms.WriteAsync(buffer, 0, bytesRead);
                }

                paso.Video = Convert.ToBase64String(ms.ToArray());
                paso.VideoNombreArchivo = result.FileName;
                paso.VideoContentType = string.IsNullOrWhiteSpace(result.ContentType)
                    ? "video/mp4"
                    : result.ContentType;

                videoLabel.Text = result.FileName;
            }
            catch
            {
                await DisplayAlertAsync("Error", "No se pudo seleccionar el vídeo", "OK");
            }
        };

        pasoLayout.Children.Add(label);
        pasoLayout.Children.Add(descripcionEntry);
        pasoLayout.Children.Add(videoButton);
        pasoLayout.Children.Add(videoLabel);

        PasosContainer.Children.Add(pasoLayout);
    }

    // Valida el formulario, inserta la receta y después inserta sus pasos asociados.
    private async void GuardarRecetaClicked(object sender, EventArgs e)
    {
        if (App.UsuarioActual == null)
        {
            await DisplayAlertAsync("Error", "Debes iniciar sesión para crear una receta.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(NombreEntry.Text) ||
            string.IsNullOrWhiteSpace(DescripcionEntry.Text) ||
            string.IsNullOrWhiteSpace(ComensalesEntry.Text) ||
            string.IsNullOrWhiteSpace(TiempoEntry.Text) ||
            string.IsNullOrWhiteSpace(OrigenEntry.Text) ||
            string.IsNullOrWhiteSpace(MainIngredientEntry.Text) ||
            CocinaPicker.SelectedItem == null)
        {
            await DisplayAlertAsync("Error", "Rellena todos los datos principales de la receta.", "OK");
            return;
        }

        if (!int.TryParse(ComensalesEntry.Text, out int comensales) || comensales <= 0)
        {
            await DisplayAlertAsync("Error", "Los comensales deben ser un número mayor que 0.", "OK");
            return;
        }

        if (_pasos.Count == 0)
        {
            await DisplayAlertAsync("Error", "Añade al menos un paso a la receta.", "OK");
            return;
        }

        if (_pasos.Any(paso => string.IsNullOrWhiteSpace(paso.Descripcion)))
        {
            await DisplayAlertAsync("Error", "Todos los pasos deben tener descripción.", "OK");
            return;
        }

        var receta = new Receta
        {
            UsuarioId = App.UsuarioActual.Id,
            Titulo = NombreEntry.Text.Trim(),
            Imagen = imagenBase64,
            Descripcion = DescripcionEntry.Text.Trim(),
            Comensales = comensales,
            OrigenDelPlato = OrigenEntry.Text.Trim(),
            TiempoPreparacion = TiempoEntry.Text.Trim(),
            TipoCocina = CocinaPicker.SelectedItem?.ToString(),
            IngredientePrincipal = MainIngredientEntry.Text.Trim()
        };

        // Asignamos los alérgenos y preferencias seleccionados
        foreach (var alergeno in Alergenos)
        {
            switch (alergeno.Nombre)
            {
                case "Gluten":
                    receta.Gluten = alergeno.Seleccion;
                    break;
                case "Leche":
                    receta.Lactosa = alergeno.Seleccion;
                    break;
                case "Frutos secos":
                    receta.FrutosSecos = alergeno.Seleccion;
                    break;
                case "Marisco":
                    receta.Mariscos = alergeno.Seleccion;
                    break;
                case "Huevos":
                    receta.Huevo = alergeno.Seleccion;
                    break;
                case "Soja":
                    receta.Soja = alergeno.Seleccion;
                    break;
                case "Pescado":
                    receta.Pescado = alergeno.Seleccion;
                    break;
                case "Cacahuetes":
                    receta.Cacahuetes = alergeno.Seleccion;
                    break;
                case "Sésamo":
                    receta.Sesamo = alergeno.Seleccion;
                    break;
                case "Sulfitos":
                    receta.Sulfitos = alergeno.Seleccion;
                    break;
                case "Mostaza":
                    receta.Mostaza = alergeno.Seleccion;
                    break;
                case "Altramuces":
                    receta.Altramuces = alergeno.Seleccion;
                    break;
                case "Moluscos":
                    receta.Moluscos = alergeno.Seleccion;
                    break;
                case "Apio":
                    receta.Apio = alergeno.Seleccion;
                    break;
            }
        }

        foreach (var preferencia in Preferencias)
        {
            switch (preferencia.Nombre)
            {
                case "Vegano":
                    receta.Vegano = preferencia.Seleccion;
                    break;
                case "Vegetariano":
                    receta.Vegetariano = preferencia.Seleccion;
                    break;
            }
        }

        var api = new DatabaseService();
        var creada = await api.PostRecetaAsync(receta);

        if (creada == null)
        {
            await DisplayAlertAsync("Error", "No se pudo guardar la receta.", "OK");
            return;
        }

        foreach (var paso in _pasos)
        {

           
                paso.RecetaId = creada.Id;
                await api.PostPasoRecetaAsync(paso);



        }

        _pasos.Clear();
        _contadorPasos = 0;
        imagenBase64 = null;
        PasosContainer.Children.Clear();

        NombreEntry.Text = string.Empty;
        DescripcionEntry.Text = string.Empty;
        ComensalesEntry.Text = string.Empty;
        TiempoEntry.Text = string.Empty;
        OrigenEntry.Text = string.Empty;
        MainIngredientEntry.Text = string.Empty;
        CocinaPicker.SelectedItem = null;
        ImagenPreview.Source = null;

        foreach (var alergeno in Alergenos)
            alergeno.Seleccion = false;

        foreach (var preferencia in Preferencias)
            preferencia.Seleccion = false;

        await DisplayAlertAsync("Éxito", "Receta creada correctamente.", "OK");
        await Shell.Current.GoToAsync("//home");
    }
}