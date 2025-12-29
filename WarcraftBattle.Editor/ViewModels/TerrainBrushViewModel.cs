using System.Windows.Media;

namespace WarcraftBattle.Editor.ViewModels
{
    public class TerrainBrushViewModel
    {
        public TerrainBrushViewModel(string name, Brush brush)
        {
            Name = name;
            Brush = brush;
        }

        public string Name { get; }

        public Brush Brush { get; }
    }
}
