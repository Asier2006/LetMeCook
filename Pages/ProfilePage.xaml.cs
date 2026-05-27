using Microsoft.Maui.Controls.Shapes;
using System.Collections.ObjectModel;

namespace MiniTFG;

public partial class ProfilePage : ContentPage
{
    public ObservableCollection<Receta> MisRecetas { get; set; } = new();

    private bool menuAbierto = false;

    public ProfilePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var api = new DatabaseService();

        UsernameLabel.Text = App.UsuarioActual.Nombre;

        MostrarEstrellas(App.UsuarioActual.ValoracionMedia);

        await CargarRecetas();

        ProfileImage.Source = await api.GetImageSourceAsync(App.UsuarioActual.Foto, "user.png");
        BannerImage.Source = await api.GetImageSourceAsync(App.UsuarioActual.Banner, "opbanner.jpg");

        ActivarBoton(BtnRecetas);
        ContentSwitcher.Content = CrearVistaRecetas();
    }

    private async Task CargarRecetas()
    {
        var api = new DatabaseService();
        var lista = await api.GetRecetasAsync();

        MisRecetas.Clear();

        foreach (var r in lista.Where(r => r.UsuarioId == App.UsuarioActual.Id))
        {
            r.ImagenSource = await api.GetImageSourceAsync(r.Imagen, "recipes.png");
            MisRecetas.Add(r);
        }
    }

    private void MostrarEstrellas(double media)
    {
        EstrellasContainer.Children.Clear();

        for (int i = 1; i <= 5; i++)
        {
            double porcentaje = media >= i ? 1 :
                                media <= i - 1 ? 0 :
                                media - (i - 1);

            EstrellasContainer.Children.Add(CrearEstrella(porcentaje));
        }
    }

    private Grid CrearEstrella(double porcentaje)
    {
        const double STAR_SIZE = 30;

        var grid = new Grid
        {
            WidthRequest = STAR_SIZE,
            HeightRequest = STAR_SIZE
        };

        var empty = new Image { Source = "starempty.png", Aspect = Aspect.Fill };
        var full = new Image { Source = "starfull.png", Aspect = Aspect.Fill };

        full.Clip = new RectangleGeometry
        {
            Rect = new Rect(0, 0, STAR_SIZE * porcentaje, STAR_SIZE)
        };

        grid.Children.Add(empty);
        grid.Children.Add(full);

        return grid;
    }

    private View CrearVistaRecetas()
    {
        var titulo = new Label
        {
            Text = "Mis recetas",
            FontSize = 22,
            FontAttributes = FontAttributes.Bold
        };

        var collection = new CollectionView
        {
            ItemsSource = MisRecetas,
            ItemTemplate = new DataTemplate(() =>
            {
                var border = new Border
                {
                    StrokeThickness = 0,
                    BackgroundColor = Color.FromArgb("#9C40F7"),
                    StrokeShape = new RoundRectangle { CornerRadius = 15 },
                    Padding = 10,
                    HeightRequest = 250
                };

                var grid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Auto },
                        new ColumnDefinition { Width = GridLength.Star }
                    }
                };

                var img = new Image
                {
                    Aspect = Aspect.AspectFill,
                    HeightRequest = 200,
                    WidthRequest = 200
                };
                img.SetBinding(Image.SourceProperty, "ImagenSource");
                Grid.SetColumn(img, 0);

                var stack = new VerticalStackLayout
                {
                    Padding = new Thickness(10, 0),
                    Spacing = 4
                };
                Grid.SetColumn(stack, 1);

                var tituloReceta = new Label
                {
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    FontSize = 18
                };
                tituloReceta.SetBinding(Label.TextProperty, "Titulo");
                stack.Children.Add(tituloReceta);

                var comensales = new Label { TextColor = Colors.White };
                comensales.SetBinding(Label.TextProperty, new Binding("Comensales", stringFormat: "Comensales: {0}"));
                stack.Children.Add(comensales);

                var tiempo = new Label { TextColor = Colors.White };
                tiempo.SetBinding(Label.TextProperty, new Binding("TiempoPreparacion", stringFormat: "Tiempo: {0} min"));
                stack.Children.Add(tiempo);

                var cocina = new Label { TextColor = Colors.White };
                cocina.SetBinding(Label.TextProperty, new Binding("TipoCocina", stringFormat: "Cocina: {0}"));
                stack.Children.Add(cocina);

                var origen = new Label { TextColor = Colors.White };
                origen.SetBinding(Label.TextProperty, new Binding("OrigenDelPlato", stringFormat: "Origen: {0}"));
                stack.Children.Add(origen);

                var ingrediente = new Label
                {
                    TextColor = Colors.White,
                    LineBreakMode = LineBreakMode.WordWrap
                };
                ingrediente.SetBinding(Label.TextProperty, new Binding("IngredientePrincipal", stringFormat: "Ingrediente principal: {0}"));
                stack.Children.Add(ingrediente);

                grid.Children.Add(img);
                grid.Children.Add(stack);
                border.Content = grid;

                return border;
            })
        };

        return new VerticalStackLayout
        {
            Spacing = 10,
            Children = { titulo, collection }
        };
    }

    private View CrearVistaSobreMi()
    {
        var titulo = new Label
        {
            Text = "Sobre mí",
            FontSize = 22,
            FontAttributes = FontAttributes.Bold
        };

        var descripcion = new Label
        {
            FontSize = 16,
            TextColor = Colors.Gray
        };
        descripcion.SetBinding(Label.TextProperty, new Binding("Descripcion", source: App.UsuarioActual));

        return new VerticalStackLayout
        {
            Spacing = 10,
            Children = { titulo, descripcion }
        };
    }

    private void ActivarBoton(Button btn)
    {
        BtnRecetas.BackgroundColor = Color.FromArgb("#E09A3F");
        BtnSobreMi.BackgroundColor = Color.FromArgb("#E09A3F");

        btn.BackgroundColor = Color.FromArgb("#C87F2F");
    }

    private void RecetasTabClicked(object sender, EventArgs e)
    {
        ActivarBoton(BtnRecetas);
        ContentSwitcher.Content = CrearVistaRecetas();
    }

    private void SobreMiTabClicked(object sender, EventArgs e)
    {
        ActivarBoton(BtnSobreMi);
        ContentSwitcher.Content = CrearVistaSobreMi();
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
        await Shell.Current.GoToAsync("//profile");
    }

    private async void AbrirTiendaClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("shop");
    }

    // MENÚ LATERAL
    private async void AbrirAjustesClicked(object sender, EventArgs e)
    {
        if (!menuAbierto)
            await AbrirMenu();
        else
            await CerrarMenu();
    }

    private async Task AbrirMenu()
    {
        menuAbierto = true;

        SideMenu.IsVisible = true;
        Overlay.IsVisible = true;

        SideMenu.TranslationX = 300;
        Overlay.Opacity = 0;

        await Task.WhenAll(
            SideMenu.TranslateTo(0, 0, 250, Easing.CubicOut),
            Overlay.FadeTo(1, 250)
        );
    }

    private async Task CerrarMenu()
    {
        menuAbierto = false;

        await Task.WhenAll(
            SideMenu.TranslateTo(300, 0, 250, Easing.CubicIn),
            Overlay.FadeTo(0, 250)
        );

        SideMenu.IsVisible = false;
        Overlay.IsVisible = false;
    }

    private async void CerrarMenuTapped(object sender, TappedEventArgs e)
    {
        if (menuAbierto)
            await CerrarMenu();
    }
}
