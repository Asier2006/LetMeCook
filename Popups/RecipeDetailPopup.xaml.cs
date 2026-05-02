// Archivo comentado: RecipeDetailPopup.xaml.cs
// Popup de detalle rápido de una receta antes de abrir sus pasos.
// Organización del proyecto: los modelos están en Models, las páginas en Pages, los popups en Popups, los servicios en Services y las utilidades en Helpers.

using CommunityToolkit.Maui.Views;

namespace MiniTFG;

/// <summary>
/// Popup de resumen que devuelve los ids necesarios para abrir los pasos.
/// </summary>
public partial class RecipeDetailPopup : Popup
{
    private readonly int _recetaId;
    private readonly int _usuarioId;

    public RecipeDetailPopup(Receta receta)
    {
        InitializeComponent();

        _recetaId = receta.Id;
        _usuarioId = receta.UsuarioId;
        TituloLabel.Text = receta.Titulo;
        RecetaImage.Source = receta.ImagenSource;
        DescripcionLabel.Text = receta.Descripcion;
    }

    private void OnLetMeCookClicked(object sender, EventArgs e)
    {
        Close(new int[] { _recetaId, _usuarioId });
    }
}
