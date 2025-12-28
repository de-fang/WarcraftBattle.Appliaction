using System;
using System.IO;
using Caliburn.Micro;
using Microsoft.Win32;
using WarcraftBattle.Shared.Models;
using WarcraftBattle.Shared.Models.Config;

namespace WarcraftBattle.Editor.ViewModels
{
    public class ShellViewModel : Screen
    {
        private readonly EditorConfigService _configService = new EditorConfigService();
        private string _configPath;
        private StageEntryViewModel _selectedStage;
        private object _selectedItem;
        private ObstacleDef _selectedObstacleDef;
        private BuildingInfo _selectedBuildingInfo;
        private string _statusMessage = "Ready";

        public ShellViewModel()
        {
            DisplayName = "WarcraftBattle Editor";
            LoadDefaultConfig();
        }

        public BindableCollection<StageEntryViewModel> Stages { get; } = new BindableCollection<StageEntryViewModel>();

        public BindableCollection<ObstacleDef> ObstaclePalette { get; } = new BindableCollection<ObstacleDef>();

        public BindableCollection<BuildingInfo> BuildingPalette { get; } = new BindableCollection<BuildingInfo>();

        public BindableCollection<ObstaclePlacementViewModel> Obstacles { get; } =
            new BindableCollection<ObstaclePlacementViewModel>();

        public BindableCollection<PlacementViewModel> Placements { get; } =
            new BindableCollection<PlacementViewModel>();

        public StageEntryViewModel SelectedStage
        {
            get => _selectedStage;
            set
            {
                if (_selectedStage == value)
                {
                    return;
                }

                _selectedStage = value;
                NotifyOfPropertyChange();
                LoadStageItems();
            }
        }

        public object SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem == value)
                {
                    return;
                }

