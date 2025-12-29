using System.Collections.ObjectModel;
using System.Windows.Media;
using WarcraftBattle.Shared.Models;

namespace WarcraftBattle.Editor.ViewModels
{
    public class EditorSessionViewModel
    {
        public EditorSessionViewModel()
        {
            Terrains = new ObservableCollection<TerrainBrushViewModel>
            {
                new TerrainBrushViewModel("Grass", Brushes.ForestGreen),
                new TerrainBrushViewModel("Dirt", Brushes.SaddleBrown),
                new TerrainBrushViewModel("Water", Brushes.SteelBlue),
                new TerrainBrushViewModel("Stone", Brushes.DimGray)
            };

            Obstacles = new ObservableCollection<ObstacleDef>
            {
                new ObstacleDef { Key = "Rock", Name = "Rock", Width = 80, Height = 60 },
                new ObstacleDef { Key = "Tree", Name = "Tree", Width = 60, Height = 90 },
                new ObstacleDef { Key = "Crate", Name = "Crate", Width = 40, Height = 40 }
            };

            Buildings = new ObservableCollection<BuildingInfo>
            {
                new BuildingInfo { Id = "Barracks", Name = "Barracks", Width = 160, Height = 120 },
                new BuildingInfo { Id = "Tower", Name = "Tower", Width = 80, Height = 140 },
                new BuildingInfo { Id = "House", Name = "House", Width = 120, Height = 100 }
            };

            SelectedTerrain = Terrains[0];
            SelectedObstacle = Obstacles[0];
            SelectedBuilding = Buildings[0];
        }

        public ObservableCollection<TerrainBrushViewModel> Terrains { get; }

        public ObservableCollection<ObstacleDef> Obstacles { get; }

        public ObservableCollection<BuildingInfo> Buildings { get; }

        public TerrainBrushViewModel SelectedTerrain { get; set; }

        public ObstacleDef SelectedObstacle { get; set; }

        public BuildingInfo SelectedBuilding { get; set; }
    }
}
