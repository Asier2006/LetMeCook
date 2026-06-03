using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace MiniTFG;

[QueryProperty(nameof(RecetaId), "recetaId")]
[QueryProperty(nameof(UsuarioId), "usuarioId")]
public partial class LetMeCookPage : ContentPage
{
    private const long MAX_VIDEO_SIZE_BYTES = 10L * 1024L * 1024L;
    private const double MAX_VIDEO_DURATION_SECONDS = 5.0;

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

        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        header.Children.Add(new Label
        {
            Text = $"Paso {numeroPaso}",
            TextColor = Color.FromArgb("#FAC26C"),
            FontAttributes = FontAttributes.Bold,
            FontSize = 20
        });

        var eliminarBtn = new Button
        {
            Text = "Eliminar",
            BackgroundColor = Color.FromArgb("#FF5555"),
            TextColor = Colors.White,
            CornerRadius = 8,
            Padding = new Thickness(10, 4),
            FontSize = 12
        };

        eliminarBtn.Clicked += (_, _) =>
        {
            _pasos.Remove(paso);
            PasosContainer.Children.Remove(card);
            ActualizarPuntosPreview();
        };

        header.Children.Add(eliminarBtn);
        Grid.SetColumn(eliminarBtn, 1);

        layout.Children.Add(header);

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

        var videoButton = new Button
        {
            Text = "Añadir vídeo (+3 puntos)",
            BackgroundColor = Color.FromArgb("#E09A3F"),
            TextColor = Colors.White,
            CornerRadius = 8
        };

        var videosLayout = new HorizontalStackLayout
        {
            Spacing = 8
        };

