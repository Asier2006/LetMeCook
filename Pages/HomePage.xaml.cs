using CommunityToolkit.Maui.Views;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MiniTFG;

/// <summary>
/// Lista recetas, filtra resultados con etiquetas de BBDD y sincroniza acciones sociales con MySQL.
/// </summary>
public partial class HomePage : ContentPage
{
    public ObservableCollection<Receta> Recetas { get; set; } = new();
    public ObservableCollection<string> OrigenSugerencias { get; set; } = new();
    public ObservableCollection<string> TipoCocinaSugerencias { get; set; } = new();
    public ObservableCollection<string> IngredienteSugerencias { get; set; } = new();

    private List<Receta> TodasLasRecetas = new();
    private List<Receta> RecetasFiltradasBase = new();
    private List<string> _catalogoOrigenes = new();
    private List<string> _catalogoTiposCocina = new();
    private List<string> _catalogoIngredientes = new();

    private readonly HashSet<string> _origenesSeleccionados = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _tiposCocinaSeleccionados = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _ingredientesSeleccionados = new(StringComparer.OrdinalIgnoreCase);

    private HashSet<int> _likesUsuario = new();
    private HashSet<int> _usuariosValorados = new();
    private bool _filtrosPreferenciasActivos = true;
    private bool _estaCargandoRecetas = false;

    // 🔍 Búsqueda global
    private string _busquedaGlobal = string.Empty;

    public HomePage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        EntryBusquedaGlobal.Unfocus(); // evita que se abra solo

