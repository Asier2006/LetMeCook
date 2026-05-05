using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace MiniTFG;

[QueryProperty(nameof(RecetaId), "recetaId")]
[QueryProperty(nameof(UsuarioId), "usuarioId")]
public partial class LetMeCookPage : ContentPage
{
    private const long MAX_VIDEO_SIZE_BYTES = 30L * 1024L * 1024L;

    private readonly List<PasoReceta> _pasos = new();
    private int _recetaId;
    private int _usuarioId;
    private int _contadorPasos;
    private bool _cargado;
    private bool _guardando;

    public int RecetaId
    {
        get => _recetaId;
        set
        {
            _recetaId = value;
            _ = CargarRecetaAsync();
        }
    }

    public int UsuarioId
    {
        get => _usuarioId;
        set => _usuarioId = value;
    }

    public LetMeCookPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_pasos.Count == 0)
            AgregarPasoVisual();
    }

    private async Task CargarRecetaAsync()
    {
        if (_recetaId <= 0 || _cargado)
            return;

        _cargado = true;

        try
        {
            var api = new DatabaseService();
            var receta = await api.GetRecetaByIdAsync(_recetaId);

            if (receta != null)
                RecetaTituloLabel.Text = receta.Titulo;
        }
        catch
        {
            RecetaTituloLabel.Text = "Completa tu receta";
        }
    }

    private void AgregarPasoClicked(object sender, EventArgs e)
    {
        AgregarPasoVisual();
    }

    private void AgregarPasoVisual()
    {
        _contadorPasos++;
        int numeroPaso = _contadorPasos;

        var paso = new PasoReceta { NumeroPaso = numeroPaso };
        _pasos.Add(paso);

        var card = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
            BackgroundColor = Color.FromArgb("#2D2D2D"),
            Padding = new Thickness(14)
        };

        var layout = new VerticalStackLayout { Spacing = 10 };

        layout.Children.Add(new Label
        {
            Text = $"Paso {numeroPaso}",
            TextColor = Color.FromArgb("#9C40F7"),
            FontAttributes = FontAttributes.Bold,
            FontSize = 20
        });

        var tituloEntry = new Entry
        {
            Placeholder = $"Título o comentario corto del paso {numeroPaso}",
            TextColor = Colors.White,
            PlaceholderColor = Colors.Gray,
            BackgroundColor = Color.FromArgb("#3A3A3A")
        };

        var descripcionEditor = new Editor
        {
            Placeholder = "Descripción/comentario opcional (+2 puntos)",
            TextColor = Colors.White,
            PlaceholderColor = Colors.Gray,
            BackgroundColor = Color.FromArgb("#3A3A3A"),
            AutoSize = EditorAutoSizeOption.TextChanges,
            HeightRequest = 100
        };

        void ActualizarDescripcion()
        {
            var partes = new[] { tituloEntry.Text, descripcionEditor.Text }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim());

            paso.Descripcion = string.Join(Environment.NewLine, partes);
            ActualizarPuntosPreview();
        }

        tituloEntry.TextChanged += (_, _) => ActualizarDescripcion();
        descripcionEditor.TextChanged += (_, _) => ActualizarDescripcion();

        var videoLabel = new Label
        {
            Text = "Sin vídeo seleccionado",
            TextColor = Colors.LightGray,
            FontSize = 13
        };

        var videoButton = new Button
        {
            Text = "Seleccionar vídeo (+3 puntos)",
            BackgroundColor = Color.FromArgb("#9C40F7"),
            TextColor = Colors.White,
            CornerRadius = 8
        };

        videoButton.Clicked += async (_, _) =>
        {
            await SeleccionarVideoAsync(paso, videoLabel, numeroPaso);
            ActualizarPuntosPreview();
        };

        layout.Children.Add(tituloEntry);
        layout.Children.Add(descripcionEditor);
        layout.Children.Add(videoButton);
        layout.Children.Add(videoLabel);

        card.Content = layout;
        PasosContainer.Children.Add(card);
        ActualizarPuntosPreview();
    }

    private async Task SeleccionarVideoAsync(PasoReceta paso, Label videoLabel, int numeroPaso)
    {
        try
        {
            var result = await FilePicker.PickAsync(GetVideoPickOptions($"Selecciona el vídeo del paso {numeroPaso}"));

            if (result == null)
                return;

            using var stream = await result.OpenReadAsync();

            if (stream.CanSeek && stream.Length > MAX_VIDEO_SIZE_BYTES)
            {
                await DisplayAlertAsync("Vídeo demasiado grande", "El vídeo no puede superar los 30 MB.", "OK");
                return;
            }

            using var ms = new MemoryStream();
            byte[] buffer = new byte[81920];
            long totalBytes = 0;
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                totalBytes += bytesRead;

                if (totalBytes > MAX_VIDEO_SIZE_BYTES)
                {
                    await DisplayAlertAsync("Vídeo demasiado grande", "El vídeo no puede superar los 30 MB.", "OK");
                    return;
                }

                await ms.WriteAsync(buffer, 0, bytesRead);
            }

            paso.Video = Convert.ToBase64String(ms.ToArray());
            paso.VideoNombreArchivo = result.FileName;
            paso.VideoContentType = string.IsNullOrWhiteSpace(result.ContentType) ? "video/mp4" : result.ContentType;
            videoLabel.Text = result.FileName;
        }
        catch
        {
            await DisplayAlertAsync("Error", "No se pudo seleccionar el vídeo", "OK");
        }
    }

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

    private int CalcularPuntosAvanzados()
    {
        int puntos = 0;

        foreach (var paso in _pasos)
        {
            if (!string.IsNullOrWhiteSpace(paso.Descripcion))
                puntos += 2;

            if (!string.IsNullOrWhiteSpace(paso.Video))
                puntos += 3;
        }

        return puntos;
    }

    private void ActualizarPuntosPreview()
    {
        PuntosLabel.Text = $"Puntos avanzados: {CalcularPuntosAvanzados()}";
    }

    private async void GuardarPasosClicked(object sender, EventArgs e)
    {
        if (_guardando)
            return;

        var pasosValidos = _pasos
            .Where(p => !string.IsNullOrWhiteSpace(p.Descripcion) || !string.IsNullOrWhiteSpace(p.Video))
            .ToList();

        if (pasosValidos.Count == 0)
        {
            await DisplayAlertAsync("Sin pasos", "Añade al menos una descripción o un vídeo.", "OK");
            return;
        }

        _guardando = true;

        try
        {
            var api = new DatabaseService();
            int numero = 1;

            foreach (var paso in pasosValidos)
            {
                paso.RecetaId = _recetaId;
                paso.NumeroPaso = numero++;
                await api.PostPasoRecetaAsync(paso);
            }

            int puntos = CalcularPuntosAvanzados();
            if (puntos > 0 && App.UsuarioActual != null)
                await api.SumarPuntosUsuarioAsync(App.UsuarioActual.Id, puntos);

            await DisplayAlertAsync("Receta completada", $"Pasos guardados. Has obtenido {puntos} puntos.", "OK");
            await Shell.Current.GoToAsync("//home");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudieron guardar los pasos: {ex.Message}", "OK");
        }
        finally
        {
            _guardando = false;
        }
    }

    private async void CancelarClicked(object sender, EventArgs e)
    {
        bool salir = await DisplayAlertAsync(
            "Salir",
            "La receta básica ya está creada. ¿Quieres salir sin añadir pasos?",
            "Sí",
            "No");

        if (salir)
            await Shell.Current.GoToAsync("//home");
    }
}
