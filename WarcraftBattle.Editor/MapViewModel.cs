using Caliburn.Micro;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using WarcraftBattle.Engine;
using WarcraftBattle.Shared.Enums;
using WarcraftBattle.Shared.Models;
using WarcraftBattle.Shared.Models.Config;

namespace WarcraftBattle.Editor
{
    public enum EditorPlacementMode
    {
        Select,
        Building,
        Obstacle
    }

    public class MapViewModel : Screen
    {
        private StageInfo? _selectedStage;
        private BuildingInfo? _selectedBuilding;
        private ObstacleDef? _selectedObstacle;
        private EditorPlacementMode _placementMode = EditorPlacementMode.Select;
        private string _stageTitle = string.Empty;
        private double _stageWidth;
        private double _stageHeight;
        private double _stageEnemyHp;
        private int _stageObstacleCount;
        private TeamType _selectedTeam = TeamType.Human;

        public MapViewModel(OutputViewModel output)
        {
            Output = output;
            DisplayName = "Map";
            Engine = new GameEngine
            {
                IsEditorMode = true,
                EnableFog = false,
                TimeScale = 1.0
            };

            foreach (var stage in Engine.Stages.Values.OrderBy(s => s.Id))
            {
                Stages.Add(stage);
            }

            foreach (var building in Engine.BuildingRegistry.Values.OrderBy(b => b.Name))
            {
                BuildingOptions.Add(building);
            }

            foreach (var obstacle in GameEngine.ObstacleInfo.Values.OrderBy(o => o.Name))
            {
                ObstacleOptions.Add(obstacle);
            }

            if (Stages.Count > 0)
            {
                SelectedStage = Stages[0];
            }
        }

        public GameEngine Engine { get; }
        public OutputViewModel Output { get; }

        public ObservableCollection<StageInfo> Stages { get; } = new ObservableCollection<StageInfo>();
        public ObservableCollection<BuildingInfo> BuildingOptions { get; } = new ObservableCollection<BuildingInfo>();
        public ObservableCollection<ObstacleDef> ObstacleOptions { get; } = new ObservableCollection<ObstacleDef>();

        public Array TeamOptions => Enum.GetValues(typeof(TeamType));

        public StageInfo? SelectedStage
        {
            get => _selectedStage;
            set
            {
                if (Set(ref _selectedStage, value))
                {
                    LoadStage();
                    UpdateStageFields();
                    NotifyOfPropertyChange(() => CanApplyStageSettings);
                    NotifyOfPropertyChange(() => CanSaveMap);
                }
            }
        }

        public BuildingInfo? SelectedBuilding
        {
            get => _selectedBuilding;
            set
            {
                if (Set(ref _selectedBuilding, value))
                {
                    if (PlacementMode == EditorPlacementMode.Building)
                    {
                        PrepareBuildingGhost();
                    }
                    NotifyOfPropertyChange(() => CanPlaceBuildings);
                }
            }
        }

        public ObstacleDef? SelectedObstacle
        {
            get => _selectedObstacle;
            set
            {
                if (Set(ref _selectedObstacle, value))
                {
                    if (PlacementMode == EditorPlacementMode.Obstacle)
                    {
                        PrepareObstacleGhost();
                    }
                    NotifyOfPropertyChange(() => CanPlaceObstacles);
                }
            }
        }

        public EditorPlacementMode PlacementMode
        {
            get => _placementMode;
            set => Set(ref _placementMode, value);
        }

        public string StageTitle
        {
            get => _stageTitle;
            set => Set(ref _stageTitle, value);
        }

        public double StageWidth
        {
            get => _stageWidth;
            set => Set(ref _stageWidth, value);
        }

        public double StageHeight
        {
            get => _stageHeight;
            set => Set(ref _stageHeight, value);
        }

        public double StageEnemyHp
        {
            get => _stageEnemyHp;
            set => Set(ref _stageEnemyHp, value);
        }

        public int StageObstacleCount
        {
            get => _stageObstacleCount;
            set => Set(ref _stageObstacleCount, value);
        }

        public TeamType SelectedTeam
        {
            get => _selectedTeam;
            set => Set(ref _selectedTeam, value);
        }

