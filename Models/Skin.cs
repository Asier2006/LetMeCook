using System.ComponentModel;

namespace MiniTFG
{

    public class Skin : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Imagen { get; set; }
        public int Precio { get; set; }
        public bool Activo { get; set; }

        private bool _comprado;
        public bool Comprado
        {
            get => _comprado;
            set
            {
                if (_comprado != value)
                {
                    _comprado = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
