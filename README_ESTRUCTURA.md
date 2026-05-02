# MiniTFG - estructura del proyecto

Este proyecto está organizado para separar responsabilidades y que sea más fácil entenderlo o modificarlo.

## Carpetas principales

- `Models/`: clases de datos que representan tablas o elementos usados por la UI (`Usuario`, `Receta`, `PasoReceta`, `Skin`, `Like`, etc.).
- `Services/`: lógica de acceso a datos. `DatabaseService.cs` contiene las consultas MySQL y centraliza toda la comunicación con la base de datos.
- `Pages/`: pantallas completas de MAUI. Cada pantalla tiene su `.xaml` visual y su `.xaml.cs` con eventos y lógica de interfaz.
- `Popups/`: ventanas emergentes de CommunityToolkit, como detalle de receta y valoración por estrellas.
- `Helpers/`: utilidades pequeñas compartidas por varias páginas.
- `Database/`: script SQL corregido para crear/actualizar la base de datos.
- `Resources/Images/`: imágenes locales usadas para iconos, skins, banners y perfil.
- `Platforms/`: código específico de Android, iOS, MacCatalyst y Windows generado por MAUI.

## Flujo principal

1. `App.xaml.cs` arranca la aplicación y restaura sesión si hay un usuario recordado.
2. `AppShell.xaml.cs` registra las rutas de navegación.
3. `LoginPage` permite iniciar sesión, entrar como invitado o crear cuenta.
4. `NamePage`, `AllergiesPage` y `PreferencesPage` completan el registro.
5. `HomePage` carga recetas, aplica filtros, likes y valoraciones.
6. `RecipesPage` crea recetas con imagen, pasos y vídeos.
7. `RecipeStepsPage` muestra pasos y reproduce vídeos reconstruidos desde Base64.
8. `ProfilePage`, `OtherProfilePage`, `ShopPage` y `SettingsPage` gestionan perfil, tienda y ajustes.

## Nota técnica

Las clases siguen usando el namespace `MiniTFG` aunque estén organizadas en carpetas. Esto evita tener que tocar todos los `using` y mantiene compatibilidad con las rutas XAML existentes.