        public bool CanApplyStageSettings => SelectedStage != null;
        public bool CanSaveMap => SelectedStage != null;
        public bool CanPlaceBuildings => SelectedBuilding != null;
        public bool CanPlaceObstacles => SelectedObstacle != null;

        private void LoadStage()
        {
            if (SelectedStage == null)
            {
                return;
            }

            Engine.IsEditorMode = true;
            Engine.EnableFog = false;
            Engine.TimeScale = 1.0;
            Engine.Start(SelectedStage.Id);
            Engine.CanBuildAtGhost = false;
            Engine.IsBuildMode = false;
            Engine.PendingBuildingInfo = null;
        }

        private void UpdateStageFields()
        {
            if (SelectedStage == null)
            {
                return;
            }

            StageTitle = SelectedStage.Title;
            StageWidth = SelectedStage.MapWidth;
            StageHeight = SelectedStage.MapHeight;
            StageEnemyHp = SelectedStage.EnemyBaseHp;
            StageObstacleCount = SelectedStage.RandomObstacleCount;
        }

        public void ApplyStageSettings()
        {
            if (SelectedStage == null)
            {
                return;
            }

            SelectedStage.Title = StageTitle;
            SelectedStage.MapWidth = StageWidth;
            SelectedStage.MapHeight = StageHeight;
            SelectedStage.EnemyBaseHp = StageEnemyHp;
            SelectedStage.RandomObstacleCount = StageObstacleCount;

            Engine.Start(SelectedStage.Id);
            Output.AddMessage(string.Format(Properties.Strings.StageUpdatedFormat, SelectedStage.Title));
        }

        public void SetPlacementMode(EditorPlacementMode mode)
        {
            PlacementMode = mode;

            switch (mode)
            {
                case EditorPlacementMode.Building:
                    PrepareBuildingGhost();
                    break;
                case EditorPlacementMode.Obstacle:
                    PrepareObstacleGhost();
                    break;
                default:
                    Engine.IsBuildMode = false;
                    Engine.PendingBuildingInfo = null;
                    Engine.CanBuildAtGhost = false;
                    break;
            }
        }

        private void PrepareBuildingGhost()
        {
            if (SelectedBuilding == null)
            {
                return;
            }

            Engine.PendingBuildingInfo = SelectedBuilding.Clone();
            Engine.IsBuildMode = true;
        }

        private void PrepareObstacleGhost()
        {
            if (SelectedObstacle == null)
            {
                return;
            }

            Engine.PendingBuildingInfo = new BuildingInfo
            {
                Id = "ObstacleGhost",
                Name = SelectedObstacle.Name,
                Width = SelectedObstacle.Width,
                Height = SelectedObstacle.Height
            };
            Engine.IsBuildMode = true;
        }

        public bool CanPlaceAtGhost() => Engine.IsBuildMode && Engine.CanBuildAtGhost;

        public void PlaceBuildingAtGhost()
        {
            if (SelectedStage == null || SelectedBuilding == null)
            {
                return;
            }

            var info = SelectedBuilding.Clone();
            var position = Engine.GhostPosition;
            var building = new Building(position.X, position.Y, SelectedTeam, info);
            Engine.EntityManager.Add(building);
            Engine.InvalidatePathfinding();

            SelectedStage.Placements.Add(new EntityPlacement
            {
                Team = SelectedTeam.ToString().ToLowerInvariant(),
                Type = "Building",
                Key = info.Id,
                X = position.X,
                Y = position.Y,
                Rotation = 0,
                Width = info.Width,
                Height = info.Height
            });
        }

        public void PlaceObstacleAtGhost()
        {
            if (SelectedStage == null || SelectedObstacle == null)
            {
                return;
            }

            var position = Engine.GhostPosition;
            var obstacle = new Obstacle(position.X, position.Y, SelectedObstacle.Key);
            Engine.EntityManager.Add(obstacle);
            Engine.InvalidatePathfinding();

            SelectedStage.FixedObstacles.Add(new LevelObstacleDef
            {
                Type = SelectedObstacle.Key,
                X = position.X,
                Y = position.Y,
                Rotation = 0,
                Width = SelectedObstacle.Width,
                Height = SelectedObstacle.Height
            });
        }

