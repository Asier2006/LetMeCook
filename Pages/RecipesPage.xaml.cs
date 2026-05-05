using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using Microsoft.Maui.Storage;

namespace MiniTFG;

/// <summary>
/// Formulario de creación básica. Los pasos se añaden después desde la página avanzada Let me Cook.
/// </summary>
public partial class RecipesPage : ContentPage
{
    public ObservableCollection<AlergenosPreferencias> Alergenos { get; set; }
    public ObservableCollection<AlergenosPreferencias> Preferencias { get; set; }
    public ObservableCollection<string> OrigenSugerencias { get; set; } = new();
    public ObservableCollection<string> TipoCocinaSugerencias { get; set; } = new();
    public ObservableCollection<string> IngredienteSugerencias { get; set; } = new();

    private readonly HashSet<string> _origenesSeleccionados = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _tiposCocinaSeleccionados = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _ingredientesSeleccionados = new(StringComparer.OrdinalIgnoreCase);

    private List<string> _catalogoOrigenes = new();
    private List<string> _catalogoTiposCocina = new();
    private List<string> _catalogoIngredientes = new();
    private string imagenBase64 = null;
    private bool _guardando;

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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarCatalogosAsync();
    }

    private async Task CargarCatalogosAsync()
    {
        try
        {
            var api = new DatabaseService();
            await api.EnsureCatalogosAsync();
            _catalogoOrigenes = await api.GetOrigenesPlatoAsync();
            _catalogoTiposCocina = await api.GetTiposCocinaAsync();
            _catalogoIngredientes = await api.GetIngredientesAsync();
        }
        catch
        {
            _catalogoOrigenes = new List<string>();
            _catalogoTiposCocina = new List<string>();
            _catalogoIngredientes = new List<string>();
        }
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

    private async void SeleccionarImagenClicked(object sender, EventArgs e)
    {
        string opcion = await DisplayActionSheetAsync(
            "Seleccionar imagen",
            "Cancelar",
            null,
            "Hacer foto",
            "Elegir de la galería");

        if (opcion == "Hacer foto")
            await TomarFoto();
        else if (opcion == "Elegir de la galería")
            await ElegirDeGaleria();
    }

    private async Task TomarFoto()
    {
        try
        {
            var foto = await MediaPicker.CapturePhotoAsync();

            if (foto == null)
                return;

            using var stream = await foto.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            imagenBase64 = Convert.ToBase64String(memoryStream.ToArray());
            ImagenPreview.Source = ImageSource.FromFile(foto.FullPath);
        }
        catch
        {
            await DisplayAlertAsync("Error", "No se pudo abrir la cámara", "OK");
        }
    }

    private async Task ElegirDeGaleria()
    {
        try
        {
            var foto = await MediaPicker.PickPhotoAsync();

            if (foto == null)
                return;

            using var stream = await foto.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            imagenBase64 = Convert.ToBase64String(memoryStream.ToArray());
            ImagenPreview.Source = ImageSource.FromFile(foto.FullPath);
        }
        catch
        {
            await DisplayAlertAsync("Error", "No se pudo seleccionar la imagen", "OK");
        }
    }

    private void OnOrigenTextChanged(object sender, TextChangedEventArgs e)
    {
        ActualizarSugerencias(_catalogoOrigenes, e.NewTextValue, OrigenSugerencias);
    }

    private void OnTipoCocinaTextChanged(object sender, TextChangedEventArgs e)
    {
        ActualizarSugerencias(_catalogoTiposCocina, e.NewTextValue, TipoCocinaSugerencias);
    }

    private void OnIngredienteTextChanged(object sender, TextChangedEventArgs e)
    {
        ActualizarSugerencias(_catalogoIngredientes, e.NewTextValue, IngredienteSugerencias);
    }

    private void OnOrigenSugerenciaTapped(object sender, EventArgs e)
    {
        AgregarEtiqueta(_origenesSeleccionados, ObtenerTextoEtiqueta(sender));
        OrigenEntry.Text = string.Empty;
        OrigenSugerencias.Clear();
        ActualizarTagsVisuales();
    }

    private void OnTipoCocinaSugerenciaTapped(object sender, EventArgs e)
    {
        AgregarEtiqueta(_tiposCocinaSeleccionados, ObtenerTextoEtiqueta(sender));
        TipoCocinaEntry.Text = string.Empty;
        TipoCocinaSugerencias.Clear();
        ActualizarTagsVisuales();
    }

    private void OnIngredienteSugerenciaTapped(object sender, EventArgs e)
    {
        AgregarEtiqueta(_ingredientesSeleccionados, ObtenerTextoEtiqueta(sender));
        IngredienteEntry.Text = string.Empty;
        IngredienteSugerencias.Clear();
        ActualizarTagsVisuales();
    }

    private static string ObtenerTextoEtiqueta(object sender)
    {
        return sender switch
        {
            BindableObject bindable when bindable.BindingContext is string text => text,
            _ => null
        };
    }

    private static void ActualizarSugerencias(IEnumerable<string> catalogo, string busqueda, ObservableCollection<string> destino)
    {
        destino.Clear();

        if (string.IsNullOrWhiteSpace(busqueda))
            return;

        foreach (var item in catalogo
                     .Where(x => ContieneNormalizado(x, busqueda))
                     .OrderBy(x => x)
                     .Take(8))
        {
            destino.Add(item);
        }
    }

    private void AgregarEtiqueta(HashSet<string> destino, string etiqueta)
    {
        if (!string.IsNullOrWhiteSpace(etiqueta))
            destino.Add(etiqueta.Trim());
    }

    private void ActualizarTagsVisuales()
    {
        PintarTags(OrigenTagsLayout, _origenesSeleccionados);
        PintarTags(TipoCocinaTagsLayout, _tiposCocinaSeleccionados);
        PintarTags(IngredienteTagsLayout, _ingredientesSeleccionados);
    }

    private void PintarTags(FlexLayout layout, HashSet<string> etiquetas)
    {
        layout.Children.Clear();

        foreach (var etiqueta in etiquetas.ToList())
            layout.Children.Add(CrearTag(etiqueta, () => etiquetas.Remove(etiqueta)));
    }

    private View CrearTag(string texto, Action quitar)
    {
        var tag = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            BackgroundColor = Color.FromArgb("#6A0DAD"),
            Padding = new Thickness(10, 5),
            Margin = new Thickness(2),
            Content = new Label
            {
                Text = $"{texto} ✕",
                TextColor = Colors.White,
                FontSize = 12
            }
        };

        tag.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                quitar?.Invoke();
                ActualizarTagsVisuales();
            })
        });

        return tag;
    }

    private void CompletarEtiquetasDesdeTexto()
    {
        CompletarEtiqueta(OrigenEntry.Text, _catalogoOrigenes, _origenesSeleccionados);
        CompletarEtiqueta(TipoCocinaEntry.Text, _catalogoTiposCocina, _tiposCocinaSeleccionados);
        CompletarEtiqueta(IngredienteEntry.Text, _catalogoIngredientes, _ingredientesSeleccionados);
    }

    private void CompletarEtiqueta(string texto, List<string> catalogo, HashSet<string> destino)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return;

        var match = catalogo.FirstOrDefault(x => ContieneNormalizado(x, texto));
        destino.Add((match ?? texto).Trim());
    }

    private async void GuardarRecetaClicked(object sender, EventArgs e)
    {
        var creada = await GuardarRecetaBaseAsync();

        if (creada == null)
            return;

        await DisplayAlertAsync("Éxito", "Receta creada correctamente. Puedes completarla más tarde desde Let me Cook.", "OK");
        LimpiarFormulario();
        await Shell.Current.GoToAsync("//home");
    }

    private async void LetMeCookClicked(object sender, EventArgs e)
    {
        var creada = await GuardarRecetaBaseAsync();

        if (creada == null)
            return;

        LimpiarFormulario();
        await Shell.Current.GoToAsync($"letmecook?recetaId={creada.Id}&usuarioId={creada.UsuarioId}");
    }

    private async Task<Receta> GuardarRecetaBaseAsync()
    {
        if (_guardando)
            return null;

        if (App.UsuarioActual == null)
        {
            await DisplayAlertAsync("Error", "Debes iniciar sesión para crear una receta.", "OK");
            return null;
        }

        if (string.IsNullOrWhiteSpace(NombreEntry.Text) ||
            string.IsNullOrWhiteSpace(DescripcionEntry.Text) ||
            string.IsNullOrWhiteSpace(ComensalesEntry.Text))
        {
            await DisplayAlertAsync("Campos obligatorios", "Rellena nombre, descripción y comensales.", "OK");
            return null;
        }

        if (!int.TryParse(ComensalesEntry.Text, out int comensales) || comensales <= 0)
        {
            await DisplayAlertAsync("Error", "Los comensales deben ser un número mayor que 0.", "OK");
            return null;
        }

        _guardando = true;

        try
        {
            CompletarEtiquetasDesdeTexto();
            ActualizarTagsVisuales();

            var receta = new Receta
            {
                UsuarioId = App.UsuarioActual.Id,
                Titulo = NombreEntry.Text.Trim(),
                Imagen = imagenBase64,
                Descripcion = DescripcionEntry.Text.Trim(),
                Comensales = comensales,
                OrigenDelPlato = _origenesSeleccionados.Count == 0 ? null : string.Join(", ", _origenesSeleccionados),
                TiempoPreparacion = string.IsNullOrWhiteSpace(TiempoEntry.Text) ? null : TiempoEntry.Text.Trim(),
                TipoCocina = _tiposCocinaSeleccionados.Count == 0 ? null : string.Join(", ", _tiposCocinaSeleccionados),
                IngredientePrincipal = _ingredientesSeleccionados.Count == 0 ? null : string.Join(", ", _ingredientesSeleccionados)
            };

            AplicarAlergenosYPreferencias(receta);

            var api = new DatabaseService();
            var creada = await api.PostRecetaAsync(receta);

            if (creada == null)
            {
                await DisplayAlertAsync("Error", "No se pudo guardar la receta.", "OK");
                return null;
            }

            await api.GuardarEtiquetasRecetaAsync(
                creada.Id,
                _origenesSeleccionados,
                _tiposCocinaSeleccionados,
                _ingredientesSeleccionados);

            int puntos = CalcularPuntosBase();
            if (puntos > 0)
                await api.SumarPuntosUsuarioAsync(App.UsuarioActual.Id, puntos);

            return creada;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudo crear la receta: {ex.Message}", "OK");
            return null;
        }
        finally
        {
            _guardando = false;
        }
    }

    private int CalcularPuntosBase()
    {
        int puntos = 0;

        if (!string.IsNullOrWhiteSpace(imagenBase64)) puntos += 1;
        if (!string.IsNullOrWhiteSpace(TiempoEntry.Text)) puntos += 1;
        if (_origenesSeleccionados.Count > 0) puntos += 1;
        if (_tiposCocinaSeleccionados.Count > 0) puntos += 1;
        if (_ingredientesSeleccionados.Count > 0) puntos += 1;

        return puntos;
    }

    private void AplicarAlergenosYPreferencias(Receta receta)
    {
        foreach (var alergeno in Alergenos)
        {
            switch (alergeno.Nombre)
            {
                case "Gluten": receta.Gluten = alergeno.Seleccion; break;
                case "Leche": receta.Lactosa = alergeno.Seleccion; break;
                case "Frutos secos": receta.FrutosSecos = alergeno.Seleccion; break;
                case "Marisco": receta.Mariscos = alergeno.Seleccion; break;
                case "Huevos": receta.Huevo = alergeno.Seleccion; break;
                case "Soja": receta.Soja = alergeno.Seleccion; break;
                case "Pescado": receta.Pescado = alergeno.Seleccion; break;
                case "Cacahuetes": receta.Cacahuetes = alergeno.Seleccion; break;
                case "Sésamo": receta.Sesamo = alergeno.Seleccion; break;
                case "Sulfitos": receta.Sulfitos = alergeno.Seleccion; break;
                case "Mostaza": receta.Mostaza = alergeno.Seleccion; break;
                case "Altramuces": receta.Altramuces = alergeno.Seleccion; break;
                case "Moluscos": receta.Moluscos = alergeno.Seleccion; break;
                case "Apio": receta.Apio = alergeno.Seleccion; break;
            }
        }

        foreach (var preferencia in Preferencias)
        {
            switch (preferencia.Nombre)
            {
                case "Vegano": receta.Vegano = preferencia.Seleccion; break;
                case "Vegetariano": receta.Vegetariano = preferencia.Seleccion; break;
            }
        }
    }

    private void LimpiarFormulario()
    {
        imagenBase64 = null;
        NombreEntry.Text = string.Empty;
        DescripcionEntry.Text = string.Empty;
        ComensalesEntry.Text = string.Empty;
        TiempoEntry.Text = string.Empty;
        OrigenEntry.Text = string.Empty;
        TipoCocinaEntry.Text = string.Empty;
        IngredienteEntry.Text = string.Empty;
        ImagenPreview.Source = null;

        _origenesSeleccionados.Clear();
        _tiposCocinaSeleccionados.Clear();
        _ingredientesSeleccionados.Clear();
        OrigenSugerencias.Clear();
        TipoCocinaSugerencias.Clear();
        IngredienteSugerencias.Clear();
        ActualizarTagsVisuales();

        foreach (var alergeno in Alergenos)
            alergeno.Seleccion = false;

        foreach (var preferencia in Preferencias)
            preferencia.Seleccion = false;
    }

    private static bool ContieneNormalizado(string origen, string busqueda)
    {
        if (string.IsNullOrWhiteSpace(origen) || string.IsNullOrWhiteSpace(busqueda))
            return false;

        return Normalizar(origen).Contains(Normalizar(busqueda), StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalizar(string texto)
    {
        var normalized = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
