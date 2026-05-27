namespace MiniTFG
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Foto { get; set; }
        public string Banner { get; set; }
        public string Correo { get; set; }
        public string Contrasena { get; set; }

        // Alérgenos que el usuario quiere evitar.
        public bool Gluten { get; set; }
        public bool Lactosa { get; set; }
        public bool Huevo { get; set; }
        public bool FrutosSecos { get; set; }
        public bool Marisco { get; set; }
        public bool Soja { get; set; }
        public bool Pescado { get; set; }
        public bool Cacahuetes { get; set; }
        public bool Sesamo { get; set; }
        public bool Sulfitos { get; set; }
        public bool Mostaza { get; set; }
        public bool Altramuces { get; set; }
        public bool Moluscos { get; set; }
        public bool Apio { get; set; }

        // Preferencias dietéticas utilizadas para filtrar recetas.
        public bool Vegano { get; set; }
        public bool Vegetariano { get; set; }

        // Datos calculados al leer usuarios: no son introducidos manualmente por el usuario.
        public double ValoracionMedia { get; set; }
        public int NumeroValoraciones { get; set; }

        // Puntos gastables en tienda y obtenidos por actividad: likes, campos opcionales y vídeos.
        public int Puntos { get; set; }
        public string? Descripcion { get; set; }

    }
}
