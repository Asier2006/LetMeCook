using System.Text.Json.Serialization;

namespace MiniTFG
{

    public class PasoReceta
    {
        public int Id { get; set; }
        public int RecetaId { get; set; }
        public int NumeroPaso { get; set; }
        public string Descripcion { get; set; }
        public string Video { get; set; }
        public string VideoNombreArchivo { get; set; }
        public string VideoContentType { get; set; }

        [JsonIgnore]
        public string VideoFilePath { get; set; }
    }
}
