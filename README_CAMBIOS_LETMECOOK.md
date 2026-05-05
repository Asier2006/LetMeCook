# Cambios aplicados en esta versión

## 1. Buscador por filtros con etiquetas

En `Pages/HomePage.xaml` y `Pages/HomePage.xaml.cs` se ha añadido búsqueda por etiquetas para:

- Origen / nacionalidad
- Tipo de cocina
- Ingredientes

La búsqueda usa lógica `contains` ignorando mayúsculas y tildes. Ejemplos:

- `es` muestra `España`
- `hor` muestra `Horno`
- `man` muestra `Manzana`

Las etiquetas vienen de las nuevas tablas MySQL:

- `OrigenesPlato`
- `TiposCocina`
- `Ingredientes`

También se mantienen filtros por comensales, tiempo y preferencias/alérgenos del usuario.

## 2. Creación de receta por etiquetas

En `Pages/RecipesPage.xaml` y `Pages/RecipesPage.xaml.cs` se ha eliminado el bloque antiguo de pasos. Ahora la creación básica solo guarda la receta.

Campos obligatorios:

- Nombre
- Descripción
- Comensales

Campos opcionales:

- Imagen
- Tiempo
- Origen
- Tipo de cocina
- Ingredientes

Origen, tipo de cocina e ingredientes usan etiquetas seleccionables desde la base de datos. También se permite crear una etiqueta nueva si se escribe manualmente y no existe.

## 3. Let me Cook avanzado

Se ha añadido la página:

- `Pages/LetMeCookPage.xaml`
- `Pages/LetMeCookPage.xaml.cs`

Desde el botón `Let me Cook` se crea primero la receta básica y luego se abre la pantalla avanzada para añadir:

- pasos
- comentarios/descripciones
- vídeos por paso

Los vídeos siguen teniendo límite de 30 MB.

## 4. Sistema de puntos

Se ha añadido `Usuarios.Puntos` y la propiedad `Usuario.Puntos`.

Reglas implementadas:

- Like recibido: +10 puntos para el creador de la receta.
- Quitar like: -10 puntos al creador.
- Campo opcional rellenado en creación básica: +1 punto por bloque.
- Descripción/comentario en paso avanzado: +2 puntos.
- Vídeo añadido en paso avanzado: +3 puntos.

La tienda ahora descuenta puntos reales de `Usuarios.Puntos` en vez de calcular saldo por likes.

## SQL

Dentro de `BBDD/` hay dos scripts:

- `minitfg_schema_corregido.sql`: esquema completo actualizado.
- `minitfg_migracion_puntos_etiquetas.sql`: migración segura si ya tienes datos y no quieres borrar tablas.
