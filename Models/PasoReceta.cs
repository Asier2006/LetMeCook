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

        public string Video1 { get; set; }
        public string Video1Nombre { get; set; }
        public string Video1Tipo { get; set; }
        public string Video2 { get; set; }
        public string Video2Nombre { get; set; }
        public string Video2Tipo { get; set; }
        public string Video3 { get; set; }
        public string Video3Nombre { get; set; }
        public string Video3Tipo { get; set; }
        public string Video4 { get; set; }
        public string Video4Nombre { get; set; }
        public string Video4Tipo { get; set; }
        public string Video5 { get; set; }
        public string Video5Nombre { get; set; }
        public string Video5Tipo { get; set; }


        [JsonIgnore]
        public string VideoFilePath { get; set; }
    }
}
