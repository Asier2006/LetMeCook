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

    // Espera a recibir recetaId y usuarioId desde Shell antes de consultar la base de datos.
    private void TryCargarPasos()
    {
        if (_recetaId != 0 && _usuarioId != 0 && !_loaded)
        {
            _loaded = true;
            CargarPasos();
        }
    }

    // Lee los pasos de MySQL y reconstruye los vídeos Base64 como archivos temporales reproducibles.
    private async void CargarPasos()
    {
        var api = new DatabaseService();

        var receta = await api.GetRecetaByIdAsync(_recetaId);
        if (receta != null)
            TituloLabel.Text = receta.Titulo;

        var pasos = await api.GetPasosRecetaAsync(_recetaId);

        foreach (var paso in pasos)
        {
            if (!string.IsNullOrEmpty(paso.Video))
            {
                try
                {
                    var bytes = Convert.FromBase64String(paso.Video);
                    var nombreArchivo = string.IsNullOrWhiteSpace(paso.VideoNombreArchivo)
                        ? $"paso_{paso.Id}.mp4"
                        : $"{paso.Id}_{Path.GetFileName(paso.VideoNombreArchivo)}";

                    var filePath = Path.Combine(FileSystem.CacheDirectory, nombreArchivo);
                    await File.WriteAllBytesAsync(filePath, bytes);
                    paso.VideoFilePath = filePath;
                }
                catch
                {
                    paso.VideoFilePath = null;
                }
            }
        }

        var items = pasos.OrderBy(p => p.NumeroPaso).ToList();

        // Añadir item final "Completado"
        items.Add(new PasoReceta
        {
            NumeroPaso = -1,
            Descripcion = "__COMPLETADO__"
        });

        PasosCarousel.ItemTemplate = new PasoDataTemplateSelector(OnVideoTapped, OnCompletadoClicked);
        PasosCarousel.ItemsSource = items;
    }

    private async void OnVideoTapped(object sender, EventArgs e)
    {
        var element = (View)sender;
        var paso = (PasoReceta)element.BindingContext;

        if (!string.IsNullOrEmpty(paso.VideoFilePath))
        {
            await Launcher.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(paso.VideoFilePath)
            });
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

/// <summary>
/// Selecciona la plantilla visual de paso normal o pantalla final de completado.
/// </summary>
public class PasoDataTemplateSelector : DataTemplateSelector
{
    private readonly DataTemplate _pasoTemplate;
    private readonly DataTemplate _completadoTemplate;

    public PasoDataTemplateSelector(EventHandler videoTapped, EventHandler completadoClicked)
    {
        _pasoTemplate = new DataTemplate(() =>
        {
            var layout = new VerticalStackLayout
            {
                Padding = 20,
                Spacing = 20
            };

            // TÍTULO DEL PASO (GRANDE, NARANJA)
            var stepLabel = new Label
            {
                FontSize = 26,
                TextColor = Color.FromArgb("#FAC26C"),
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center
            };
            stepLabel.SetBinding(Label.TextProperty, "NumeroPaso", stringFormat: "Paso {0}");
            layout.Children.Add(stepLabel);

            // DESCRIPCIÓN DEL PASO (MÁS PEQUEÑA)
            var descLabel = new Label
            {
                FontSize = 16,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(10, 0)
            };
            descLabel.SetBinding(Label.TextProperty, "Descripcion");
            layout.Children.Add(descLabel);

            // CONTENEDOR DE VÍDEO PEQUEÑO CON PLAY
            var videoBorder = new Border
            {
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 15 },
                BackgroundColor = Color.FromArgb("#333333"),
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                HeightRequest = 160,
                WidthRequest = 220,
                Margin = new Thickness(0, 10)
            };

            var videoGrid = new Grid();
            videoGrid.SetBinding(BindableObject.BindingContextProperty, ".");

            var playLabel = new Label
            {
                Text = "▶",
                FontSize = 48,
                TextColor = Colors.White,
                Opacity = 0.8,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            videoGrid.Children.Add(playLabel);

            videoGrid.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() => videoTapped(videoGrid, EventArgs.Empty))
            });

            videoBorder.Content = videoGrid;

            layout.Children.Add(videoBorder);

            return layout;
        });

        _completadoTemplate = new DataTemplate(() =>
        {
            var layout = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center, // ← AQUÍ EL CAMBIO: LayoutOptions.Center
                Spacing = 20,
                Padding = new Thickness(30)
            };

            layout.Children.Add(new Label
            {
                Text = "🎉",
                FontSize = 60,
                HorizontalOptions = LayoutOptions.Center
            });

            layout.Children.Add(new Label
            {
                Text = "¡Has completado la receta!",
                FontSize = 24,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center
            });

            var btn = new Button
            {
                Text = "Completado",
                BackgroundColor = Color.FromArgb("#FAC26C"),
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 15,
                HeightRequest = 55,
                FontSize = 18
            };
            btn.Clicked += completadoClicked;
            layout.Children.Add(btn);

            return layout;
        });
    }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        var paso = (PasoReceta)item;
        return paso.NumeroPaso == -1 ? _completadoTemplate : _pasoTemplate;
    }
}
