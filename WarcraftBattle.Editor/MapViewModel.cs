using Caliburn.Micro;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using System.Xml.Serialization;
using WarcraftBattle.Engine;
using WarcraftBattle.Engine.Animation;
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
        private UnitStats? _selectedUnit;
        private EditorPlacementMode _placementMode = EditorPlacementMode.Select;
        private string _stageTitle = string.Empty;
        private double _stageWidth;
        private double _stageHeight;
        private double _stageEnemyHp;
        private int _stageObstacleCount;
        private TeamType _selectedTeam = TeamType.Human;
        private string _unitName = string.Empty;
        private string _unitFaction = string.Empty;
        private int _unitCost;
        private double _unitHp;
        private double _unitDamage;
        private double _unitRange;
        private double _unitSpeed;
        private int _unitCooldown;
        private double _unitWidth;
        private double _unitHeight;
        private double _unitMaxMana;
        private double _unitManaRegen;
        private bool _unitIsHero;
        private bool _unitIsMounted;
        private bool _unitDoubleShot;
        private UnitType _unitType;
        private AttackType _unitAttackType;
        private ArmorType _unitArmorType;
        private string _unitSpriteImage = string.Empty;
        private int _unitSpriteFrameW = 32;
        private int _unitSpriteFrameH = 32;
        private int _unitSpriteFrameCount = 1;
        private double _unitSpriteScale = 1.0;
        private bool _isLoadingUnitFields;
        private readonly DispatcherTimer _previewTimer;
        private DateTime _lastPreviewTick;

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

            foreach (var unit in Engine.BaseUnitStats.Values.OrderBy(u => u.Name))
            {
                UnitOptions.Add(unit);
            }

            if (Stages.Count > 0)
            {
                SelectedStage = Stages[0];
            }

            if (UnitOptions.Count > 0)
            {
                SelectedUnit = UnitOptions[0];
            }

            _previewTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(33)
            };
            _previewTimer.Tick += OnPreviewTick;
            _lastPreviewTick = DateTime.UtcNow;
            _previewTimer.Start();

            UnitAnimationPreviews.CollectionChanged += (sender, args) =>
            {
                NotifyOfPropertyChange(() => HasUnitAnimations);
            };
        }

        public GameEngine Engine { get; }
        public OutputViewModel Output { get; }

        public ObservableCollection<StageInfo> Stages { get; } = new ObservableCollection<StageInfo>();
        public ObservableCollection<BuildingInfo> BuildingOptions { get; } = new ObservableCollection<BuildingInfo>();
        public ObservableCollection<ObstacleDef> ObstacleOptions { get; } = new ObservableCollection<ObstacleDef>();
        public ObservableCollection<UnitStats> UnitOptions { get; } = new ObservableCollection<UnitStats>();
        public ObservableCollection<UnitAnimationPreview> UnitAnimationPreviews { get; } = new ObservableCollection<UnitAnimationPreview>();

        public Array TeamOptions => Enum.GetValues(typeof(TeamType));
        public Array UnitTypeOptions => Enum.GetValues(typeof(UnitType));
        public Array AttackTypeOptions => Enum.GetValues(typeof(AttackType));
        public Array ArmorTypeOptions => Enum.GetValues(typeof(ArmorType));

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

        public UnitStats? SelectedUnit
        {
            get => _selectedUnit;
            set
            {
                if (Set(ref _selectedUnit, value))
                {
                    UpdateUnitFields();
                    RebuildUnitAnimationPreviews();
                    NotifyOfPropertyChange(() => HasSelectedUnit);
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

        public string UnitName
        {
            get => _unitName;
            set
            {
                if (Set(ref _unitName, value) && !_isLoadingUnitFields && SelectedUnit != null)
                {
                    SelectedUnit.Name = value;
                }
            }
        }

        public string UnitFaction
        {
            get => _unitFaction;
            set
            {
                if (Set(ref _unitFaction, value) && !_isLoadingUnitFields && SelectedUnit != null)
                {
                    SelectedUnit.Faction = value;
                }
            }
        }

        public int UnitCost
        {
            get => _unitCost;
            set
            {
                if (Set(ref _unitCost, value) && !_isLoadingUnitFields && SelectedUnit != null)
                {
                    SelectedUnit.Cost = value;
                }
            }
        }

        public double UnitHp
        {
            get => _unitHp;
            set
            {
                if (Set(ref _unitHp, value) && !_isLoadingUnitFields && SelectedUnit != null)
                {
                    SelectedUnit.HP = value;
                }
            }
        }

        public double UnitDamage
        {
            get => _unitDamage;
            set
            {
                if (Set(ref _unitDamage, value) && !_isLoadingUnitFields && SelectedUnit != null)
                {
                    SelectedUnit.Dmg = value;
                }
            }
        }

        public double UnitRange
        {
            get => _unitRange;
            set
            {
                if (Set(ref _unitRange, value) && !_isLoadingUnitFields && SelectedUnit != null)
                {
                    SelectedUnit.Range = value;
                }
            }
        }

        public double UnitSpeed
        {
            get => _unitSpeed;
            set
            {
                if (Set(ref _unitSpeed, value) && !_isLoadingUnitFields && SelectedUnit != null)
                {
                    SelectedUnit.Speed = value;
                }
            }
        }

        public int UnitCooldown
        {
            get => _unitCooldown;
            set
            {
                if (Set(ref _unitCooldown, value) && !_isLoadingUnitFields && SelectedUnit != null)
                {
                    SelectedUnit.CD = value;
                }
            }
        }

        public double UnitWidth
        {
            get => _unitWidth;
            set
            {
                if (Set(ref _unitWidth, value) && !_isLoadingUnitFields && SelectedUnit != null)
                {
                    SelectedUnit.Width = value;
                }
            }
        }

        public double UnitHeight
        {
            get => _unitHeight;
            set
            {
                if (Set(ref _unitHeight, value) && !_isLoadingUnitFields && SelectedUnit != null)
                {
                    SelectedUnit.Height = value;
                }
            }
        }

        public double UnitMaxMana
        {
            get => _unitMaxMana;
            set
            {
                if (Set(ref _unitMaxMana, value) && !_isLoadingUnitFields && SelectedUnit != null)
                {
                    SelectedUnit.MaxMana = value;
                }
            }
        }

        public double UnitManaRegen
        {
            get => _unitManaRegen;
            set
            {
                if (Set(ref _unitManaRegen, value) && !_isLoadingUnitFields && SelectedUnit != null)
                {
                    SelectedUnit.ManaRegen = value;
                }
            }
        }

        public bool UnitIsHero
        {
            get => _unitIsHero;
            set
            {
                if (Set(ref _unitIsHero, value) && !_isLoadingUnitFields && SelectedUnit != null)
                {
                    SelectedUnit.IsHero = value;
                }
            }
        }

        public bool UnitIsMounted
        {
            get => _unitIsMounted;
            set
            {
                if (Set(ref _unitIsMounted, value) && !_isLoadingUnitFields && SelectedUnit != null)
                {
                    SelectedUnit.IsMounted = value;
                }
            }
        }

        public bool UnitDoubleShot
        {
            get => _unitDoubleShot;
            set
            {
                if (Set(ref _unitDoubleShot, value) && !_isLoadingUnitFields && SelectedUnit != null)
                {
                    SelectedUnit.DoubleShot = value;
                }
            }
        }

        public UnitType UnitType
        {
            get => _unitType;
            set
            {
                if (Set(ref _unitType, value) && !_isLoadingUnitFields && SelectedUnit != null)
                {
                    SelectedUnit.Type = value;
                }
            }
        }

        public AttackType UnitAttackType
        {
            get => _unitAttackType;
            set
            {
                if (Set(ref _unitAttackType, value) && !_isLoadingUnitFields && SelectedUnit != null)
                {
                    SelectedUnit.AtkType = value;
                }
            }
        }

        public ArmorType UnitArmorType
        {
            get => _unitArmorType;
            set
            {
                if (Set(ref _unitArmorType, value) && !_isLoadingUnitFields && SelectedUnit != null)
                {
                    SelectedUnit.DefType = value;
                }
            }
        }

        public string UnitSpriteImage
        {
            get => _unitSpriteImage;
            set
            {
                if (Set(ref _unitSpriteImage, value) && !_isLoadingUnitFields)
                {
                    if (SelectedUnit == null)
                    {
                        return;
                    }
                    EnsureSpriteConfig();
                    SelectedUnit.SpriteConfig!.Image = value;
                    RefreshUnitSpritePreview();
                }
            }
        }

        public int UnitSpriteFrameW
        {
            get => _unitSpriteFrameW;
            set
            {
                if (Set(ref _unitSpriteFrameW, value) && !_isLoadingUnitFields)
                {
                    if (SelectedUnit == null)
                    {
                        return;
                    }
                    EnsureSpriteConfig();
                    SelectedUnit.SpriteConfig!.FrameW = value;
                    RefreshUnitSpritePreview();
                }
            }
        }

        public int UnitSpriteFrameH
        {
            get => _unitSpriteFrameH;
            set
            {
                if (Set(ref _unitSpriteFrameH, value) && !_isLoadingUnitFields)
                {
                    if (SelectedUnit == null)
                    {
                        return;
                    }
                    EnsureSpriteConfig();
                    SelectedUnit.SpriteConfig!.FrameH = value;
                    RefreshUnitSpritePreview();
                }
            }
        }

        public int UnitSpriteFrameCount
        {
            get => _unitSpriteFrameCount;
            set
            {
                if (Set(ref _unitSpriteFrameCount, value) && !_isLoadingUnitFields)
                {
                    if (SelectedUnit == null)
                    {
                        return;
                    }
                    EnsureSpriteConfig();
                    SelectedUnit.SpriteConfig!.Count = value;
                    RefreshUnitSpritePreview();
                }
            }
        }

        public double UnitSpriteScale
        {
            get => _unitSpriteScale;
            set
            {
                if (Set(ref _unitSpriteScale, value) && !_isLoadingUnitFields)
                {
                    if (SelectedUnit == null)
                    {
                        return;
                    }
                    EnsureSpriteConfig();
                    SelectedUnit.SpriteConfig!.Scale = value;
                    RefreshUnitSpritePreview();
                }
            }
        }

        public bool CanApplyStageSettings => SelectedStage != null;
        public bool CanSaveMap => SelectedStage != null;
        public bool CanPlaceBuildings => SelectedBuilding != null;
        public bool CanPlaceObstacles => SelectedObstacle != null;
        public bool HasSelectedUnit => SelectedUnit != null;
        public bool HasUnitAnimations => UnitAnimationPreviews.Count > 0;

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

        private void UpdateUnitFields()
        {
            _isLoadingUnitFields = true;

            if (SelectedUnit == null)
            {
                UnitName = string.Empty;
                UnitFaction = string.Empty;
                UnitCost = 0;
                UnitHp = 0;
                UnitDamage = 0;
                UnitRange = 0;
                UnitSpeed = 0;
                UnitCooldown = 0;
                UnitWidth = 0;
                UnitHeight = 0;
                UnitMaxMana = 0;
                UnitManaRegen = 0;
                UnitIsHero = false;
                UnitIsMounted = false;
                UnitDoubleShot = false;
                UnitType = UnitType.Melee;
                UnitAttackType = AttackType.Normal;
                UnitArmorType = ArmorType.Medium;
                UnitSpriteImage = string.Empty;
                UnitSpriteFrameW = 32;
                UnitSpriteFrameH = 32;
                UnitSpriteFrameCount = 1;
                UnitSpriteScale = 1.0;
            }
            else
            {
                UnitName = SelectedUnit.Name;
                UnitFaction = SelectedUnit.Faction;
                UnitCost = SelectedUnit.Cost;
                UnitHp = SelectedUnit.HP;
                UnitDamage = SelectedUnit.Dmg;
                UnitRange = SelectedUnit.Range;
                UnitSpeed = SelectedUnit.Speed;
                UnitCooldown = SelectedUnit.CD;
                UnitWidth = SelectedUnit.Width;
                UnitHeight = SelectedUnit.Height;
                UnitMaxMana = SelectedUnit.MaxMana;
                UnitManaRegen = SelectedUnit.ManaRegen;
                UnitIsHero = SelectedUnit.IsHero;
                UnitIsMounted = SelectedUnit.IsMounted;
                UnitDoubleShot = SelectedUnit.DoubleShot;
                UnitType = SelectedUnit.Type;
                UnitAttackType = SelectedUnit.AtkType;
                UnitArmorType = SelectedUnit.DefType;

                var spriteConfig = SelectedUnit.SpriteConfig;
                UnitSpriteImage = spriteConfig?.Image ?? string.Empty;
                UnitSpriteFrameW = spriteConfig?.FrameW ?? 32;
                UnitSpriteFrameH = spriteConfig?.FrameH ?? 32;
                UnitSpriteFrameCount = spriteConfig?.Count ?? 1;
                UnitSpriteScale = spriteConfig?.Scale ?? 1.0;
            }

            _isLoadingUnitFields = false;
        }

        private void EnsureSpriteConfig()
        {
            if (SelectedUnit == null)
            {
                return;
            }

            if (SelectedUnit.SpriteConfig == null)
            {
                SelectedUnit.SpriteConfig = new SpriteSheetConfig();
            }
        }

        private void RefreshUnitSpritePreview()
        {
            if (SelectedUnit == null)
            {
                return;
            }

            AssetManager.Init(Engine.BaseUnitStats, Engine.EffectConfigs);
            RebuildUnitAnimationPreviews();
        }

        private void RebuildUnitAnimationPreviews()
        {
            UnitAnimationPreviews.Clear();

            if (SelectedUnit?.SpriteConfig == null)
            {
                return;
            }

            var stateNames = SelectedUnit.SpriteConfig.States
                .Select(s => s.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToList();

            foreach (var stateName in stateNames)
            {
                var animator = AssetManager.CreateAnimator(SelectedUnit.Key);
                if (animator == null)
                {
                    continue;
                }

                animator.Play(stateName, true);
                var preview = new UnitAnimationPreview(stateName, animator);
                preview.Update(0);
                UnitAnimationPreviews.Add(preview);
            }
        }

        private void OnPreviewTick(object? sender, EventArgs e)
        {
            if (UnitAnimationPreviews.Count == 0)
            {
                _lastPreviewTick = DateTime.UtcNow;
                return;
            }

            var now = DateTime.UtcNow;
            var dt = (now - _lastPreviewTick).TotalSeconds;
            _lastPreviewTick = now;

            foreach (var preview in UnitAnimationPreviews)
            {
                preview.Update(dt);
            }
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
                    .Select(t => new MapTileRectConfig
                    {
                        X = t.X,
                        Y = t.Y,
                        W = t.W,
                        H = t.H,
                        Id = t.TileId // MapTileRectConfig exposes the tile identifier as Id.
                    })
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
    }
}