        public void DeleteSelected()
        {
            if (SelectedStage == null || Engine.SelectedEntity == null)
            {
                return;
            }

            var entity = Engine.SelectedEntity;
            Engine.EntityManager.Remove(entity);
            Engine.SelectedEntity = null;

            if (entity is Building building)
            {
                RemovePlacementForBuilding(building);
            }
            else if (entity is Obstacle obstacle)
            {
                RemovePlacementForObstacle(obstacle);
            }
        }

        private void RemovePlacementForBuilding(Building building)
        {
            var matchIndex = SelectedStage!.Placements.FindIndex(p =>
                p.Type == "Building"
                && p.Key == building.Id
                && IsClose(p.X, p.Y, building.X, building.Y)
            );

            if (matchIndex >= 0)
            {
                SelectedStage.Placements.RemoveAt(matchIndex);
            }
        }

        private void RemovePlacementForObstacle(Obstacle obstacle)
        {
            var matchIndex = SelectedStage!.FixedObstacles.FindIndex(o =>
                o.Type == obstacle.Type
                && IsClose(o.X, o.Y, obstacle.X, obstacle.Y)
            );

            if (matchIndex >= 0)
            {
                SelectedStage.FixedObstacles.RemoveAt(matchIndex);
            }
        }

        private static bool IsClose(double x1, double y1, double x2, double y2)
        {
            const double tolerance = 1.0;
            return Math.Abs(x1 - x2) <= tolerance && Math.Abs(y1 - y2) <= tolerance;
        }

        public void SaveMap()
        {
            if (SelectedStage == null)
            {
                return;
            }

            var sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameConfig.xml");
            if (!File.Exists(sourcePath))
            {
                Output.AddMessage(Properties.Strings.GameConfigMissing);
                return;
            }

            var serializer = new XmlSerializer(typeof(GameConfigData));
            GameConfigData config;

            using (var stream = File.OpenRead(sourcePath))
            {
                config = (GameConfigData)serializer.Deserialize(stream)!;
            }

            foreach (var stage in Engine.Stages.Values)
            {
                var stageConfig = config.Stages.FirstOrDefault(s => s.Id == stage.Id);
                if (stageConfig == null)
                {
                    stageConfig = new StageConfig { Id = stage.Id };
                    config.Stages.Add(stageConfig);
                }

                stageConfig.Title = stage.Title;
                stageConfig.Hp = stage.EnemyBaseHp;
                stageConfig.Width = stage.MapWidth;
                stageConfig.Height = stage.MapHeight;
                stageConfig.ObstaclesCount = stage.RandomObstacleCount;
                stageConfig.TileMapData = stage.TileMapData ?? string.Empty;

                stageConfig.Terrain = stage.TerrainRules
                    .Select(t => new StageTerrainConfig { Id = t.TileId, Weight = t.Weight })
                    .ToList();

                stageConfig.MapTiles = stage.TileOverrides
                    .Select(CreateMapTileRectConfig)
                    .ToList();

                stageConfig.Placements = stage.Placements
                    .Select(p => new EntityPlacementConfig
                    {
                        Team = p.Team,
                        Type = p.Type,
                        Key = p.Key,
                        X = p.X,
                        Y = p.Y,
                        Rotation = p.Rotation,
                        Width = p.Width,
                        Height = p.Height
                    })
                    .ToList();

                stageConfig.Obstacles = stage.FixedObstacles
                    .Select(o => new StageObstacleConfig
                    {
                        X = o.X,
                        Y = o.Y,
                        Type = o.Type,
                        Rotation = o.Rotation,
                        Width = o.Width,
                        Height = o.Height
                    })
                    .ToList();
            }

            var outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameConfig.Editor.xml");
            using (var stream = File.Create(outputPath))
            {
                serializer.Serialize(stream, config);
            }

            Output.AddMessage(string.Format(Properties.Strings.SaveCompleteFormat, outputPath));
        }

        private bool Set<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            NotifyOfPropertyChange(propertyName);
            return true;
        }

        private static MapTileRectConfig CreateMapTileRectConfig(TileRectDef tileOverride)
        {
            return new MapTileRectConfig
            {
                X = tileOverride.X,
                Y = tileOverride.Y,
                W = tileOverride.W,
                H = tileOverride.H,
                Id = tileOverride.TileId
            };
        }
    }
}
