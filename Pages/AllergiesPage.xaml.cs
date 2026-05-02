namespace MiniTFG;

/// <summary>
/// Permite marcar alérgenos durante el registro del usuario.
/// </summary>
public partial class AllergiesPage : ContentPage
{
	public List<AlergenosPreferencias> Alergenos { get; set; }

    public AllergiesPage()
	{
		InitializeComponent();

		Alergenos = new List<AlergenosPreferencias>
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

        BindingContext = this;
    }

	    // Copia las selecciones de la pantalla al usuario temporal y avanza al paso de preferencias.
    private async void NextClicked(object sender, EventArgs e)
	{
        foreach (var item in Alergenos) // Recorremos la lista de alérgenos y rellenamos las propiedades del usuario temporal según la selección
        {
            switch (item.Nombre)
            {
                case "Gluten":
                    App.UsuarioTemporal.Gluten = item.Seleccion;
                    break;

                case "Leche":
                    App.UsuarioTemporal.Lactosa = item.Seleccion;
                    break;

                case "Frutos secos":
                    App.UsuarioTemporal.FrutosSecos = item.Seleccion;
                    break;

                case "Marisco":
                    App.UsuarioTemporal.Marisco = item.Seleccion;
                    break;

                case "Huevos":
                    App.UsuarioTemporal.Huevo = item.Seleccion;
                    break;

                case "Soja":
                    App.UsuarioTemporal.Soja = item.Seleccion;
                    break;

                case "Pescado":
                    App.UsuarioTemporal.Pescado = item.Seleccion;
                    break;

                case "Cacahuetes":
                    App.UsuarioTemporal.Cacahuetes = item.Seleccion;
                    break;

                case "Sésamo":
                    App.UsuarioTemporal.Sesamo = item.Seleccion;
                    break;

                case "Sulfitos":
                    App.UsuarioTemporal.Sulfitos = item.Seleccion;
                    break;

                case "Mostaza":
                    App.UsuarioTemporal.Mostaza = item.Seleccion;
                    break;

                case "Altramuces":
                    App.UsuarioTemporal.Altramuces = item.Seleccion;
                    break;

                case "Moluscos":
                    App.UsuarioTemporal.Moluscos = item.Seleccion;
                    break;

                case "Apio":
                    App.UsuarioTemporal.Apio = item.Seleccion;
                    break;
            }
        }
        await Shell.Current.GoToAsync("preferences");
    }
}