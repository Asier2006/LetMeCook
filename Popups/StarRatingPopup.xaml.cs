using CommunityToolkit.Maui.Views;

namespace MiniTFG;

public partial class StarRatingPopup : Popup
{
    private readonly int _usuarioValoradoId;
    private readonly bool _esAutoValoracion;
    private int _puntuacionSeleccionada;

    public StarRatingPopup(int usuarioValoradoId)
    {
        InitializeComponent();

        _usuarioValoradoId = usuarioValoradoId;

        // Si el usuario que está logueado es el mismo que el dueño de la receta,
        // entonces está intentando valorarse a sí mismo.
        _esAutoValoracion = App.UsuarioActual != null &&
                             App.UsuarioActual.Id == _usuarioValoradoId;

        ActualizarEstrellas(0);

        if (_esAutoValoracion)
        {
            MensajeLabel.Text = "No puedes valorar tu propia receta.";
            GuardarButton.IsEnabled = false;
            GuardarButton.Opacity = 0.45;
        }
    }

    private async void StarClicked(object sender, EventArgs e)
    {
        if (_esAutoValoracion)
        {
            _puntuacionSeleccionada = 0;
            ActualizarEstrellas(0);

            MensajeLabel.Text = "No puedes valorar tu propia receta.";

            await Application.Current.MainPage.DisplayAlert(
                "No permitido",
                "No puedes valorar tu propia receta.",
                "OK"
            );

            return;
        }

        if (sender is not ImageButton boton)
            return;

        if (!int.TryParse(boton.CommandParameter?.ToString(), out int puntuacion))
            return;

        _puntuacionSeleccionada = puntuacion;
        ActualizarEstrellas(puntuacion);

        MensajeLabel.Text = $"Has seleccionado {puntuacion} estrella{(puntuacion == 1 ? "" : "s")}.";
    }

    private async void GuardarClicked(object sender, EventArgs e)
    {
        if (App.UsuarioActual == null)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Debe iniciar sesión",
                "Inicie sesión para valorar.",
                "OK"
            );

            Close(null);
            return;
        }

        int usuarioQueValoraId = App.UsuarioActual.Id;

        // Segunda comprobación de seguridad antes de guardar en base de datos.
        if (usuarioQueValoraId == _usuarioValoradoId)
        {
            _puntuacionSeleccionada = 0;
            ActualizarEstrellas(0);

            MensajeLabel.Text = "No puedes valorar tu propia receta.";

            await Application.Current.MainPage.DisplayAlert(
                "No permitido",
                "No puedes valorar tu propia receta.",
                "OK"
            );

            return;
        }

        if (_puntuacionSeleccionada < 1 || _puntuacionSeleccionada > 5)
        {
            MensajeLabel.Text = "Selecciona primero una puntuación.";
            return;
        }

        try
        {
            var api = new DatabaseService();

            bool guardado = await api.PostValoracionAsync(new Valoracion
            {
                UsuarioValoradoId = _usuarioValoradoId,
                UsuarioQueValoraId = usuarioQueValoraId,
                Puntuacion = _puntuacionSeleccionada
            });

            if (!guardado)
            {
                MensajeLabel.Text = "No se pudo guardar la valoración.";
                return;
            }

            Close(_puntuacionSeleccionada);
        }
        catch (Exception ex)
        {
            MensajeLabel.Text = "Error al guardar la valoración.";

            await Application.Current.MainPage.DisplayAlert(
                "Error",
                $"No se pudo guardar la valoración: {ex.Message}",
                "OK"
            );
        }
    }

    private void CancelarClicked(object sender, EventArgs e)
    {
        Close(null);
    }

    private void ActualizarEstrellas(int puntuacion)
    {
        Star1.Source = puntuacion >= 1 ? "starfull.png" : "star.png";
        Star2.Source = puntuacion >= 2 ? "starfull.png" : "star.png";
        Star3.Source = puntuacion >= 3 ? "starfull.png" : "star.png";
        Star4.Source = puntuacion >= 4 ? "starfull.png" : "star.png";
        Star5.Source = puntuacion >= 5 ? "starfull.png" : "star.png";
    }
}