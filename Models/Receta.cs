using System.ComponentModel;
using System.Text.Json.Serialization;

namespace MiniTFG
{
    public class Receta : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // Campos persistidos en la tabla Recetas.
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string Titulo { get; set; }
        public string Descripcion { get; set; }
        public string Imagen { get; set; }
        public int Comensales { get; set; }
        public string OrigenDelPlato { get; set; }
        public string TiempoPreparacion { get; set; }
        public string TipoCocina { get; set; }
        public string IngredientePrincipal { get; set; }

        // ===============================
        // ETIQUETAS PRINCIPALES PARA HOMEPAGE
        // ===============================
        [JsonIgnore]
        public string OrigenPrincipal =>
            string.IsNullOrWhiteSpace(OrigenDelPlato)
                ? null
                : OrigenDelPlato.Split(',')[0].Trim();

        [JsonIgnore]
        public string TipoCocinaPrincipal =>
            string.IsNullOrWhiteSpace(TipoCocina)
                ? null
                : TipoCocina.Split(',')[0].Trim();

        [JsonIgnore]
        public string IngredientePrincipalSimple =>
            string.IsNullOrWhiteSpace(IngredientePrincipal)
                ? null
                : IngredientePrincipal.Split(',')[0].Trim();


        // Alérgenos y preferencias guardados como booleanos en MySQL.
        public bool Gluten { get; set; }
        public bool Lactosa { get; set; }
        public bool Huevo { get; set; }
        public bool FrutosSecos { get; set; }
        public bool Mariscos { get; set; }
        public bool Soja { get; set; }
        public bool Pescado { get; set; }
        public bool Cacahuetes { get; set; }
        public bool Sesamo { get; set; }
        public bool Sulfitos { get; set; }
        public bool Mostaza { get; set; }
        public bool Altramuces { get; set; }
        public bool Moluscos { get; set; }
        public bool Apio { get; set; }
        public bool Vegano { get; set; }
        public bool Vegetariano { get; set; }

        // Propiedades solo de UI. No deben serializarse ni guardarse como columnas de receta.
        private ImageSource _imagenSource;

        [JsonIgnore]
        public ImageSource ImagenSource
        {
            get => _imagenSource ?? "recipes.png";
            set
            {
                _imagenSource = value;
                OnPropertyChanged(nameof(ImagenSource));
            }
        }

        [JsonIgnore]
        public string IconoLike => UsuarioHaDadoLike ? "heartfull.png" : "heart.png";

        private int _likes;
        [JsonIgnore]
        public int Likes
        {
            get => _likes;
            set
            {
                _likes = value;
                OnPropertyChanged(nameof(Likes));
            }
        }

        private bool _usuarioHaDadoLike;
        [JsonIgnore]
        public bool UsuarioHaDadoLike
        {
            get => _usuarioHaDadoLike;
            set
            {
                _usuarioHaDadoLike = value;
                OnPropertyChanged(nameof(UsuarioHaDadoLike));
                OnPropertyChanged(nameof(IconoLike));
            }
        }

        private bool _usuarioHaValorado;
        [JsonIgnore]
        public bool UsuarioHaValorado
        {
            get => _usuarioHaValorado;
            set
            {
                _usuarioHaValorado = value;
                OnPropertyChanged(nameof(UsuarioHaValorado));
                OnPropertyChanged(nameof(IconoEstrella));
            }
        }

        [JsonIgnore]
        public string IconoEstrella => UsuarioHaValorado ? "starfull.png" : "star.png";

        private string _creadorNombre;
        [JsonIgnore]
        public string CreadorNombre
        {
            get => _creadorNombre;
            set
            {
                if (_creadorNombre != value)
                {
                    _creadorNombre = value;
                    OnPropertyChanged(nameof(CreadorNombre));
                }
            }
        }

        private string _creadorFoto;
        [JsonIgnore]
        public string CreadorFoto
        {
            get => _creadorFoto;
            set
            {
                if (_creadorFoto != value)
                {
                    _creadorFoto = value;
                    OnPropertyChanged(nameof(CreadorFoto));
                }
            }
        }

        private ImageSource _creadorFotoSource;
        [JsonIgnore]
        public ImageSource CreadorFotoSource
        {
            get => _creadorFotoSource ?? "user.png";
            set
            {
                _creadorFotoSource = value;
                OnPropertyChanged(nameof(CreadorFotoSource));
            }
        }
    }
}
