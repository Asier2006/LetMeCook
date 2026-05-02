// Servicio centralizado de acceso a MySQL: usuarios, recetas, pasos, likes, valoraciones y skins.

using MySqlConnector;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MiniTFG
{
    /// <summary>
    /// Encapsula todas las consultas SQL contra MySQL.
    /// Las páginas llaman a este servicio en lugar de contener SQL, por lo que la lógica de datos queda centralizada.
    /// </summary>
    public class DatabaseService
    {
        // ---------------------------------------------------------------
        //  CADENA DE CONEXIÓN
        // ---------------------------------------------------------------
        private const string Host     = "letmecook1-letmecook.i.aivencloud.com";      
        private const int    Port     = 12054;               
        private const string Database = "defaultdb";
        private const string User     = "avnadmin";
        private const string Password = "AVNS_D1x3VO6CqjHBO_4p5KP";

        private static string ConnectionString =>
            $"Server={Host};Port={Port};Database={Database};" +
            $"User ID={User};Password={Password};" +
            "SslMode=Required;AllowPublicKeyRetrieval=true;TreatTinyAsBoolean=true;";

        private MySqlConnection Connect() => new MySqlConnection(ConnectionString);


        //  ASSETS / BASE64
        //  Recetas: Base64. Perfil/skins: nombre de archivo local en Resources/Images.

        public Task<ImageSource> GetImageSourceAsync(string urlOrFile, string porDefecto = "user.png")
        {
            return Task.FromResult(ToImageSource(urlOrFile, porDefecto));
        }

        public ImageSource ToImageSource(string urlOrFile, string porDefecto = "user.png")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(urlOrFile))
                    return ImageSource.FromFile(porDefecto);

                var value = urlOrFile.Trim();

                // Soporta imágenes guardadas como data URI:
                // data:image/jpeg;base64,/9j/4AAQSkZJRgABAQ...
                if (value.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
                {
                    int commaIndex = value.IndexOf(',');
                    if (commaIndex >= 0)
                        value = value[(commaIndex + 1)..].Trim();
                }

                // Primero intentamos Base64.
                // Importante: las fotos JPG suelen empezar por "/9j/",
                // así que NO hay que tratar "/" como ruta de archivo.
                if (value.Length > 64)
                {
                    try
                    {
                        var bytes = Convert.FromBase64String(value);
                        return ImageSource.FromStream(() => new MemoryStream(bytes));
                    }
                    catch
                    {
                        // Si no era Base64, se intentará cargar como archivo local.
                    }
                }

                // Fotos de perfil, banners, iconos y fallback desde Resources/Images.
                return ImageSource.FromFile(value);
            }
            catch
            {
                return ImageSource.FromFile(porDefecto);
            }
        }


        //  USUARIOS

        public async Task<Usuario[]> GetUsuariosAsync()
        {
            var list = new List<Usuario>();
            await using var conn = Connect();
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand("SELECT * FROM Usuarios", conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(MapUsuario(reader));
            return list.ToArray();
        }

        public async Task<Usuario> GetUsuarioByIdAsync(int id)
        {
            await using var conn = Connect();
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand("SELECT * FROM Usuarios WHERE Id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            await using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapUsuario(reader) : null;
        }

        public async Task<Usuario> PostUsuarioAsync(Usuario u)
        {
            await using var conn = Connect();
            await conn.OpenAsync();
            var sql = @"INSERT INTO Usuarios
                        (Nombre, Foto, Banner, Correo, Contrasena,
                         Gluten, Lactosa, Huevo, FrutosSecos, Marisco,
                         Soja, Pescado, Cacahuetes, Sesamo, Sulfitos,
                         Mostaza, Altramuces, Moluscos, Apio, Vegano, Vegetariano)
                        VALUES
                        (@Nombre, @Foto, @Banner, @Correo, @Contrasena,
                         @Gluten, @Lactosa, @Huevo, @FrutosSecos, @Marisco,
                         @Soja, @Pescado, @Cacahuetes, @Sesamo, @Sulfitos,
                         @Mostaza, @Altramuces, @Moluscos, @Apio, @Vegano, @Vegetariano);
                        SELECT LAST_INSERT_ID();";
            await using var cmd = new MySqlCommand(sql, conn);
            AddUsuarioParams(cmd, u);
            u.Id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return u;
        }

        public async Task<bool> UpdateUsuarioAsync(Usuario u)
        {
            await using var conn = Connect();
            await conn.OpenAsync();
            var sql = @"UPDATE Usuarios SET
                        Nombre=@Nombre, Foto=@Foto, Banner=@Banner, Correo=@Correo, Contrasena=@Contrasena,
                        Gluten=@Gluten, Lactosa=@Lactosa, Huevo=@Huevo, FrutosSecos=@FrutosSecos, Marisco=@Marisco,
                        Soja=@Soja, Pescado=@Pescado, Cacahuetes=@Cacahuetes, Sesamo=@Sesamo, Sulfitos=@Sulfitos,
                        Mostaza=@Mostaza, Altramuces=@Altramuces, Moluscos=@Moluscos, Apio=@Apio,
                        Vegano=@Vegano, Vegetariano=@Vegetariano
                        WHERE Id=@Id";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", u.Id);
            AddUsuarioParams(cmd, u);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<Usuario> LoginAsync(string correo, string contrasena)
        {
            await using var conn = Connect();
            await conn.OpenAsync();
            // AVISO: almacena la contraseña hasheada en producción (bcrypt, etc.)
            await using var cmd = new MySqlCommand(
                "SELECT * FROM Usuarios WHERE Correo=@correo AND Contrasena=@contrasena LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@correo", correo?.Trim());
            cmd.Parameters.AddWithValue("@contrasena", contrasena);
            await using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapUsuario(reader) : null;
        }

        public async Task<bool> UsuarioExistePorCorreoAsync(string correo, int? excluirUsuarioId = null)
        {
            await using var conn = Connect();
            await conn.OpenAsync();

            var sql = "SELECT COUNT(*) FROM Usuarios WHERE Correo=@correo";
            if (excluirUsuarioId.HasValue)
                sql += " AND Id<>@id";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@correo", correo?.Trim());
            if (excluirUsuarioId.HasValue)
                cmd.Parameters.AddWithValue("@id", excluirUsuarioId.Value);

            return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
        }

        public async Task<bool> UpdateNombreUsuarioAsync(int usuarioId, string nuevoNombre)
        {
            await using var conn = Connect();
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand("UPDATE Usuarios SET Nombre=@nombre WHERE Id=@id", conn);
            cmd.Parameters.AddWithValue("@nombre", nuevoNombre?.Trim());
            cmd.Parameters.AddWithValue("@id", usuarioId);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UpdateCorreoUsuarioAsync(int usuarioId, string nuevoCorreo)
        {
            await using var conn = Connect();
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand("UPDATE Usuarios SET Correo=@correo WHERE Id=@id", conn);
            cmd.Parameters.AddWithValue("@correo", nuevoCorreo?.Trim());
            cmd.Parameters.AddWithValue("@id", usuarioId);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UpdatePasswordUsuarioAsync(int usuarioId, string contrasenaActual, string nuevaContrasena)
        {
            await using var conn = Connect();
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(
                "UPDATE Usuarios SET Contrasena=@nueva WHERE Id=@id AND Contrasena=@actual", conn);
            cmd.Parameters.AddWithValue("@nueva", nuevaContrasena);
            cmd.Parameters.AddWithValue("@id", usuarioId);
            cmd.Parameters.AddWithValue("@actual", contrasenaActual);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UpdatePreferenciasUsuarioAsync(Usuario u)
        {
            await using var conn = Connect();
            await conn.OpenAsync();
            var sql = @"UPDATE Usuarios SET
                        Gluten=@Gluten, Lactosa=@Lactosa, Huevo=@Huevo, FrutosSecos=@FrutosSecos, Marisco=@Marisco,
                        Soja=@Soja, Pescado=@Pescado, Cacahuetes=@Cacahuetes, Sesamo=@Sesamo, Sulfitos=@Sulfitos,
                        Mostaza=@Mostaza, Altramuces=@Altramuces, Moluscos=@Moluscos, Apio=@Apio,
                        Vegano=@Vegano, Vegetariano=@Vegetariano
                        WHERE Id=@Id";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", u.Id);
            cmd.Parameters.AddWithValue("@Gluten", u.Gluten);
            cmd.Parameters.AddWithValue("@Lactosa", u.Lactosa);
            cmd.Parameters.AddWithValue("@Huevo", u.Huevo);
            cmd.Parameters.AddWithValue("@FrutosSecos", u.FrutosSecos);
            cmd.Parameters.AddWithValue("@Marisco", u.Marisco);
            cmd.Parameters.AddWithValue("@Soja", u.Soja);
            cmd.Parameters.AddWithValue("@Pescado", u.Pescado);
            cmd.Parameters.AddWithValue("@Cacahuetes", u.Cacahuetes);
            cmd.Parameters.AddWithValue("@Sesamo", u.Sesamo);
            cmd.Parameters.AddWithValue("@Sulfitos", u.Sulfitos);
            cmd.Parameters.AddWithValue("@Mostaza", u.Mostaza);
            cmd.Parameters.AddWithValue("@Altramuces", u.Altramuces);
            cmd.Parameters.AddWithValue("@Moluscos", u.Moluscos);
            cmd.Parameters.AddWithValue("@Apio", u.Apio);
            cmd.Parameters.AddWithValue("@Vegano", u.Vegano);
            cmd.Parameters.AddWithValue("@Vegetariano", u.Vegetariano);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }




        //  RECETAS


        public async Task<Receta[]> GetRecetasAsync()
        {
            var list = new List<Receta>();
            await using var conn = Connect();
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand("SELECT * FROM Recetas", conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(MapReceta(reader));
            return list.ToArray();
        }

        public async Task<Receta> GetRecetaByIdAsync(int id)
        {
            await using var conn = Connect();
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand("SELECT * FROM Recetas WHERE Id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            await using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapReceta(reader) : null;
        }

        public async Task<Receta> PostRecetaAsync(Receta r)
        {
            await using var conn = Connect();
            await conn.OpenAsync();
            var sql = @"INSERT INTO Recetas
                        (UsuarioId, Titulo, Descripcion, Imagen, Comensales,
                         OrigenDelPlato, TiempoPreparacion, TipoCocina, IngredientePrincipal,
                         Gluten, Lactosa, Huevo, FrutosSecos, Mariscos, Soja, Pescado,
                         Cacahuetes, Sesamo, Sulfitos, Mostaza, Altramuces, Moluscos,
                         Apio, Vegano, Vegetariano)
                        VALUES
                        (@UsuarioId, @Titulo, @Descripcion, @Imagen, @Comensales,
                         @OrigenDelPlato, @TiempoPreparacion, @TipoCocina, @IngredientePrincipal,
                         @Gluten, @Lactosa, @Huevo, @FrutosSecos, @Mariscos, @Soja, @Pescado,
                         @Cacahuetes, @Sesamo, @Sulfitos, @Mostaza, @Altramuces, @Moluscos,
                         @Apio, @Vegano, @Vegetariano);
                        SELECT LAST_INSERT_ID();";
            await using var cmd = new MySqlCommand(sql, conn);
            AddRecetaParams(cmd, r);
            r.Id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return r;
        }

        public async Task<bool> DeleteRecetaAsync(int id)
        {
            await using var conn = Connect();
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand("DELETE FROM Recetas WHERE Id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }



        //  PASOS RECETA


        public async Task<List<PasoReceta>> GetPasosRecetaAsync(int recetaId)
        {
            var list = new List<PasoReceta>();
            await using var conn = Connect();
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(
                "SELECT * FROM PasosReceta WHERE RecetaId=@id ORDER BY NumeroPaso", conn);
            cmd.Parameters.AddWithValue("@id", recetaId);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(new PasoReceta
                {
                    Id          = reader.GetInt32("Id"),
                    RecetaId    = reader.GetInt32("RecetaId"),
                    NumeroPaso  = reader.GetInt32("NumeroPaso"),
                    Descripcion = reader.IsDBNull(reader.GetOrdinal("Descripcion")) ? null : reader.GetString("Descripcion"),
                    Video       = reader.IsDBNull(reader.GetOrdinal("Video"))       ? null : reader.GetString("Video")
                });
            return list;
        }

        public async Task<PasoReceta> PostPasoRecetaAsync(PasoReceta paso)
        {
            try
            {
                await using var conn = Connect();
                await conn.OpenAsync();
                var sql = @"INSERT INTO PasosReceta (RecetaId, NumeroPaso, Descripcion, Video)
                        VALUES (@RecetaId, @NumeroPaso, @Descripcion, @Video);
                        SELECT LAST_INSERT_ID();";
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@RecetaId", paso.RecetaId);
                cmd.Parameters.AddWithValue("@NumeroPaso", paso.NumeroPaso);
                cmd.Parameters.AddWithValue("@Descripcion", paso.Descripcion ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Video", paso.Video ?? (object)DBNull.Value);
                paso.Id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                return paso;
            }
            catch
            {
                return null;
            }
        }

        // ---------------------------------------------------------------
        //  LIKES
        // ---------------------------------------------------------------

        public async Task<Like> PostLikeAsync(Like like)
        {
            await using var conn = Connect();
            await conn.OpenAsync();
            var sql = @"INSERT IGNORE INTO Likes (UsuarioId, RecetaId)
                        VALUES (@UsuarioId, @RecetaId);
                        SELECT LAST_INSERT_ID();";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UsuarioId", like.UsuarioId);
            cmd.Parameters.AddWithValue("@RecetaId",  like.RecetaId);
            like.Id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return like;
        }

        public async Task<IEnumerable<Like>> GetLikesUsuarioAsync(int usuarioId)
        {
            var list = new List<Like>();
            await using var conn = Connect();
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(
                "SELECT * FROM Likes WHERE UsuarioId=@id", conn);
            cmd.Parameters.AddWithValue("@id", usuarioId);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(new Like
                {
                    Id        = reader.GetInt32("Id"),
                    UsuarioId = reader.GetInt32("UsuarioId"),
                    RecetaId  = reader.GetInt32("RecetaId")
                });
            return list;
        }

        public async Task<bool> DeleteLikeAsync(int usuarioId, int recetaId)
        {
            await using var conn = Connect();
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(
                "DELETE FROM Likes WHERE UsuarioId=@uid AND RecetaId=@rid", conn);
            cmd.Parameters.AddWithValue("@uid", usuarioId);
            cmd.Parameters.AddWithValue("@rid", recetaId);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<Dictionary<int, int>> GetLikesPorRecetaAsync(IEnumerable<int> recetaIds)
        {
            var ids = recetaIds?.Distinct().ToList() ?? new List<int>();
            var result = ids.ToDictionary(id => id, _ => 0);

            if (ids.Count == 0)
                return result;

            await using var conn = Connect();
            await conn.OpenAsync();

            var parametros = ids.Select((_, i) => $"@id{i}").ToArray();
            var sql = $"SELECT RecetaId, COUNT(*) Total FROM Likes WHERE RecetaId IN ({string.Join(",", parametros)}) GROUP BY RecetaId";
            await using var cmd = new MySqlCommand(sql, conn);

            for (int i = 0; i < ids.Count; i++)
                cmd.Parameters.AddWithValue(parametros[i], ids[i]);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result[reader.GetInt32("RecetaId")] = reader.GetInt32("Total");

            return result;
        }

        public async Task<IEnumerable<Like>> GetLikesRecibidosUsuarioAsync(int usuarioId)
        {
            var list = new List<Like>();
            await using var conn = Connect();
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(
                @"SELECT l.* FROM Likes l
                  INNER JOIN Recetas r ON r.Id = l.RecetaId
                  WHERE r.UsuarioId=@id", conn);
            cmd.Parameters.AddWithValue("@id", usuarioId);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(new Like
                {
                    Id        = reader.GetInt32("Id"),
                    UsuarioId = reader.GetInt32("UsuarioId"),
                    RecetaId  = reader.GetInt32("RecetaId")
                });
            return list;
        }

        public async Task<int> GetLikesRecibidosUsuarioCountAsync(int usuarioId)
        {
            await using var conn = Connect();
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(
                @"SELECT COUNT(*) FROM Likes l
                  INNER JOIN Recetas r ON r.Id = l.RecetaId
                  WHERE r.UsuarioId=@id", conn);
            cmd.Parameters.AddWithValue("@id", usuarioId);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }



        //  VALORACIONES


        public async Task<bool> PostValoracionAsync(Valoracion v)
        {
            await using var conn = Connect();
            await conn.OpenAsync();
            var sql = @"INSERT INTO Valoraciones (UsuarioValoradoId, UsuarioQueValoraId, Puntuacion)
                        VALUES (@UsuarioValoradoId, @UsuarioQueValoraId, @Puntuacion)
                        ON DUPLICATE KEY UPDATE Puntuacion=@Puntuacion";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UsuarioValoradoId",  v.UsuarioValoradoId);
            cmd.Parameters.AddWithValue("@UsuarioQueValoraId", v.UsuarioQueValoraId);
            cmd.Parameters.AddWithValue("@Puntuacion",         v.Puntuacion);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<List<Valoracion>> GetValoracionesAsync(int usuarioValoradoId)
        {
            var list = new List<Valoracion>();
            await using var conn = Connect();
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(
                "SELECT * FROM Valoraciones WHERE UsuarioValoradoId=@id", conn);
            cmd.Parameters.AddWithValue("@id", usuarioValoradoId);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(MapValoracion(reader));
            return list;
        }

        public async Task<List<Valoracion>> GetValoracionesPorUsuarioAsync(int usuarioQueValoraId)
        {
            var list = new List<Valoracion>();
            try
            {
                await using var conn = Connect();
                await conn.OpenAsync();
                await using var cmd = new MySqlCommand(
                    "SELECT * FROM Valoraciones WHERE UsuarioQueValoraId=@id", conn);
                cmd.Parameters.AddWithValue("@id", usuarioQueValoraId);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    list.Add(MapValoracion(reader));
            }
            catch { return null; }
            return list;
        }




        //  SKINS


        public async Task EnsureDefaultSkinsAsync()
        {
            var defaults = new[]
            {
                new Skin { Nombre = "Foto Arguiñano", Imagen = "arguinano.jpg", Precio = 5, Activo = true },
                new Skin { Nombre = "Foto Chicote", Imagen = "chicote.jpg", Precio = 5, Activo = true },
                new Skin { Nombre = "Foto Heisenberg", Imagen = "heisenberg.jpg", Precio = 8, Activo = true },
                new Skin { Nombre = "Foto House", Imagen = "house.jpg", Precio = 8, Activo = true },
                new Skin { Nombre = "Foto Sanji", Imagen = "sanji.jpg", Precio = 10, Activo = true },
                new Skin { Nombre = "Banner Breaking Bad", Imagen = "brbabanner.jpg", Precio = 10, Activo = true },
                new Skin { Nombre = "Banner Better Call Saul", Imagen = "bcsbanner.jpg", Precio = 10, Activo = true },
                new Skin { Nombre = "Banner Dragon Ball", Imagen = "dbbanner.jpg", Precio = 10, Activo = true },
                new Skin { Nombre = "Banner One Piece", Imagen = "opbanner.jpg", Precio = 10, Activo = true },
                new Skin { Nombre = "Banner Peaky Blinders", Imagen = "peakybanner.jpg", Precio = 10, Activo = true }
            };

            await using var conn = Connect();
            await conn.OpenAsync();

            foreach (var skin in defaults)
            {
                await using var cmd = new MySqlCommand(
                    @"INSERT INTO Skins (Nombre, Imagen, Precio, Activo)
                      SELECT @Nombre, @Imagen, @Precio, @Activo
                      WHERE NOT EXISTS (SELECT 1 FROM Skins WHERE Nombre=@Nombre)", conn);
                cmd.Parameters.AddWithValue("@Nombre", skin.Nombre);
                cmd.Parameters.AddWithValue("@Imagen", skin.Imagen);
                cmd.Parameters.AddWithValue("@Precio", skin.Precio);
                cmd.Parameters.AddWithValue("@Activo", skin.Activo);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<List<Skin>> GetSkinsAsync()
        {
            var list = new List<Skin>();
            await using var conn = Connect();
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand("SELECT * FROM Skins WHERE Activo=1 ORDER BY Precio, Nombre", conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(new Skin
                {
                    Id     = reader.GetInt32("Id"),
                    Nombre = reader.GetString("Nombre"),
                    Imagen = reader.IsDBNull(reader.GetOrdinal("Imagen")) ? "user.png" : reader.GetString("Imagen"),
                    Precio = reader.GetInt32("Precio"),
                    Activo = reader.GetBoolean("Activo")
                });
            return list;
        }

        public async Task<List<int>> GetPurchasedUserSkinsAsync(int usuarioId)
        {
            var list = new List<int>();
            await using var conn = Connect();
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(
                "SELECT SkinId FROM UserSkins WHERE UsuarioId=@id", conn);
            cmd.Parameters.AddWithValue("@id", usuarioId);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(reader.GetInt32("SkinId"));
            return list;
        }

        public async Task<bool> PurchaseSkinAsync(int usuarioId, int skinId)
        {
            await using var conn = Connect();
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(
                "INSERT IGNORE INTO UserSkins (UsuarioId, SkinId) VALUES (@uid, @sid)", conn);
            cmd.Parameters.AddWithValue("@uid", usuarioId);
            cmd.Parameters.AddWithValue("@sid", skinId);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> ActivateSkinAsync(int usuarioId, int skinId, string tipo)
        {
            // "tipo" indica si es foto de perfil, banner, etc.
            // Adapta la columna a actualizar según tu esquema.
            string columna = tipo.ToLower() switch
            {
                "foto"   => "Foto",
                "banner" => "Banner",
                _        => "Foto"
            };
            await using var conn = Connect();
            await conn.OpenAsync();
            var sql = $"UPDATE Usuarios SET {columna}=(SELECT Imagen FROM Skins WHERE Id=@sid) WHERE Id=@uid";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@sid", skinId);
            cmd.Parameters.AddWithValue("@uid", usuarioId);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }



        //  HELPERS — mappers y parámetros

        private static Usuario MapUsuario(MySqlDataReader r) => new Usuario
        {
            Id                 = r.GetInt32("Id"),
            Nombre             = r.IsDBNull(r.GetOrdinal("Nombre"))    ? null : r.GetString("Nombre"),
            Foto               = r.IsDBNull(r.GetOrdinal("Foto"))      ? "user.png" : r.GetString("Foto"),
            Banner             = r.IsDBNull(r.GetOrdinal("Banner"))    ? "opbanner.jpg" : r.GetString("Banner"),
            Correo             = r.IsDBNull(r.GetOrdinal("Correo"))    ? null : r.GetString("Correo"),
            Contrasena         = r.IsDBNull(r.GetOrdinal("Contrasena"))? null : r.GetString("Contrasena"),
            Gluten             = r.GetBoolean("Gluten"),
            Lactosa            = r.GetBoolean("Lactosa"),
            Huevo              = r.GetBoolean("Huevo"),
            FrutosSecos        = r.GetBoolean("FrutosSecos"),
            Marisco            = r.GetBoolean("Marisco"),
            Soja               = r.GetBoolean("Soja"),
            Pescado            = r.GetBoolean("Pescado"),
            Cacahuetes         = r.GetBoolean("Cacahuetes"),
            Sesamo             = r.GetBoolean("Sesamo"),
            Sulfitos           = r.GetBoolean("Sulfitos"),
            Mostaza            = r.GetBoolean("Mostaza"),
            Altramuces         = r.GetBoolean("Altramuces"),
            Moluscos           = r.GetBoolean("Moluscos"),
            Apio               = r.GetBoolean("Apio"),
            Vegano             = r.GetBoolean("Vegano"),
            Vegetariano        = r.GetBoolean("Vegetariano"),
            ValoracionMedia    = r.IsDBNull(r.GetOrdinal("ValoracionMedia"))    ? 0 : r.GetDouble("ValoracionMedia"),
            NumeroValoraciones = r.IsDBNull(r.GetOrdinal("NumeroValoraciones")) ? 0 : r.GetInt32("NumeroValoraciones")
        };

        private static Receta MapReceta(MySqlDataReader r) => new Receta
        {
            Id                  = r.GetInt32("Id"),
            UsuarioId           = r.GetInt32("UsuarioId"),
            Titulo              = r.IsDBNull(r.GetOrdinal("Titulo"))              ? null : r.GetString("Titulo"),
            Descripcion         = r.IsDBNull(r.GetOrdinal("Descripcion"))         ? null : r.GetString("Descripcion"),
            Imagen              = r.IsDBNull(r.GetOrdinal("Imagen"))              ? null : r.GetString("Imagen"),
            Comensales          = r.GetInt32("Comensales"),
            OrigenDelPlato      = r.IsDBNull(r.GetOrdinal("OrigenDelPlato"))      ? null : r.GetString("OrigenDelPlato"),
            TiempoPreparacion   = r.IsDBNull(r.GetOrdinal("TiempoPreparacion"))   ? null : r.GetString("TiempoPreparacion"),
            TipoCocina          = r.IsDBNull(r.GetOrdinal("TipoCocina"))          ? null : r.GetString("TipoCocina"),
            IngredientePrincipal= r.IsDBNull(r.GetOrdinal("IngredientePrincipal"))? null : r.GetString("IngredientePrincipal"),
            Gluten              = r.GetBoolean("Gluten"),
            Lactosa             = r.GetBoolean("Lactosa"),
            Huevo               = r.GetBoolean("Huevo"),
            FrutosSecos         = r.GetBoolean("FrutosSecos"),
            Mariscos            = r.GetBoolean("Mariscos"),
            Soja                = r.GetBoolean("Soja"),
            Pescado             = r.GetBoolean("Pescado"),
            Cacahuetes          = r.GetBoolean("Cacahuetes"),
            Sesamo              = r.GetBoolean("Sesamo"),
            Sulfitos            = r.GetBoolean("Sulfitos"),
            Mostaza             = r.GetBoolean("Mostaza"),
            Altramuces          = r.GetBoolean("Altramuces"),
            Moluscos            = r.GetBoolean("Moluscos"),
            Apio                = r.GetBoolean("Apio"),
            Vegano              = r.GetBoolean("Vegano"),
            Vegetariano         = r.GetBoolean("Vegetariano")
        };

        private static Valoracion MapValoracion(MySqlDataReader r) => new Valoracion
        {
            UsuarioValoradoId  = r.GetInt32("UsuarioValoradoId"),
            UsuarioQueValoraId = r.GetInt32("UsuarioQueValoraId"),
            Puntuacion         = r.GetInt32("Puntuacion")
        };

        private static void AddUsuarioParams(MySqlCommand cmd, Usuario u)
        {
            cmd.Parameters.AddWithValue("@Nombre",      u.Nombre      ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Foto",        string.IsNullOrWhiteSpace(u.Foto) ? "user.png" : u.Foto);
            cmd.Parameters.AddWithValue("@Banner",      string.IsNullOrWhiteSpace(u.Banner) ? "opbanner.jpg" : u.Banner);
            cmd.Parameters.AddWithValue("@Correo",      u.Correo      ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Contrasena",  u.Contrasena  ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Gluten",      u.Gluten);
            cmd.Parameters.AddWithValue("@Lactosa",     u.Lactosa);
            cmd.Parameters.AddWithValue("@Huevo",       u.Huevo);
            cmd.Parameters.AddWithValue("@FrutosSecos", u.FrutosSecos);
            cmd.Parameters.AddWithValue("@Marisco",     u.Marisco);
            cmd.Parameters.AddWithValue("@Soja",        u.Soja);
            cmd.Parameters.AddWithValue("@Pescado",     u.Pescado);
            cmd.Parameters.AddWithValue("@Cacahuetes",  u.Cacahuetes);
            cmd.Parameters.AddWithValue("@Sesamo",      u.Sesamo);
            cmd.Parameters.AddWithValue("@Sulfitos",    u.Sulfitos);
            cmd.Parameters.AddWithValue("@Mostaza",     u.Mostaza);
            cmd.Parameters.AddWithValue("@Altramuces",  u.Altramuces);
            cmd.Parameters.AddWithValue("@Moluscos",    u.Moluscos);
            cmd.Parameters.AddWithValue("@Apio",        u.Apio);
            cmd.Parameters.AddWithValue("@Vegano",      u.Vegano);
            cmd.Parameters.AddWithValue("@Vegetariano", u.Vegetariano);
        }

        private static void AddRecetaParams(MySqlCommand cmd, Receta r)
        {
            cmd.Parameters.AddWithValue("@UsuarioId",            r.UsuarioId);
            cmd.Parameters.AddWithValue("@Titulo",               r.Titulo               ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Descripcion",          r.Descripcion          ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Imagen",               r.Imagen               ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Comensales",           r.Comensales);
            cmd.Parameters.AddWithValue("@OrigenDelPlato",       r.OrigenDelPlato        ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@TiempoPreparacion",    r.TiempoPreparacion    ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@TipoCocina",           r.TipoCocina           ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@IngredientePrincipal", r.IngredientePrincipal ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Gluten",      r.Gluten);
            cmd.Parameters.AddWithValue("@Lactosa",     r.Lactosa);
            cmd.Parameters.AddWithValue("@Huevo",       r.Huevo);
            cmd.Parameters.AddWithValue("@FrutosSecos", r.FrutosSecos);
            cmd.Parameters.AddWithValue("@Mariscos",    r.Mariscos);
            cmd.Parameters.AddWithValue("@Soja",        r.Soja);
            cmd.Parameters.AddWithValue("@Pescado",     r.Pescado);
            cmd.Parameters.AddWithValue("@Cacahuetes",  r.Cacahuetes);
            cmd.Parameters.AddWithValue("@Sesamo",      r.Sesamo);
            cmd.Parameters.AddWithValue("@Sulfitos",    r.Sulfitos);
            cmd.Parameters.AddWithValue("@Mostaza",     r.Mostaza);
            cmd.Parameters.AddWithValue("@Altramuces",  r.Altramuces);
            cmd.Parameters.AddWithValue("@Moluscos",    r.Moluscos);
            cmd.Parameters.AddWithValue("@Apio",        r.Apio);
            cmd.Parameters.AddWithValue("@Vegano",      r.Vegano);
            cmd.Parameters.AddWithValue("@Vegetariano", r.Vegetariano);
        }
    }
}
