using CommunityToolkit.Maui.Views;

namespace MiniTFG;

[QueryProperty(nameof(RecetaId), "recetaId")]
[QueryProperty(nameof(UsuarioId), "usuarioId")]
/// <summary>
/// Presenta los pasos de una receta y abre los vídeos asociados.
/// </summary>
public partial class RecipeStepsPage : ContentPage
{
    private int _recetaId;
    private int _usuarioId;
    private bool _loaded;

    public int RecetaId
    {
        get => _recetaId;
        set
        {
            _recetaId = value;
            TryCargarPasos();
        }
    }

    public int UsuarioId
    {
        get => _usuarioId;
        set
        {
            _usuarioId = value;
            TryCargarPasos();
        }
    }

    public RecipeStepsPage()
    {
        InitializeComponent();
    }

    private void TryCargarPasos()
    {
        if (_recetaId != 0 && _usuarioId != 0 && !_loaded)
        {
            _loaded = true;
            CargarPasos();
        }
    }

    private async void CargarPasos()
    {
        var api = new DatabaseService();

        var receta = await api.GetRecetaByIdAsync(_recetaId);
        if (receta != null)
            TituloLabel.Text = receta.Titulo;

        var pasos = await api.GetPasosRecetaAsync(_recetaId);
        PasosContainer.Children.Clear();

        foreach (var paso in pasos.OrderBy(p => p.NumeroPaso))
        {
            foreach (var video in paso.Videos.OrderBy(v => v.Orden))
                video.VideoFilePath = await CrearArchivoTemporalVideoAsync(video, paso.Id);

            PasosContainer.Children.Add(CrearPasoCard(paso));
        }
    }

    private View CrearPasoCard(PasoReceta paso)
    {
        var layout = new VerticalStackLayout
        {
            Spacing = 14
        };

        //
        // 🔶 1. TÍTULO DEL PASO
        //
        layout.Children.Add(new Label
        {
            Text = $"Paso {paso.NumeroPaso}",
            FontSize = 26,
            TextColor = Color.FromArgb("#FAC26C"),
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Start
        });

        //
        // 🔶 2. SEPARADOR
        //
        layout.Children.Add(new BoxView
        {
            HeightRequest = 2,
            BackgroundColor = Color.FromArgb("#FAC26C"),
            Opacity = 0.6,
            Margin = new Thickness(0, 4)
        });

        //
        // 🔶 3. DESCRIPCIÓN DEL PASO
        //
        layout.Children.Add(new Label
        {
            Text = string.IsNullOrWhiteSpace(paso.Descripcion) ? "Sin descripción" : paso.Descripcion,
            FontSize = 18,
            TextColor = Colors.White,
            HorizontalTextAlignment = TextAlignment.Start,
            LineBreakMode = LineBreakMode.WordWrap
        });

        //
        // 🔶 4. VIDEOS EN SCROLL HORIZONTAL
        //
        layout.Children.Add(CrearVideosScroll(paso));


        

        //
        // 🔶 TARJETA FINAL
        //
        return new Border
        {
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 18 },
            BackgroundColor = Color.FromArgb("#2D2D2D"),
            Padding = new Thickness(16),
            Content = layout
        };
    }


    private View CrearVideosScroll(PasoReceta paso)
    {
        var videosLayout = new HorizontalStackLayout
        {
            Spacing = 12,
            HorizontalOptions = LayoutOptions.Start,   // 👈 IMPORTANTE
            VerticalOptions = LayoutOptions.Start
        };

        var videos = paso.Videos
            .Where(v => !string.IsNullOrWhiteSpace(v.VideoFilePath))
            .OrderBy(v => v.Orden)
            .ToList();

        if (videos.Count == 0)
        {
            videosLayout.Children.Add(new Border
            {
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
                BackgroundColor = Color.FromArgb("#333333"),
                HeightRequest = 150,
                WidthRequest = 220,
                Content = new Label
                {
                    Text = "Sin vídeos",
                    TextColor = Colors.LightGray,
                    FontSize = 15,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            });
        }
        else
        {
            foreach (var video in videos)
                videosLayout.Children.Add(CrearVideoCard(video));   // cada card ya tiene WidthRequest = 220
        }

        return new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Always, // para que veas que hay scroll
            Content = videosLayout
        };
    }


    private View CrearVideoCard(PasoRecetaVideo video)
    {
        var grid = new Grid();

        // Icono de play grande
        grid.Children.Add(new Label
        {
            Text = "▶",
            FontSize = 46,
            TextColor = Colors.White,
            Opacity = 0.85,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        });

        // Texto "Vídeo X"
        grid.Children.Add(new Label
        {
            Text = video.Orden > 0 ? $"Vídeo {video.Orden}" : "Vídeo",
            FontSize = 13,
            TextColor = Colors.White,
            Opacity = 0.8,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.End,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Al pulsar → abre el vídeo con el reproductor del sistema (como antes)
        grid.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => AbrirVideo(video))
        });

        return new Border
        {
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 15 },
            BackgroundColor = Color.FromArgb("#333333"),
            HeightRequest = 150,
            WidthRequest = 220,
            Content = grid
        };
    }




    private async void AbrirVideo(PasoRecetaVideo video)
    {
        if (string.IsNullOrWhiteSpace(video.VideoFilePath))
            return;

        await Launcher.OpenAsync(new OpenFileRequest
        {
            File = new ReadOnlyFile(video.VideoFilePath)
        });
    }

    private static async Task<string> CrearArchivoTemporalVideoAsync(PasoRecetaVideo video, int pasoId)
    {
        if (string.IsNullOrWhiteSpace(video.Video))
            return null;

        try
        {
            var base64 = video.Video.Trim();
            var commaIndex = base64.IndexOf(',');
            if (base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && commaIndex >= 0)
                base64 = base64[(commaIndex + 1)..];

            var bytes = Convert.FromBase64String(base64);
            var extension = Path.GetExtension(video.VideoNombreArchivo);
            if (string.IsNullOrWhiteSpace(extension))
                extension = ".mp4";

            var nombreArchivo = $"paso_{pasoId}_video_{video.Orden}{extension}";
            var filePath = Path.Combine(FileSystem.CacheDirectory, nombreArchivo);
            await File.WriteAllBytesAsync(filePath, bytes);
            return filePath;
        }
        catch
        {
            return null;
        }
    }

    private async void OnCompletadoClicked(object sender, EventArgs e)
    {
        if (App.UsuarioActual == null)
        {
            await Shell.Current.GoToAsync("//home");
            return;
        }

        var popup = new StarRatingPopup(_usuarioId);
        var resultado = await this.ShowPopupAsync(popup);

        if (resultado is int estrellas)
        {
            await DisplayAlert("Gracias", $"Has valorado con {estrellas} estrellas", "OK");
        }

        await Shell.Current.GoToAsync("//home");
    }
}
