using System.Collections.ObjectModel;
using Caliburn.Micro;
using WarcraftBattle.Editor.Properties;
using WarcraftBattle.Shared.Models;

namespace WarcraftBattle.Editor.ViewModels
{
    public class MapViewModel : Screen
    {
        private const int TileSize = 48;
        private readonly EditorSessionViewModel _session;
        private int _tilesWide;
        private int _tilesHigh;

        public MapViewModel(string title, EditorSessionViewModel session)
        {
            DisplayName = title;
            _session = session;
            Stage = new StageInfo
            {
                MapWidth = 20 * TileSize,
                MapHeight = 12 * TileSize
            };
            _tilesWide = 20;
            _tilesHigh = 12;
            Tiles = new ObservableCollection<TileViewModel>();
            Placements = new ObservableCollection<PlacementViewModel>();
            BuildTiles();
        }

        public StageInfo Stage { get; }

        public ObservableCollection<TileViewModel> Tiles { get; }

        public ObservableCollection<PlacementViewModel> Placements { get; }

        public int TilesWide
        {
            get => _tilesWide;
            set
            {
                if (_tilesWide == value || value <= 0)
                {
                    return;
                }

                _tilesWide = value;
                ResizeMap();
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(CanvasWidth));
            }
        }

        public int TilesHigh
        {
            get => _tilesHigh;
            set
            {
                if (_tilesHigh == value || value <= 0)
                {
                    return;
                }

                _tilesHigh = value;
                ResizeMap();
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(CanvasHeight));
            }
        }

        public double CanvasWidth => _tilesWide * TileSize;

        public double CanvasHeight => _tilesHigh * TileSize;

        public void PaintTile(TileViewModel tile)
        {
            if (_session.SelectedTerrain == null)
            {
                return;
            }

            tile.Terrain = _session.SelectedTerrain;
        }

        public void AddObstacle(double x, double y)
        {
            if (_session.SelectedObstacle == null)
            {
                return;
            }

            Placements.Add(new PlacementViewModel(
                _session.SelectedObstacle.Name,
                StringResources.PaletteObstacle,
                _session.SelectedObstacle.Width,
                _session.SelectedObstacle.Height
            )
            {
                X = x,
                Y = y
            });
        }

        public void AddBuilding(double x, double y)
        {
            if (_session.SelectedBuilding == null)
            {
                return;
            }

            Placements.Add(new PlacementViewModel(
                _session.SelectedBuilding.Name,
                StringResources.PaletteBuilding,
                _session.SelectedBuilding.Width,
                _session.SelectedBuilding.Height
            )
            {
                X = x,
                Y = y
            });
        }

        private void ResizeMap()
        {
            Stage.MapWidth = CanvasWidth;
            Stage.MapHeight = CanvasHeight;
            BuildTiles();
        }

        private void BuildTiles()
        {
            Tiles.Clear();
            var defaultTerrain = _session.SelectedTerrain ?? _session.Terrains[0];
            for (var row = 0; row < _tilesHigh; row++)
            {
                for (var column = 0; column < _tilesWide; column++)
                {
                    Tiles.Add(new TileViewModel(row, column, defaultTerrain));
                }
            }
        }
    }
}
