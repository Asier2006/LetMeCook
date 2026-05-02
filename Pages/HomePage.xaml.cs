using CommunityToolkit.Maui.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MiniTFG;

/// <summary>
/// Lista recetas, filtra resultados y sincroniza acciones sociales con MySQL.
/// </summary>
public partial class HomePage : ContentPage
{
    public ObservableCollection<Receta> Recetas { get; set; } = new();
    private List<Receta> TodasLasRecetas;
    private List<Receta> RecetasFiltradasBase;
    public int Likes { get; set; }
    public bool UsuarioHaDadoLike { get; set; }

    // almacena localmente los ids de recetas que el usuario ya ha marcado como like
    private HashSet<int> _likesUsuario = new();
    private HashSet<int> _usuariosValorados = new();
    private bool _filtrosPreferenciasActivos = true;
    private bool _estaCargandoRecetas = false;

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Cada vez que se entra al Home se recargan los likes y valoraciones
        // del usuario actual. Esto evita mantener datos de la cuenta anterior.
        await CargarRecetasAsync();
    }

    public HomePage()
    {
        InitializeComponent();
        BindingContext = this;

    }

    private async void OnCreatorClicked(object sender, EventArgs e)
    {
        // Navega al perfil público del creador usando su id como parámetro de Shell.
        var button = sender as Button;
        int creadorId = (int)button.CommandParameter;

        await Shell.Current.GoToAsync($"other?usuarioId={creadorId}");
    }


    // Aplica filtros automáticos: excluye alérgenos marcados y respeta dieta vegana/vegetariana.
    private List<Receta> FiltrarPorPreferencias(List<Receta> recetas, Usuario usuario)
    {
        // Parte de todas las recetas y va descartando las que no cumplen las preferencias del usuario.
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

    private void OnToggleFiltros(object sender, EventArgs e)
    {
        PanelFiltros.IsVisible = !PanelFiltros.IsVisible;
    }

    private void OnFiltroEntryCompleted(object sender, EventArgs e)
    {
        ActualizarTags();
    }

    private void ActualizarTags()
    {
        TagsLayout.Children.Clear();

        var filtros = new (string Nombre, string Valor)[]
        {
            ("Comensales", EntryComensales.Text),
            ("Origen", EntryOrigen.Text),
            ("Tiempo", EntryTiempo.Text),
            ("Cocina", EntryTipoCocina.Text),
            ("Ingrediente", EntryIngrediente.Text)
        };

        foreach (var (nombre, valor) in filtros)
        {
            if (string.IsNullOrWhiteSpace(valor)) continue;

            var tag = new Border
            {
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                BackgroundColor = Color.FromArgb("#6A0DAD"),
                Padding = new Thickness(10, 5),
                Margin = new Thickness(2)
            };

            var label = new Label
            {
                Text = $"{nombre}: {valor.Trim()} ✕",
                TextColor = Colors.White,
                FontSize = 12
            };

            string nombreCaptura = nombre;
            tag.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() =>
                {
                    switch (nombreCaptura)
                    {
                        case "Comensales": EntryComensales.Text = string.Empty; break;
                        case "Origen": EntryOrigen.Text = string.Empty; break;
                        case "Tiempo": EntryTiempo.Text = string.Empty; break;
                        case "Cocina": EntryTipoCocina.Text = string.Empty; break;
                        case "Ingrediente": EntryIngrediente.Text = string.Empty; break;
                    }
                    ActualizarTags();
                    OnBuscarClicked(null, null);
                })
            });

            tag.Content = label;
            TagsLayout.Children.Add(tag);
        }
    }

    // Combina filtros manuales de búsqueda con la base ya filtrada por preferencias.
    private void OnBuscarClicked(object sender, EventArgs e)
    {
        var baseList = _filtrosPreferenciasActivos
            ? (RecetasFiltradasBase ?? new List<Receta>())
            : (TodasLasRecetas ?? new List<Receta>());

        var resultado = baseList.AsEnumerable();

        if (int.TryParse(EntryComensales.Text?.Trim(), out int comensales))
            resultado = resultado.Where(r => r.Comensales == comensales);

        if (!string.IsNullOrWhiteSpace(EntryOrigen.Text))
            resultado = resultado.Where(r => r.OrigenDelPlato != null && r.OrigenDelPlato.Contains(EntryOrigen.Text.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(EntryTiempo.Text))
            resultado = resultado.Where(r => r.TiempoPreparacion != null && r.TiempoPreparacion.Contains(EntryTiempo.Text.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(EntryTipoCocina.Text))
            resultado = resultado.Where(r => r.TipoCocina != null && r.TipoCocina.Contains(EntryTipoCocina.Text.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(EntryIngrediente.Text))
            resultado = resultado.Where(r => r.IngredientePrincipal != null && r.IngredientePrincipal.Contains(EntryIngrediente.Text.Trim(), StringComparison.OrdinalIgnoreCase));

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

        OnBuscarClicked(null, null);
    }

    // Carga recetas, creador, imágenes, likes y valoraciones antes de pintar la lista.
    private async Task CargarRecetasAsync()
    {
        if (_estaCargandoRecetas)
            return;

        _estaCargandoRecetas = true;

        try
        {
            var api = new DatabaseService();
            int usuarioId = App.UsuarioActual?.Id ?? 0;

            // Limpiamos siempre los estados del usuario anterior.
            _likesUsuario.Clear();
            _usuariosValorados.Clear();

            // Descargar recetas desde MySQL.
            var lista = await api.GetRecetasAsync();

            if (lista == null)
                return;

            // Contador real de likes por receta.
            var likesPorReceta = await api.GetLikesPorRecetaAsync(lista.Select(r => r.Id));

            // Cargar likes y valoraciones SOLO del usuario actual.
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
                    _likesUsuario = new HashSet<int>();
                    _usuariosValorados = new HashSet<int>();
                }
            }

            foreach (var r in lista)
            {
                // Datos del creador.
                var creador = await api.GetUsuarioByIdAsync(r.UsuarioId);

                r.CreadorNombre = creador?.Nombre ?? "Usuario";
                r.CreadorFoto = creador?.Foto ?? "user.png";
                r.CreadorFotoSource = await api.GetImageSourceAsync(r.CreadorFoto, "user.png");

                // Likes totales de la receta.
                r.Likes = likesPorReceta.TryGetValue(r.Id, out int totalLikes)
                    ? totalLikes
                    : 0;

                // MUY IMPORTANTE:
                // Reiniciar siempre estos valores para no heredar el estado
                // de la cuenta anterior.
                r.UsuarioHaDadoLike = false;
                r.UsuarioHaValorado = false;

                // Estado del usuario actual.
                if (usuarioId != 0)
                {
                    r.UsuarioHaDadoLike = _likesUsuario.Contains(r.Id);
                    r.UsuarioHaValorado = _usuariosValorados.Contains(r.UsuarioId);
                }

                // Imagen de la receta.
                r.ImagenSource = await api.GetImageSourceAsync(r.Imagen, "recipes.png");
            }

            TodasLasRecetas = lista.ToList();

            // Aplicar filtros por preferencias del usuario actual.
            if (App.UsuarioActual != null)
                RecetasFiltradasBase = FiltrarPorPreferencias(TodasLasRecetas, App.UsuarioActual);
            else
                RecetasFiltradasBase = TodasLasRecetas.ToList();

            // Pintar recetas.
            Recetas.Clear();

            foreach (var r in RecetasFiltradasBase)
                Recetas.Add(r);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error",
                $"No se pudieron cargar las recetas: {ex.Message}",
                "OK"
            );
        }
        finally
        {
            _estaCargandoRecetas = false;
        }
    }

    // Alterna el like de la receta y sincroniza el estado con la tabla Likes.
    private async void LikeClicked(object sender, EventArgs e)
    {
        // Toggle de like: inserta si no existe, elimina si ya existe y corrige la UI con una re-sincronización defensiva si ocurre un error.
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
                await api.PostLikeAsync(new Like
                {
                    UsuarioId = usuarioId,
                    RecetaId = recetaId
                });

                receta.UsuarioHaDadoLike = true;
                receta.Likes++;
                _likesUsuario.Add(recetaId);
            }
            catch (HttpRequestException)
            {
                // posible conflicto/duplicado: sincronizar desde servidor
                await SafeRefreshLikes(api, usuarioId);
                receta.UsuarioHaDadoLike = _likesUsuario.Contains(recetaId);
            }
            catch
            {
                // error genérico: refrescar como medida defensiva
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
                // si falla eliminando, re-sincroniza
                await SafeRefreshLikes(api, usuarioId);
                receta.UsuarioHaDadoLike = _likesUsuario.Contains(recetaId);
            }
        }

        // Refrescar icono y contador
        img.Source = receta.IconoLike;
    }

    // Abre el popup de estrellas y marca que el usuario ya ha valorado a ese creador.
    private async void PuntuarClicked(object sender, EventArgs e)
    {
        // Abre el selector de estrellas y guarda la valoración del creador.
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
            img.Source = receta.IconoEstrella; // ← opcional, la UI se refresca sola
            await DisplayAlertAsync("Gracias", $"Has valorado con {estrellas} estrellas", "OK");
        }
    }


    // helper para refrescar likes del servidor sin lanzar excepciones visibles
    private async Task SafeRefreshLikes(DatabaseService api, int usuarioId)
    {
        try
        {
            var likesFromServer = await api.GetLikesUsuarioAsync(usuarioId);
            _likesUsuario = new HashSet<int>(likesFromServer.Select(l => l.RecetaId));
        }
        catch
        {
            // ignoramos errores en refresh para no bloquear la UI
        }
    }

    private async void OnRecetaTapped(object sender, EventArgs e)
    {
        var border = (Border)sender;
        var receta = (Receta)border.BindingContext;

        var popup = new RecipeDetailPopup(receta);
        var resultado = await this.ShowPopupAsync(popup);

        if (resultado is int[] ids && ids.Length == 2)
        {
            await Shell.Current.GoToAsync($"recipesteps?recetaId={ids[0]}&usuarioId={ids[1]}");
        }
    }

    private async void InicioClicked(object sender, EventArgs e)
    {
        CargarRecetasAsync();
        await Shell.Current.GoToAsync("//home");
    }

    private async void RecetasClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//recipes");
    }

    private async void PerfilClicked(object sender, EventArgs e)
    {
        // Protege el acceso al perfil: los invitados deben iniciar sesión antes de entrar.
        if (App.UsuarioActual == null)
        {
            bool irLogin = await DisplayAlertAsync(
                "Inicia sesión",
                "Para acceder a tu perfil necesitas iniciar sesión.",
                "Iniciar sesión",
                "Cancelar"
            );

            if (irLogin)
            {
                await Shell.Current.GoToAsync("//login");
            }

            return;
        }
        await Shell.Current.GoToAsync("//profile");
    }
}