using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace WarcraftBattle.Editor.ViewModels
{
    public class TileViewModel : INotifyPropertyChanged
    {
        private TerrainBrushViewModel _terrain;

        public TileViewModel(int row, int column, TerrainBrushViewModel terrain)
        {
            Row = row;
            Column = column;
            _terrain = terrain;
        }

        public int Row { get; }

        public int Column { get; }

        public TerrainBrushViewModel Terrain
        {
            get => _terrain;
            set
            {
                if (_terrain == value)
                {
                    return;
                }

                _terrain = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Brush));
            }
        }

        public Brush Brush => Terrain.Brush;

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