        await CargarCatalogosAsync();
        await CargarRecetasAsync();
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
            _catalogoOrigenes = new();
            _catalogoTiposCocina = new();
            _catalogoIngredientes = new();
        }
    }

    private async void OnCreatorClicked(object sender, TappedEventArgs e)
    {
        if (e.Parameter is int creadorId)
            await Shell.Current.GoToAsync($"other?usuarioId={creadorId}");
    }

    private List<Receta> FiltrarPorPreferencias(List<Receta> recetas, Usuarios usuario)
    {
        var filtradas = recetas.AsEnumerable();

        if (usuario.Gluten) filtradas = filtradas.Where(r => !r.Gluten);
        if (usuario.Lactosa) filtradas = filtradas.Where(r => !r.Lactosa);
        if (usuario.Huevo) filtradas = filtradas.Where(r => !r.Huevo);
        if (usuario.FrutosSecos) filtradas = filtradas.Where(r => !r.FrutosSecos);
        if (usuario.Marisco) filtradas = filtradas.Where(r => !r.Mariscos);
        if (usuario.Soja) filtradas = filtradas.Where(r => !r.Soja);
        if (usuario.Pescado) filtradas = filtradas.Where(r => !r.Pescado);
        if (usuario.Cacahuetes) filtradas = filtradas.Where(r => !r.Cacahuetes);
        if (usuario.Sesamo) filtradas = filtradas.Where(r => !r.Sesamo);
        if (usuario.Sulfitos) filtradas = filtradas.Where(r => !r.Sulfitos);
        if (usuario.Mostaza) filtradas = filtradas.Where(r => !r.Mostaza);
        if (usuario.Altramuces) filtradas = filtradas.Where(r => !r.Altramuces);
        if (usuario.Moluscos) filtradas = filtradas.Where(r => !r.Moluscos);
        if (usuario.Apio) filtradas = filtradas.Where(r => !r.Apio);

        if (usuario.Vegano) filtradas = filtradas.Where(r => r.Vegano);
        else if (usuario.Vegetariano) filtradas = filtradas.Where(r => r.Vegetariano);

        return filtradas.ToList();
    }

    // abre panel al enfocar el Entry
    private void OnToggleFiltros(object sender, FocusEventArgs e)
    {
        PanelFiltros.IsVisible = true;
    }

    private void OnCerrarFiltrosClicked(object sender, EventArgs e)
    {
        PanelFiltros.IsVisible = false;
        EntryBusquedaGlobal.Unfocus();
    }

    private void OnFiltroEntryCompleted(object sender, EventArgs e)
    {
        AplicarTextoComoEtiquetaSiProcede();
        Buscar();
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

    // 🔍 BÚSQUEDA GLOBAL
    private void OnBusquedaGlobalChanged(object sender, TextChangedEventArgs e)
    {
        _busquedaGlobal = e.NewTextValue ?? string.Empty;
        Buscar();
    }

    private void OnOrigenSugerenciaTapped(object sender, EventArgs e)
    {
        var etiqueta = ObtenerTextoEtiqueta(sender);
        AgregarEtiqueta(_origenesSeleccionados, etiqueta);
        EntryOrigen.Text = string.Empty;
        OrigenSugerencias.Clear();
        ActualizarTags();
        Buscar();
    }

    private void OnTipoCocinaSugerenciaTapped(object sender, EventArgs e)
    {
        var etiqueta = ObtenerTextoEtiqueta(sender);
        AgregarEtiqueta(_tiposCocinaSeleccionados, etiqueta);
        EntryTipoCocina.Text = string.Empty;
        TipoCocinaSugerencias.Clear();
        ActualizarTags();
        Buscar();
    }

    private void OnIngredienteSugerenciaTapped(object sender, EventArgs e)
    {
        var etiqueta = ObtenerTextoEtiqueta(sender);
        AgregarEtiqueta(_ingredientesSeleccionados, etiqueta);
        EntryIngrediente.Text = string.Empty;
        IngredienteSugerencias.Clear();
        ActualizarTags();
        Buscar();
    }

    private static string ObtenerTextoEtiqueta(object sender)
    {
        return sender switch
        {
            BindableObject bindable when bindable.BindingContext is string text => text,
            _ => null
        };
    }

    private static void AgregarEtiqueta(HashSet<string> destino, string etiqueta)
    {
        if (!string.IsNullOrWhiteSpace(etiqueta))
            destino.Add(etiqueta.Trim());
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

    private void SeleccionarPrimeraCoincidenciaSiExiste(string texto, List<string> catalogo, HashSet<string> destino)
    {
        if (string.IsNullOrWhiteSpace(texto) || destino.Count > 0)
            return;

        var match = catalogo.FirstOrDefault(x => ContieneNormalizado(x, texto));
        if (!string.IsNullOrWhiteSpace(match))
            destino.Add(match);
    }

    private void AplicarTextoComoEtiquetaSiProcede()
    {
        SeleccionarPrimeraCoincidenciaSiExiste(EntryOrigen.Text, _catalogoOrigenes, _origenesSeleccionados);
        SeleccionarPrimeraCoincidenciaSiExiste(EntryTipoCocina.Text, _catalogoTiposCocina, _tiposCocinaSeleccionados);
        SeleccionarPrimeraCoincidenciaSiExiste(EntryIngrediente.Text, _catalogoIngredientes, _ingredientesSeleccionados);

        ActualizarTags();
    }

    private void ActualizarTags()
    {
        TagsLayout.Children.Clear();
        OrigenTagsLayout.Children.Clear();
        TipoCocinaTagsLayout.Children.Clear();
        IngredienteTagsLayout.Children.Clear();

        if (!string.IsNullOrWhiteSpace(EntryComensales.Text))
            TagsLayout.Children.Add(CrearTag($"Comensales: {EntryComensales.Text.Trim()}", () => EntryComensales.Text = string.Empty));

        if (!string.IsNullOrWhiteSpace(EntryTiempo.Text))
            TagsLayout.Children.Add(CrearTag($"Tiempo: {EntryTiempo.Text.Trim()}", () => EntryTiempo.Text = string.Empty));

        foreach (var origen in _origenesSeleccionados.ToList())
        {
            OrigenTagsLayout.Children.Add(CrearTag(origen, () => QuitarEtiqueta(_origenesSeleccionados, origen)));
            TagsLayout.Children.Add(CrearTag($"Origen: {origen}", () => QuitarEtiqueta(_origenesSeleccionados, origen)));
        }

        foreach (var tipo in _tiposCocinaSeleccionados.ToList())
        {
            TipoCocinaTagsLayout.Children.Add(CrearTag(tipo, () => QuitarEtiqueta(_tiposCocinaSeleccionados, tipo)));
            TagsLayout.Children.Add(CrearTag($"Cocina: {tipo}", () => QuitarEtiqueta(_tiposCocinaSeleccionados, tipo)));
        }

        foreach (var ingrediente in _ingredientesSeleccionados.ToList())
        {
            IngredienteTagsLayout.Children.Add(CrearTag(ingrediente, () => QuitarEtiqueta(_ingredientesSeleccionados, ingrediente)));
            TagsLayout.Children.Add(CrearTag($"Ingrediente: {ingrediente}", () => QuitarEtiqueta(_ingredientesSeleccionados, ingrediente)));
        }
    }

    private View CrearTag(string texto, Action quitar)
    {
        var tag = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            BackgroundColor = Color.FromArgb("#FAC26C"),
            Padding = new Thickness(10, 5),
            Margin = new Thickness(2),
            Content = new Label
            {
                Text = $"{texto} ✕",
                TextColor = Colors.White,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold
            }
        };

        tag.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                quitar?.Invoke();
                Buscar();
            })
        });

        return tag;
    }

    private void QuitarEtiqueta(HashSet<string> origen, string etiqueta)
    {
        origen.Remove(etiqueta);
        ActualizarTags();
        Buscar();
    }

    private void OnBuscarClicked(object sender, EventArgs e)
    {
        Buscar();
    }

    private void Buscar()
    {
        AplicarTextoComoEtiquetaSiProcede();

        var baseList = _filtrosPreferenciasActivos
            ? (RecetasFiltradasBase ?? new List<Receta>())
            : (TodasLasRecetas ?? new List<Receta>());

        var resultado = baseList.AsEnumerable();

        if (int.TryParse(EntryComensales.Text?.Trim(), out int comensales))
            resultado = resultado.Where(r => r.Comensales == comensales);

        if (!string.IsNullOrWhiteSpace(EntryTiempo.Text))
        {
            var tiempoBusqueda = EntryTiempo.Text.Trim();

            if (TryObtenerMinutos(tiempoBusqueda, out int minutosMaximos))
            {
                resultado = resultado.Where(r =>
                    TryObtenerMinutos(r.TiempoPreparacion, out int minutosReceta)
                    && minutosReceta <= minutosMaximos);
            }
            else
            {
                resultado = resultado.Where(r => ContieneNormalizado(r.TiempoPreparacion, tiempoBusqueda));
            }
        }

        if (_origenesSeleccionados.Count > 0)
            resultado = resultado.Where(r => _origenesSeleccionados.Any(tag => ContieneNormalizado(r.OrigenDelPlato, tag)));
        else if (!string.IsNullOrWhiteSpace(EntryOrigen.Text))
            resultado = resultado.Where(r => ContieneNormalizado(r.OrigenDelPlato, EntryOrigen.Text));

        if (_tiposCocinaSeleccionados.Count > 0)
            resultado = resultado.Where(r => _tiposCocinaSeleccionados.Any(tag => ContieneNormalizado(r.TipoCocina, tag)));
        else if (!string.IsNullOrWhiteSpace(EntryTipoCocina.Text))
            resultado = resultado.Where(r => ContieneNormalizado(r.TipoCocina, EntryTipoCocina.Text));

        if (_ingredientesSeleccionados.Count > 0)
            resultado = resultado.Where(r => _ingredientesSeleccionados.Any(tag => ContieneNormalizado(r.IngredientePrincipal, tag)));
        else if (!string.IsNullOrWhiteSpace(EntryIngrediente.Text))
            resultado = resultado.Where(r => ContieneNormalizado(r.IngredientePrincipal, EntryIngrediente.Text));

        // 🔍 BÚSQUEDA GLOBAL
        if (!string.IsNullOrWhiteSpace(_busquedaGlobal))
        {
            var texto = _busquedaGlobal;

            resultado = resultado.Where(r =>
                ContieneNormalizado(r.Titulo, texto) ||
                ContieneNormalizado(r.OrigenDelPlato, texto) ||
                ContieneNormalizado(r.TipoCocina, texto) ||
                ContieneNormalizado(r.IngredientePrincipal, texto));
        }

        Recetas.Clear();
        foreach (var r in resultado)
            Recetas.Add(r);

        ActualizarTags();
    }

    private void OnTogglePreferenciasClicked(object sender, EventArgs e)
    {
        _filtrosPreferenciasActivos = !_filtrosPreferenciasActivos;
        BtnTogglePreferencias.Text = _filtrosPreferenciasActivos
            ? "Desactivar filtros preferencias"
            : "Activar filtros preferencias";
        BtnTogglePreferencias.BackgroundColor = _filtrosPreferenciasActivos
            ? Color.FromArgb("#444")
            : Color.FromArgb("#6A0DAD");

        Buscar();
    }

    private async Task CargarRecetasAsync()
    {
        if (_estaCargandoRecetas)
            return;

        _estaCargandoRecetas = true;

        try
        {
            var api = new DatabaseService();
            int usuarioId = App.UsuarioActual?.Id ?? 0;

            _likesUsuario.Clear();
            _usuariosValorados.Clear();

            var lista = await api.GetRecetasAsync();

            if (lista == null)
                return;

            var likesPorReceta = await api.GetLikesPorRecetaAsync(lista.Select(r => r.Id));

            if (usuarioId != 0)
            {
                try
                {
                    var likesUsuario = await api.GetLikesUsuarioAsync(usuarioId);
                    _likesUsuario = likesUsuario != null
                        ? new HashSet<int>(likesUsuario.Select(l => l.RecetaId))
                        : new HashSet<int>();

                    var valoracionesHechas = await api.GetValoracionesPorUsuarioAsync(usuarioId);
                    _usuariosValorados = valoracionesHechas != null
                        ? new HashSet<int>(valoracionesHechas.Select(v => v.UsuarioValoradoId))
                        : new HashSet<int>();
                }
                catch
                {
                    _likesUsuario = new();
                    _usuariosValorados = new();
                }
            }

            foreach (var r in lista)
            {
                var creador = await api.GetUsuarioByIdAsync(r.UsuarioId);

                r.CreadorNombre = creador?.Nombre ?? "Usuario";
                r.CreadorFoto = creador?.Foto ?? "user.png";
                r.CreadorFotoSource = await api.GetImageSourceAsync(r.CreadorFoto, "user.png");

                r.Likes = likesPorReceta.TryGetValue(r.Id, out int totalLikes) ? totalLikes : 0;

                r.UsuarioHaDadoLike = false;
                r.UsuarioHaValorado = false;

                if (usuarioId != 0)
                {
                    r.UsuarioHaDadoLike = _likesUsuario.Contains(r.Id);
                    r.UsuarioHaValorado = _usuariosValorados.Contains(r.UsuarioId);
                }

                r.ImagenSource = await api.GetImageSourceAsync(r.Imagen, "recipes.png");
            }

            TodasLasRecetas = lista.ToList();
            RecetasFiltradasBase = App.UsuarioActual != null
                ? FiltrarPorPreferencias(TodasLasRecetas, App.UsuarioActual)
                : TodasLasRecetas.ToList();

            Buscar();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudieron cargar las recetas: {ex.Message}", "OK");
        }
        finally
        {
            _estaCargandoRecetas = false;
        }
    }

    private async void LikeClicked(object sender, EventArgs e)
    {
        var api = new DatabaseService();
        var img = (Image)sender;
        var receta = (Receta)img.BindingContext;

        if (App.UsuarioActual == null)
        {
            await DisplayAlertAsync("Debe iniciar sesión", "Inicie sesión para dar like.", "OK");
            return;
        }

        int usuarioId = App.UsuarioActual.Id;
        int recetaId = receta.Id;

        if (!receta.UsuarioHaDadoLike)
        {
            try
            {
                await api.PostLikeAsync(new Like { UsuarioId = usuarioId, RecetaId = recetaId });
                receta.UsuarioHaDadoLike = true;
                receta.Likes++;
                _likesUsuario.Add(recetaId);
            }
            catch
            {
                await SafeRefreshLikes(api, usuarioId);
                receta.UsuarioHaDadoLike = _likesUsuario.Contains(recetaId);
            }
        }
        else
        {
            try
            {
                await api.DeleteLikeAsync(usuarioId, recetaId);
                receta.UsuarioHaDadoLike = false;
                receta.Likes = Math.Max(0, receta.Likes - 1);
                _likesUsuario.Remove(recetaId);
            }
            catch
            {
                await SafeRefreshLikes(api, usuarioId);
                receta.UsuarioHaDadoLike = _likesUsuario.Contains(recetaId);
            }
        }

        img.Source = receta.IconoLike;
    }

    private async void PuntuarClicked(object sender, EventArgs e)
    {
        var img = (Image)sender;
        var receta = (Receta)img.BindingContext;

        if (App.UsuarioActual == null)
        {
            await DisplayAlertAsync("Debe iniciar sesión", "Inicie sesión para valorar.", "OK");
            return;
        }

        var popup = new StarRatingPopup(receta.UsuarioId);
        var resultado = await this.ShowPopupAsync(popup);

        if (resultado is int estrellas)
        {
            receta.UsuarioHaValorado = true;
            _usuariosValorados.Add(receta.UsuarioId);
            img.Source = receta.IconoEstrella;
            await DisplayAlertAsync("Gracias", $"Has valorado con {estrellas} estrellas", "OK");
        }
    }

    private async Task SafeRefreshLikes(DatabaseService api, int usuarioId)
    {
        try
        {
            var likesFromServer = await api.GetLikesUsuarioAsync(usuarioId);
            _likesUsuario = new HashSet<int>(likesFromServer.Select(l => l.RecetaId));
        }
        catch
        {
        }
    }

    private async void OnRecetaTapped(object sender, EventArgs e)
    {
        var border = (Border)sender;
        var receta = (Receta)border.BindingContext;

        var popup = new RecipeDetailPopup(receta);
        var resultado = await this.ShowPopupAsync(popup);

        if (resultado is int[] ids && ids.Length == 2)
            await Shell.Current.GoToAsync($"recipesteps?recetaId={ids[0]}&usuarioId={ids[1]}");
    }

    private async Task PulsarBoton(Button boton)
    {
        var normal = Color.FromArgb("#FAC26C");
        var oscuro = Color.FromArgb("#E0A85A");

        boton.BackgroundColor = oscuro;
        await Task.Delay(120);
        boton.BackgroundColor = normal;
    }

    private async void InicioClicked(object sender, EventArgs e)
    {
        await PulsarBoton(BtnInicio);
        await CargarRecetasAsync();
        await Shell.Current.GoToAsync("//home");
    }

    private async void RecetasClicked(object sender, EventArgs e)
    {
        await PulsarBoton(BtnRecetas);
        await Shell.Current.GoToAsync("//recipes");
    }

    private async void PerfilClicked(object sender, EventArgs e)
    {
        await PulsarBoton(BtnPerfil);

        if (App.UsuarioActual == null)
        {
            bool irLogin = await DisplayAlertAsync(
                "Inicia sesión",
                "Para acceder a tu perfil necesitas iniciar sesión.",
                "Iniciar sesión",
                "Cancelar");

            if (irLogin)
                await Shell.Current.GoToAsync("//login");

            return;
        }

        await Shell.Current.GoToAsync("//profile");
    }

    private static bool ContieneNormalizado(string origen, string busqueda)
    {
        if (string.IsNullOrWhiteSpace(origen) || string.IsNullOrWhiteSpace(busqueda))
            return false;

        return Normalizar(origen).Contains(Normalizar(busqueda), StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryObtenerMinutos(string texto, out int minutos)
    {
        minutos = 0;

        if (string.IsNullOrWhiteSpace(texto))
            return false;

        var normalizado = Normalizar(texto)
            .Replace("horas", "h", StringComparison.OrdinalIgnoreCase)
            .Replace("hora", "h", StringComparison.OrdinalIgnoreCase)
            .Replace("hrs", "h", StringComparison.OrdinalIgnoreCase)
            .Replace("hr", "h", StringComparison.OrdinalIgnoreCase)
            .Replace("minutos", "min", StringComparison.OrdinalIgnoreCase)
            .Replace("minuto", "min", StringComparison.OrdinalIgnoreCase)
            .Replace("mins", "min", StringComparison.OrdinalIgnoreCase);

        var formatoHorasMinutos = Regex.Match(normalizado, @"(?<!\d)(?<horas>\d{1,2})\s*:\s*(?<minutos>\d{1,2})(?!\d)");
        if (formatoHorasMinutos.Success
            && int.TryParse(formatoHorasMinutos.Groups["horas"].Value, out int h)
            && int.TryParse(formatoHorasMinutos.Groups["minutos"].Value, out int m))
        {
            minutos = (h * 60) + m;
            return minutos >= 0;
        }

        var total = 0;
        var tieneUnidades = false;

        foreach (Match match in Regex.Matches(normalizado, @"(?<valor>\d+(?:[\.,]\d+)?)\s*(?<unidad>h|min|m)"))
        {
            if (!double.TryParse(match.Groups["valor"].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double valor))
                continue;

            var unidad = match.Groups["unidad"].Value;
            total += unidad == "h"
                ? (int)Math.Round(valor * 60, MidpointRounding.AwayFromZero)
                : (int)Math.Round(valor, MidpointRounding.AwayFromZero);

            tieneUnidades = true;
        }

        if (tieneUnidades)
        {
            minutos = total;
            return minutos >= 0;
        }

        var numeroSuelto = Regex.Match(normalizado, @"(?<!\d)(?<valor>\d+(?:[\.,]\d+)?)(?!\d)");
        if (numeroSuelto.Success
            && double.TryParse(numeroSuelto.Groups["valor"].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double minutosNumericos))
        {
            minutos = (int)Math.Round(minutosNumericos, MidpointRounding.AwayFromZero);
            return minutos >= 0;
        }

        return false;
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
