using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WarcraftBattle.Editor.Properties;

namespace WarcraftBattle.Editor.Views
{
    public partial class ToolboxView : UserControl
    {
        public ToolboxView()
        {
            InitializeComponent();
            ObstacleList.PreviewMouseMove += OnObstacleMouseMove;
            BuildingList.PreviewMouseMove += OnBuildingMouseMove;
        }

        private void OnObstacleMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            if (ObstacleList.SelectedItem == null)
            {
                return;
            }

            DragDrop.DoDragDrop(ObstacleList, StringResources.PaletteObstacle, DragDropEffects.Copy);
        }

        private void OnBuildingMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            if (BuildingList.SelectedItem == null)
            {
                return;
            }

            DragDrop.DoDragDrop(BuildingList, StringResources.PaletteBuilding, DragDropEffects.Copy);
        }
    }
}