                _selectedItem = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(HasSelection));
            }
        }

        public bool HasSelection => SelectedItem is IEditablePlacement;

        public ObstacleDef SelectedObstacleDef
        {
            get => _selectedObstacleDef;
            set
            {
                if (_selectedObstacleDef == value)
                {
                    return;
                }

                _selectedObstacleDef = value;
                NotifyOfPropertyChange();
            }
        }

        public BuildingInfo SelectedBuildingInfo
        {
            get => _selectedBuildingInfo;
            set
            {
                if (_selectedBuildingInfo == value)
                {
                    return;
                }

                _selectedBuildingInfo = value;
                NotifyOfPropertyChange();
            }
        }

        public double MapWidth => SelectedStage?.StageInfo.MapWidth ?? 1200;

        public double MapHeight => SelectedStage?.StageInfo.MapHeight ?? 800;

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage == value)
                {
                    return;
                }

                _statusMessage = value;
                NotifyOfPropertyChange();
            }
        }

        public void AddObstacle()
        {
            if (SelectedStage?.StageInfo == null || SelectedObstacleDef == null)
            {
                return;
            }

            var stage = SelectedStage.StageInfo;
            stage.FixedObstacles.Add(new LevelObstacleDef
            {
                X = 100,
                Y = 100,
                Type = SelectedObstacleDef.Key,
                Width = SelectedObstacleDef.Width,
                Height = SelectedObstacleDef.Height,
                Rotation = 0
            });

            LoadStageItems();
            StatusMessage = $"Added obstacle: {SelectedObstacleDef.Name}";
        }

        public void AddBuilding()
        {
            if (SelectedStage?.StageInfo == null || SelectedBuildingInfo == null)
            {
                return;
            }

            var stage = SelectedStage.StageInfo;
            stage.Placements.Add(new EntityPlacement
            {
                Team = "neutral",
                Type = "Building",
                Key = SelectedBuildingInfo.Id,
                X = 200,
                Y = 200,
                Width = SelectedBuildingInfo.Width,
                Height = SelectedBuildingInfo.Height,
                Rotation = 0
            });

            LoadStageItems();
            StatusMessage = $"Added building: {SelectedBuildingInfo.Name}";
        }

        public void RemoveSelected()
        {
            if (SelectedStage?.StageInfo == null || SelectedItem is not IEditablePlacement editable)
            {
                return;
            }

            var stage = SelectedStage.StageInfo;

            if (SelectedItem is ObstaclePlacementViewModel)
            {
                stage.FixedObstacles.RemoveAll(o =>
                    Math.Abs(o.X - editable.X) < 0.01 && Math.Abs(o.Y - editable.Y) < 0.01
                );
            }
            else if (SelectedItem is PlacementViewModel)
            {
                stage.Placements.RemoveAll(p =>
                    Math.Abs(p.X - editable.X) < 0.01 && Math.Abs(p.Y - editable.Y) < 0.01
                );
            }

            SelectedItem = null;
            LoadStageItems();
            StatusMessage = "Removed selection";
        }

        public void LoadConfig()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "GameConfig.xml|*.xml",
                Title = "Open GameConfig.xml"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            LoadConfigFromPath(dialog.FileName);
        }

        public void SaveConfig()
        {
            if (SelectedStage == null || string.IsNullOrWhiteSpace(_configPath))
            {
                return;
            }

            foreach (var stage in Stages)
            {
                stage.SyncToConfig();
            }

            _configService.Save(_configPath, CurrentConfig);
            StatusMessage = $"Saved: {_configPath}";
        }

        public GameConfigData CurrentConfig { get; private set; }

        private void LoadDefaultConfig()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidate = Path.Combine(baseDir, "GameConfig.xml");
            if (!File.Exists(candidate))
            {
                candidate = Path.Combine(baseDir, "..", "..", "..", "..", "WarcraftBattle", "GameConfig.xml");
                candidate = Path.GetFullPath(candidate);
            }

            if (!File.Exists(candidate))
            {
                StatusMessage = "GameConfig.xml not found. Use Load Config to open.";
                return;
            }

            LoadConfigFromPath(candidate);
        }

        private void LoadConfigFromPath(string path)
        {
            var config = _configService.Load(path);
            _configPath = path;
            CurrentConfig = config;

            Stages.Clear();
            foreach (var stage in config.Stages)
            {
                Stages.Add(new StageEntryViewModel(stage));
            }

            ObstaclePalette.Clear();
            foreach (var obstacle in config.ObstacleDefinitions)
            {
                ObstaclePalette.Add(StageMapper.ToObstacleDef(obstacle));
            }

            BuildingPalette.Clear();
            foreach (var building in config.Buildings)
            {
                BuildingPalette.Add(StageMapper.ToBuildingInfo(building));
            }

            SelectedStage = Stages.Count > 0 ? Stages[0] : null;
            SelectedObstacleDef = ObstaclePalette.Count > 0 ? ObstaclePalette[0] : null;
            SelectedBuildingInfo = BuildingPalette.Count > 0 ? BuildingPalette[0] : null;
            StatusMessage = $"Loaded: {path}";
        }

        private void LoadStageItems()
        {
            Obstacles.Clear();
            Placements.Clear();
            SelectedItem = null;

            if (SelectedStage?.StageInfo == null)
            {
                NotifyOfPropertyChange(nameof(MapWidth));
                NotifyOfPropertyChange(nameof(MapHeight));
                return;
            }

            var stage = SelectedStage.StageInfo;

            for (var i = 0; i < stage.FixedObstacles.Count; i++)
            {
                var obstacle = stage.FixedObstacles[i];
                var name = string.IsNullOrWhiteSpace(obstacle.Type) ? "Obstacle" : obstacle.Type;
                Obstacles.Add(new ObstaclePlacementViewModel(stage, i, name));
            }

            for (var i = 0; i < stage.Placements.Count; i++)
            {
                var placement = stage.Placements[i];
                var name = string.IsNullOrWhiteSpace(placement.Key) ? placement.Type : placement.Key;
                Placements.Add(new PlacementViewModel(stage, i, name));
            }

            NotifyOfPropertyChange(nameof(MapWidth));
            NotifyOfPropertyChange(nameof(MapHeight));
        }
    }
}
