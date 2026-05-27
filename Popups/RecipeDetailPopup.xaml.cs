using CommunityToolkit.Maui.Views;

namespace MiniTFG;

public partial class RecipeDetailPopup : Popup
{
    private readonly int _recetaId;
    private readonly int _usuarioId;

    public RecipeDetailPopup(Receta receta)
    {
        InitializeComponent();

        _recetaId = receta.Id;
        _usuarioId = receta.UsuarioId;

        // Título e imagen
        TituloLabel.Text = receta.Titulo;
        RecetaImage.Source = receta.ImagenSource;

        // Descripción
        DescripcionLabel.Text = receta.Descripcion;

        // Origen
        OrigenLabel.Text = string.IsNullOrWhiteSpace(receta.OrigenDelPlato)
            ? "No especificado"
            : receta.OrigenDelPlato;

        // Tipo de cocina
        TipoCocinaLabel.Text = string.IsNullOrWhiteSpace(receta.TipoCocina)
            ? "No especificado"
            : receta.TipoCocina;

        // Ingredientes
        IngredientesLabel.Text = string.IsNullOrWhiteSpace(receta.IngredientePrincipal)
            ? "No especificado"
            : receta.IngredientePrincipal;

        // Alergenos
        AlergenosLabel.Text = ObtenerAlergenos(receta);

        // Preferencias
        PreferenciasLabel.Text = ObtenerPreferencias(receta);
    }

    private string ObtenerAlergenos(Receta r)
    {
        var lista = new List<string>();

        if (r.Gluten) lista.Add("Gluten");
        if (r.Lactosa) lista.Add("Lactosa");
        if (r.Huevo) lista.Add("Huevos");
        if (r.FrutosSecos) lista.Add("Frutos secos");
        if (r.Mariscos) lista.Add("Marisco");
        if (r.Soja) lista.Add("Soja");
        if (r.Pescado) lista.Add("Pescado");
        if (r.Cacahuetes) lista.Add("Cacahuetes");
        if (r.Sesamo) lista.Add("Sésamo");
        if (r.Sulfitos) lista.Add("Sulfitos");
        if (r.Mostaza) lista.Add("Mostaza");
        if (r.Altramuces) lista.Add("Altramuces");
        if (r.Moluscos) lista.Add("Moluscos");
        if (r.Apio) lista.Add("Apio");

        return lista.Count == 0 ? "Ninguno" : string.Join(", ", lista);
    }

    private string ObtenerPreferencias(Receta r)
    {
        var lista = new List<string>();

        if (r.Vegano) lista.Add("Vegano");
        if (r.Vegetariano) lista.Add("Vegetariano");

        return lista.Count == 0 ? "Ninguna" : string.Join(", ", lista);
    }

    private void OnLetMeCookClicked(object sender, EventArgs e)
    {
        Close(new int[] { _recetaId, _usuarioId });
    }
}
