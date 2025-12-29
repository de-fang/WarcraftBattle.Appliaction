using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using WarcraftBattle.Editor.Properties;
using WarcraftBattle.Editor.ViewModels;

namespace WarcraftBattle.Editor.Views
{
    public partial class MapView : UserControl
    {
        public MapView()
        {
            InitializeComponent();
        }

        private void OnTileMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border || border.DataContext is not TileViewModel tile)
            {
                return;
            }

            if (DataContext is MapViewModel map)
            {
                map.PaintTile(tile);
            }
        }

        private void OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            if (sender is not Thumb thumb || thumb.Tag is not PlacementViewModel placement)
            {
                return;
            }

            placement.X += e.HorizontalChange;
            placement.Y += e.VerticalChange;
        }

        private void OnMapDrop(object sender, DragEventArgs e)
        {
            if (DataContext is not MapViewModel map)
            {
                return;
            }

            if (!e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                return;
            }

            var kind = e.Data.GetData(DataFormats.StringFormat) as string;
            var position = e.GetPosition((UIElement)sender);

            if (kind == StringResources.PaletteObstacle)
            {
                map.AddObstacle(position.X, position.Y);
            }
            else if (kind == StringResources.PaletteBuilding)
            {
                map.AddBuilding(position.X, position.Y);
            }
        }
    }
}