        var videosScroll = new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            Content = videosLayout,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Never
        };

        RefrescarVideosSeleccionados(paso, videosLayout);

        videoButton.Clicked += async (_, _) =>
        {
            await SeleccionarVideoAsync(paso, videosLayout, numeroPaso);
            ActualizarPuntosPreview();
        };

        layout.Children.Add(tituloEntry);
        layout.Children.Add(descripcionEditor);
        layout.Children.Add(videoButton);
        layout.Children.Add(videosScroll);

        card.Content = layout;
        PasosContainer.Children.Add(card);
        ActualizarPuntosPreview();
    }

    private async Task SeleccionarVideoAsync(PasoReceta paso, HorizontalStackLayout videosLayout, int numeroPaso)
    {
        try
        {
            var result = await FilePicker.PickAsync(GetVideoPickOptions($"Selecciona un vídeo para el paso {numeroPaso}"));

            if (result == null)
                return;

            using var stream = await result.OpenReadAsync();

            if (stream.CanSeek && stream.Length > MAX_VIDEO_SIZE_BYTES)
            {
                await DisplayAlertAsync("Vídeo demasiado grande", "El vídeo no puede superar los 10 MB.", "OK");
                return;
            }

            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var bytes = ms.ToArray();

            if (bytes.Length > MAX_VIDEO_SIZE_BYTES)
            {
                await DisplayAlertAsync("Vídeo demasiado grande", "El vídeo no puede superar los 10 MB.", "OK");
                return;
            }

            if (!TryGetVideoDurationSeconds(bytes, out double duracionSegundos))
            {
                await DisplayAlertAsync(
                    "Duración no comprobable",
                    "No se pudo comprobar la duración. Usa un vídeo MP4, MOV, M4V o 3GP de 5 segundos como máximo.",
                    "OK");
                return;
            }

            if (duracionSegundos > MAX_VIDEO_DURATION_SECONDS + 0.05)
            {
                await DisplayAlertAsync(
                    "Vídeo demasiado largo",
                    $"El vídeo dura {duracionSegundos:0.##} segundos. La duración máxima permitida es de 5 segundos.",
                    "OK");
                return;
            }

            paso.Videos.Add(new PasoRecetaVideo
            {
                Orden = paso.Videos.Count + 1,
                Video = Convert.ToBase64String(bytes),
                VideoNombreArchivo = result.FileName,
                VideoContentType = result.ContentType ?? "video/mp4",
                DuracionSegundos = Convert.ToDecimal(Math.Round(duracionSegundos, 2))
            });

            RefrescarVideosSeleccionados(paso, videosLayout);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudo seleccionar el vídeo: {ex.Message}", "OK");
        }
    }

    private static void RefrescarVideosSeleccionados(PasoReceta paso, HorizontalStackLayout videosLayout)
    {
        videosLayout.Children.Clear();

        if (paso.Videos.Count == 0)
        {
            videosLayout.Children.Add(new Label
            {
                Text = "Sin vídeos",
                TextColor = Colors.LightGray,
                FontSize = 13,
                VerticalOptions = LayoutOptions.Center
            });
            return;
        }

        foreach (var video in paso.Videos.OrderBy(v => v.Orden))
        {
            videosLayout.Children.Add(new Border
            {
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                BackgroundColor = Color.FromArgb("#3A3A3A"),
                Padding = new Thickness(10, 6),
                Content = new Label
                {
                    Text = $"▶ {video.VideoNombreArchivo}",
                    TextColor = Colors.White,
                    FontSize = 12,
                    LineBreakMode = LineBreakMode.TailTruncation,
                    MaxLines = 1,
                    WidthRequest = 170
                }
            });
        }
    }

    private static PickOptions GetVideoPickOptions(string titulo)
    {
        return new PickOptions
        {
            PickerTitle = titulo,
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { "video/mp4", "video/quicktime", "video/3gpp" } },
                { DevicePlatform.iOS, new[] { "public.movie" } },
                { DevicePlatform.MacCatalyst, new[] { "public.movie" } },
                { DevicePlatform.WinUI, new[] { ".mp4", ".mov", ".m4v", ".3gp" } }
            })
        };
    }

    private int CalcularPuntosAvanzados()
    {
        int puntos = 0;

        foreach (var paso in _pasos)
        {
            bool tieneTexto = !string.IsNullOrWhiteSpace(paso.Descripcion);
            bool tieneVideo = paso.Videos.Count > 0;

            if (tieneTexto)
                puntos += 2;

            if (tieneVideo)
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
            .Where(p => !string.IsNullOrWhiteSpace(p.Descripcion) || p.Videos.Count > 0)
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

                var pasoGuardado = await api.PostPasoRecetaAsync(paso);
                if (pasoGuardado == null || pasoGuardado.Id <= 0)
                    throw new InvalidOperationException($"No se pudo guardar el paso {paso.NumeroPaso}.");
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

    private static bool TryGetVideoDurationSeconds(byte[] bytes, out double seconds)
    {
        seconds = 0;
        return TryFindMovieHeaderDuration(bytes, 0, bytes.Length, 0, out seconds);
    }

    private static bool TryFindMovieHeaderDuration(byte[] bytes, long start, long end, int depth, out double seconds)
    {
        seconds = 0;

        if (depth > 6)
            return false;

        long offset = start;

        while (offset + 8 <= end && offset + 8 <= bytes.Length)
        {
            ulong atomSize32 = ReadUInt32BigEndian(bytes, offset);
            string atomType = ReadAscii(bytes, offset + 4, 4);
            long headerSize = 8;
            long atomSize = (long)atomSize32;

            if (atomSize32 == 1)
            {
                if (offset + 16 > end || offset + 16 > bytes.Length)
                    return false;

                atomSize = (long)ReadUInt64BigEndian(bytes, offset + 8);
                headerSize = 16;
            }
            else if (atomSize32 == 0)
            {
                atomSize = end - offset;
            }

            if (atomSize < headerSize || offset + atomSize > end || offset + atomSize > bytes.Length)
                break;

            if (atomType == "mvhd")
                return TryReadMvhdDuration(bytes, offset + headerSize, atomSize - headerSize, out seconds);

            if (IsContainerAtom(atomType) && TryFindMovieHeaderDuration(bytes, offset + headerSize, offset + atomSize, depth + 1, out seconds))
                return true;

            offset += atomSize;
        }

        return false;
    }

    private static bool TryReadMvhdDuration(byte[] bytes, long payloadOffset, long payloadSize, out double seconds)
    {
        seconds = 0;

        if (payloadSize < 20 || payloadOffset + payloadSize > bytes.Length)
            return false;

        byte version = bytes[(int)payloadOffset];

        if (version == 0)
        {
            if (payloadSize < 20)
                return false;

            uint timescale = ReadUInt32BigEndian(bytes, payloadOffset + 12);
            uint duration = ReadUInt32BigEndian(bytes, payloadOffset + 16);

            if (timescale == 0)
                return false;

            seconds = duration / (double)timescale;
            return seconds >= 0;
        }

        if (version == 1)
        {
            if (payloadSize < 32)
                return false;

            uint timescale = ReadUInt32BigEndian(bytes, payloadOffset + 20);
            ulong duration = ReadUInt64BigEndian(bytes, payloadOffset + 24);

            if (timescale == 0)
                return false;

            seconds = duration / (double)timescale;
            return seconds >= 0;
        }

        return false;
    }

    private static bool IsContainerAtom(string atomType)
    {
        return atomType is "moov" or "trak" or "mdia" or "minf" or "stbl" or "edts" or "udta" or "meta";
    }

    private static uint ReadUInt32BigEndian(byte[] bytes, long offset)
    {
        int i = (int)offset;
        return ((uint)bytes[i] << 24)
             | ((uint)bytes[i + 1] << 16)
             | ((uint)bytes[i + 2] << 8)
             | bytes[i + 3];
    }

    private static ulong ReadUInt64BigEndian(byte[] bytes, long offset)
    {
        return ((ulong)ReadUInt32BigEndian(bytes, offset) << 32)
             | ReadUInt32BigEndian(bytes, offset + 4);
    }

    private static string ReadAscii(byte[] bytes, long offset, int length)
    {
        if (offset < 0 || offset + length > bytes.Length)
            return string.Empty;

        return System.Text.Encoding.ASCII.GetString(bytes, (int)offset, length);
    }
}
