using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using WarcraftBattle.Engine.AI;
using WarcraftBattle.Engine.Animation;
using WarcraftBattle.Engine.Components;
using WarcraftBattle.Shared.Enums;
using WarcraftBattle.Shared.Models;
using WarcraftBattle.Shared.Models.Config;

namespace WarcraftBattle.Engine
{
    public class GameEngine : IGameContext
    {
        public bool ShowDebug = false;
        private bool _isPathfindingDirty = true;
        public double ViewportWidth = 2000;
        public double ViewportHeight = 720;
        public double WorldWidth { get; set; } = 5000;
        public double MapDepth { get; set; } = 5000;
        public double CameraX;
        public double CameraY;
        public double Zoom = 1.0;
        public double MinZoom = 0.5;
        public double MaxZoom = 2.0;

        public double ShakeIntensity { get; set; } = 0;
        public double Gold = 200;
        public int MaxPop = 10;
        public PlayerData Player { get; private set; } = new PlayerData();
        public double TimeScale = 1.0;

        public double FloaterGravity = 0.2;
        public double FloaterUpForce = 2.0;
        public double FloaterSpread = 2.0;
        public bool EnableFog = true;
        public double FogStart = 400;
        public double FogFade = 500;

        public int Stage = 1;
        public GameState State = GameState.Menu;

        private const double TimeStep = 1.0 / 60.0;
        private const double MaxFrameTime = 0.25; // [Fix] Prevent spiral of death
        private double _accumulator = 0;
        public double InterpolationAlpha => _accumulator / TimeStep; // [Fix] Expose alpha for rendering

        public EntityManager EntityManager { get; private set; }
        public List<Entity> Entities => EntityManager.Entities;

        // [浼樺寲] 瀵昏矾绯荤粺
        public IPathfinder Pathfinder { get; private set; }
        public FogOfWar Fog { get; private set; }

        // [浼樺寲] 瀵硅薄姹?
        public ObjectPool<Projectile> ProjectilePool { get; private set; } =
            new ObjectPool<Projectile>(() => new Projectile());

        // [DOD] Particle pool is now just an index manager for the array
        public ObjectPool<VisualEffect> VisualEffectPool { get; private set; } =
            new ObjectPool<VisualEffect>(() => new VisualEffect());

        private List<ParticleEmitter> _particleEmitters = new List<ParticleEmitter>();
        public List<VisualEffect> VisualEffects { get; private set; } = new List<VisualEffect>();
        public static Dictionary<string, ObstacleDef> ObstacleInfo =
            new Dictionary<string, ObstacleDef>();
        public Dictionary<string, EffectConfig> EffectConfigs =
            new Dictionary<string, EffectConfig>();
        public List<Projectile> Projectiles { get; private set; } = new List<Projectile>();

        // [DOD] Particle system refactor
        public ParticleData[] Particles { get; private set; }
        private int _particleCount = 0;
        private const int MAX_PARTICLES = 5000;
        public List<BuildingInfo> Buildings = new List<BuildingInfo>();
        public List<BuildingInfo> BuildingBlueprints = new List<BuildingInfo>();
        public Dictionary<string, BuildingInfo> BuildingRegistry =
            new Dictionary<string, BuildingInfo>();
        public List<UpgradeInfo> PermanentUpgrades = new List<UpgradeInfo>();
        public Dictionary<string, UnitStats> BaseUnitStats = new Dictionary<string, UnitStats>();
        public Dictionary<string, List<UpgradeLevelDef>> UnitUpgradeDefs =
            new Dictionary<string, List<UpgradeLevelDef>>();
        public Dictionary<int, StageInfo> Stages = new Dictionary<int, StageInfo>();
        public EnvironmentConfig EnvConfig = new EnvironmentConfig();
        public int[,] MapData;
        public AIProfile AIProfile { get; private set; } = new AIProfile();

        public int TileSize = 100;

        public StageInfo? CurrentStageInfo;
        private Entity? _selectedEntity;
        public Entity? SelectedEntity
        {
            get => _selectedEntity;
            set
            {
                if (_selectedEntity != value)
                {
                    _selectedEntity = value;
                    OnSelectionChanged?.Invoke();
                }
            }
        }

        public List<Entity> SelectedEntities { get; private set; } = new List<Entity>();
        public Rect SelectionRect { get; set; }
        public bool IsDraggingSelection { get; set; }
        public bool EditorDragActive { get; set; }
        public PointD EditorDragWorldPos { get; set; }

        public IInputManager Input { get; private set; }
        public IAudioManager Audio { get; private set; }
        public AICommander AI { get; private set; }

        // [New] Lighting properties
        public double DayNightTime { get; private set; } = 8.0; // Start at 8 AM
        public double DayNightFactor { get; private set; } = 0.0; // 0=Day, 1=Night
        public Color AmbientColor { get; private set; } = Colors.Transparent;

        public double CombatIntensity { get; private set; } = 0.0; // 0=Calm, 1=Heavy Combat
        public double TotalTime { get; private set; } = 0;
        private double _hitStopTimer = 0;
        private double _originalTimeScale = 1.0;
        public Entity? HoveredEntity { get; set; }

        // ---------------------------------

        public event Action? OnSelectionChanged;

        public bool IsBuildMode = false;
        public BuildingInfo? PendingBuildingInfo;
        public PointD GhostPosition;
        public bool CanBuildAtGhost = false;
        public double AiWaveLevel = 1;
        private double _aiTimer = 0;
        private List<Entity> _tempEntities = new List<Entity>(); // [Optimization] Cache list for updates
        private List<Building> _tempBuildings = new List<Building>(); // [Optimization] Cache for building queries
        private List<Building> _validSpawnersCache = new List<Building>(); // [Optimization] Cache for spawners
        private Queue<FloaterInfo> _floaterQueue = new Queue<FloaterInfo>();

        public bool IsTargetingMode = false;
        public bool IsAttackMoveMode = false;
        public SkillDef? PendingSkill = null;

        public string GameTitle { get; private set; } = "Warcraft";
        public double InitialGold { get; private set; } = 200;
        public double GoldPerSecond { get; private set; } = 5;

        // [New] Control Groups: Group Number -> List of Entity HashCodes (UIDs)
        private Dictionary<int, List<int>> _controlGroups = new Dictionary<int, List<int>>();

        public int BaseMaxPop { get; private set; } = 10;
        public double HouseGoldPerSec { get; private set; } = 2;

        public event Action? OnResourceUpdate;
        public event Action<bool, int>? OnGameOver;
        public event Action<string, double, double, string>? OnFloater;

        public void InvalidatePathfinding()
        {
            _isPathfindingDirty = true;
        }

        public GameEngine()
        {
            Fog = new FogOfWar();
            EntityManager = new EntityManager();
            Input = new InputManager(this);
            Audio = new AudioManager(this);
            AI = new AICommander(this);

            // [DOD] Initialize particle array
            Particles = new ParticleData[MAX_PARTICLES];
            InitData();

            GameServices.Register<IGameContext>(this);
            GameServices.Register<GameEngine>(this);
        }

        // ==========================================
        // 鏍稿績鏁板涓庣┖闂翠紭鍖?
        // ==========================================

        public static PointD WorldToIso(double wx, double wy)
        {
            return new PointD(wx - wy, (wx + wy) * 0.5);
        }

        public static PointD IsoToWorld(double sx, double sy)
        {
            return new PointD(sy + sx * 0.5, sy - sx * 0.5);
        }

        public PointD ScreenToWorld(double screenX, double screenY)
        {
            double isoX = (screenX / Zoom) + CameraX;
            double isoY = (screenY / Zoom) + CameraY;
            return IsoToWorld(isoX, isoY);
        }

        // [浼樺寲] 蹇€熻幏鍙栭檮杩戠殑瀹炰綋 (鏌ヨ鍛ㄥ洿 9 鏍?
        public void GetNearbyEntitiesNonAlloc(
            Entity source,
            List<Entity> output,
            int rangeCells = 1
        )
        {
            EntityManager.GetNearbyEntitiesNonAlloc(source, output, rangeCells);
        }

        // [浼樺寲] 鏃?GC 鐨勫尯鍩熸煡璇㈡柟娉?
        public void GetEntitiesInRectNonAlloc(Rect rect, List<Entity> output)
        {
            EntityManager.GetEntitiesInRectNonAlloc(rect, output);
        }

        public List<Entity> GetEntitiesInRect(Rect rect)
        {
            return EntityManager.GetEntitiesInRect(rect);
        }

        public void SetControlGroup(int groupNumber)
        {
            if (groupNumber < 0 || groupNumber > 9)
                return;

            // Store UIDs (HashCodes) of currently selected living units
            _controlGroups[groupNumber] = SelectedEntities
                .Where(e => e.HP > 0)
                .Select(e => e.GetHashCode())
                .ToList();
            if (SelectedEntities.Count > 0)
            {
                var firstUnit = SelectedEntities[0];
                AddFloater($"Group {groupNumber} set", firstUnit.X, firstUnit.Y, "Cyan");
            }
        }

        public void SelectControlGroup(int groupNumber)
        {
            if (!_controlGroups.TryGetValue(groupNumber, out var uids) || uids.Count == 0)
                return;

            ClearSelection();

            // Create a lookup from UID to Entity for fast retrieval
            var entityMap = Entities.ToDictionary(e => e.GetHashCode(), e => e);
            var livingUids = new List<int>();

            foreach (var uid in uids)
            {
                if (entityMap.TryGetValue(uid, out var entity) && entity.HP > 0)
                {
                    AddToSelection(entity);
                    livingUids.Add(uid);
                }
            }

            // Update the group with only the living units
            _controlGroups[groupNumber] = livingUids;
            RefreshSelectionState();
        }

        public void CenterCameraOnSelection()
        {
            if (SelectedEntities.Count > 0)
            {
                var livingGroup = SelectedEntities.Where(e => e.HP > 0).ToList();
                if (livingGroup.Count == 0)
                    return;

                double sumX = livingGroup.Sum(e => e.X);
                double sumY = livingGroup.Sum(e => e.Y);
                CenterCameraOnPoint(new PointD(sumX / livingGroup.Count, sumY / livingGroup.Count));
            }
        }

        public void AddFloater(
            string t,
            double x,
            double y,
            string c,
            double size = 20,
            double velX = 0,
            double velY = 0
        )
        {
            var r = new Random();
            if (velX == 0)
                velX = (r.NextDouble() - 0.5) * FloaterSpread;
            if (velY == 0)
                velY = -FloaterUpForce;
            _floaterQueue.Enqueue(
                new FloaterInfo
                {
                    Text = t,
                    X = x,
                    Y = y,
                    Color = c,
                    Size = size,
                    VelX = velX,
                    VelY = velY,
                }
            );
            OnFloater?.Invoke(t, x, y, c);
        }

        public void TriggerHitStop(double duration = 0.05, double scale = 0.1)
        {
            if (_hitStopTimer <= 0)
                _originalTimeScale = TimeScale;
            TimeScale = scale;
            _hitStopTimer = duration;
        }

        public void AddEmitter(ParticleEmitter emitter)
        {
            _particleEmitters.Add(emitter);
        }

        public static double GetDamageMultiplier(AttackType atk, ArmorType def)
        {
            return CombatRules.GetMultiplier(atk, def);
        }

        public bool BuyUpgrade(string id)
        {
            var upg = PermanentUpgrades.Find(u => u.Id == id);
            if (upg != null && Player.Honor >= upg.Cost)
            {
                Player.Honor -= upg.Cost;
                Player.PermanentUpgradeIds.Add(id);
                DataManager.Save(Player);
                return true;
            }
            return false;
        }

        public UnitStats GetUnitStats(string key)
        { /* ... [淇濇寔鍘熸牱] ... */
            if (!BaseUnitStats.ContainsKey(key))
                return new UnitStats();
            var stats = BaseUnitStats[key].Clone();
            var entry = Player.UnitLevels.FirstOrDefault(x => x.Key == key);
            int level = entry != null ? entry.Level : 0;
            if (UnitUpgradeDefs.ContainsKey(key))
            {
                var defs = UnitUpgradeDefs[key];
                for (int i = 0; i < level && i < defs.Count; i++)
                {
                    var upg = defs[i];
                    switch (upg.Type)
                    {
                        case "HP":
                            stats.HP += upg.Value;
                            break;
                        case "Dmg":
                            stats.Dmg += upg.Value;
                            break;
                        case "Range":
                            stats.Range += upg.Value;
                            break;
                        case "Speed":
                            stats.Speed += upg.Value;
                            break;
                        case "DoubleShot":
                            stats.DoubleShot = true;
                            break;
                        case "Block":
                            stats.BlockChance = upg.Value;
                            break;
                        case "ManaRegen":
                            stats.ManaRegen += upg.Value;
                            break;
                        case "MaxMana":
                            stats.MaxMana += upg.Value;
                            break;
                    }
                }
            }
            return stats;
        }

        public bool BuyUnitUpgrade(string key)
        { /* ... [淇濇寔鍘熸牱] ... */
            if (!UnitUpgradeDefs.ContainsKey(key))
                return false;
            var entry = Player.UnitLevels.FirstOrDefault(x => x.Key == key);
            int currentLv = entry != null ? entry.Level : 0;
            var defs = UnitUpgradeDefs[key];
            if (currentLv >= defs.Count)
                return false;
            var nextUpg = defs[currentLv];
            if (Player.Honor >= nextUpg.Cost)
            {
                Player.Honor -= nextUpg.Cost;
                if (entry == null)
                {
                    entry = new UnitLevelEntry { Key = key, Level = 0 };
                    Player.UnitLevels.Add(entry);
                }
                entry.Level++;
                DataManager.Save(Player);
                return true;
            }
            return false;
        }

        public UpgradeLevelDef? GetNextUpgrade(string key)
        {
            if (!UnitUpgradeDefs.ContainsKey(key))
                return null;
            var entry = Player.UnitLevels.FirstOrDefault(x => x.Key == key);
            int currentLv = entry != null ? entry.Level : 0;
            var defs = UnitUpgradeDefs[key];
            if (currentLv < defs.Count)
                return defs[currentLv];
            return null;
        }

        public Entity? FindClosestEntity(double x, double y, double maxRange)
        {
            return EntityManager.FindClosestEntity(x, y, maxRange);
        }

        public List<Entity> FindEntitiesInRange(
            double x,
            double y,
            double radius,
            TeamType? teamFilter = null
        )
        {
            return EntityManager.FindEntitiesInRange(x, y, radius, teamFilter);
        }

        public bool IsVisibleToPlayer(Entity target) // Note: teamFilter in FindEntitiesInRange is still a string, needs fixing if used.
        {
            if (!EnableFog || target.Team == TeamType.Human)
                return true; // Player's own units are always visible
            return Fog.IsVisible(target.X, target.Y);
        }

        public int GetUnitLevel(string key)
        {
            var entry = Player.UnitLevels.FirstOrDefault(x => x.Key == key);
            return entry != null ? entry.Level : 0;
        }

        public void PrepareSkill(Unit caster, string skillKey)
        { /* ... [淇濇寔鍘熸牱] ... */
            if (caster.HP <= 0)
                return;
            var skillDef = caster
                .SkillBook.FirstOrDefault(s => s.Definition.Key == skillKey)
                ?.Definition;
            if (skillDef == null)
                return;
            var runtime = caster.SkillBook.First(s => s.Definition.Key == skillKey);
            if (!runtime.IsReady || caster.Mana < skillDef.Cost)
            {
                AddFloater("Not Ready!", caster.X, caster.Y - 40, "Red");
                return;
            }
            if (skillDef.Target == "Point" || skillDef.Target == "Unit")
            {
                PendingSkill = skillDef;
                IsTargetingMode = true;
            }
            else
            {
                ExecuteSkill(caster, skillKey, 0, 0);
            }
        }

        public void ExecuteSkill(Unit caster, string skillKey, double tx, double ty)
        { /* ... [淇濇寔鍘熸牱] ... */
            var runtimeSkill = caster.SkillBook.FirstOrDefault(s => s.Definition.Key == skillKey);
            if (runtimeSkill == null)
                return;
            var skillDef = runtimeSkill.Definition;
            var skill = caster.SkillBook.FirstOrDefault(s => s.Definition.Key == skillKey);
            if (skill == null)
                return;
            caster.Mana -= skill.Definition.Cost;
            skill.CurrentCooldown = skill.Definition.CD;

            // [浼樺寲] Visual Flash Feedback on Cast
            AddFloater(skill.Definition.Name + "!", caster.X, caster.Y - 60, "Cyan", 30, 0, -3.0);

            string animToPlay = null;
            if (AssetManager.HasAnimation(caster.Key, skillKey))
            {
                animToPlay = skillKey;
            }
            else if (AssetManager.HasAnimation(caster.Key, "Cast"))
            {
                animToPlay = "Cast";
            }
            if (animToPlay != null)
            {
                caster.Animator?.Play(animToPlay);
                caster.CastLockTime = skillDef.CastTime;
            }
            ApplySkillEffect(caster, skill.Definition, tx, ty);
            PendingSkill = null;
            IsTargetingMode = false;
        }

        public void CastUnitSkill(Unit caster, string skillKey)
        {
            ExecuteSkill(caster, skillKey, 0, 0);
        }

        private void ApplySkillEffect(Unit caster, SkillDef skill, double tx, double ty)
        {
            if (skill.Key.Contains("Storm") || skill.Key.Contains("Fire"))
                ShakeIntensity = 5.0;
            foreach (var effect in skill.Effects)
            {
                ProcessEffect(caster, effect, tx, ty);
            }
        }

        private void ProcessEffect(Unit caster, SkillEffectDef effect, double tx, double ty)
        {
            List<Entity> targets = new List<Entity>();
            double originX = caster.X;
            double originY = caster.Y;
            if (tx != 0 || ty != 0)
            {
                originX = tx;
                originY = ty;
            }
            if (effect.TargetType == "Self")
                targets.Add(caster);
            else if (effect.TargetType == "Target")
            {
                var target = FindClosestEntity(tx, ty, 80);
                if (target != null)
                    targets.Add(target);
            }
            else if (effect.TargetType == "AreaEnemy")
            {
                var enemyTeam = caster.Team == TeamType.Human ? TeamType.Orc : TeamType.Human;
                targets.AddRange(FindEntitiesInRange(originX, originY, effect.Radius, enemyTeam));
            }
            else if (effect.TargetType == "AreaAlly")
            {
                targets.AddRange(FindEntitiesInRange(originX, originY, effect.Radius, caster.Team));
            }
            foreach (var target in targets)
            {
                switch (effect.Type)
                {
                    case "Damage":
                        if (target.Team == caster.Team && target != caster)
                            break; // Don't damage allies
                        target.TakeDamage(effect.Value, this);
                        CombatIntensity = Math.Min(1.0, CombatIntensity + 0.1); // Add combat intensity on damage
                        if (!string.IsNullOrEmpty(effect.VisualKey))
                        {
                            var vfx = VisualEffectPool.Get();
                            vfx.Init(target.X, target.Y, effect.VisualKey, this);
                            VisualEffects.Add(vfx);
                        }
                        break;
                    case "Heal":
                        if (target.Team != caster.Team && target.Team != TeamType.Neutral)
                        {
                            AddFloater("鏃犳晥鐩爣", target.X, target.Y - 40, "Gray");
                            break;
                        }
                        target.HP = Math.Min(target.MaxHP, target.HP + effect.Value);
                        AddFloater($"+{effect.Value}", target.X, target.Y - 40, "Lime");
                        if (!string.IsNullOrEmpty(effect.VisualKey))
                        {
                            var vfx = VisualEffectPool.Get();
                            vfx.Init(target.X, target.Y, effect.VisualKey, this);
                            VisualEffects.Add(vfx);
                        }
                        break;
                    case "Visual":
                        {
                            var vfx = VisualEffectPool.Get();
                            vfx.Init(target.X, target.Y, effect.VisualKey, this);
                            VisualEffects.Add(vfx);
                        }
                        break;
                    case "Projectile":
                        double launchZ = caster.Height * 0.6;
                        double hitZ = target.Height * 0.5;
                        var proj = ProjectilePool.Get();
                        proj.Init(
                            caster.X,
                            caster.Y,
                            launchZ,
                            target,
                            hitZ,
                            effect.Value,
                            UnitType.Ranged,
                            this,
                            effect.ProjectileId
                        );
                        proj.ArcHeight = 120;
                        Projectiles.Add(proj);
                        break;
                    case "ModifyStat":
                        if (effect.TargetType == "Self")
                        {
                            caster.MaxHP += effect.Value;
                            caster.HP += effect.Value;
                            AddFloater("Stats Up!", caster.X, caster.Y - 60, "Gold");
                            if (!string.IsNullOrEmpty(effect.VisualKey))
                            {
                                var vfx = VisualEffectPool.Get();
                                vfx.Init(caster.X, caster.Y, effect.VisualKey, this);
                                VisualEffects.Add(vfx);
                            }
                        }
                        break;
                }
            }
        }

        private void InitData()
        { /* ... [淇濇寔鍘熸牱] ... */
            Player = DataManager.Load();
            LoadGameConfigFromFiles();
            AssetManager.LoadBuildingAssets(BuildingRegistry);
            AssetManager.LoadEnvironment(EnvConfig);
            AssetManager.LoadObstacleImages(ObstacleInfo);
            InitializeMap();
            AssetManager.Init(BaseUnitStats, EffectConfigs);
        }

        public void InitializeMap(StageInfo stageInfo = null)
        { /* ... [淇濇寔鍘熸牱] ... */
            if (stageInfo != null)
            {
                WorldWidth = stageInfo.MapWidth;
                MapDepth = stageInfo.MapHeight;
            }
            int width = (int)(WorldWidth / TileSize);
            int height = (int)(MapDepth / TileSize);
            MapData = new int[width, height];
            var rand = new Random();
            List<StageTerrainDef> rules = stageInfo?.TerrainRules;
            if (rules == null || rules.Count == 0)
            {
                int defaultId =
                    EnvConfig.TerrainTextures.Count > 0 ? EnvConfig.TerrainTextures[0].Id : 0;
                rules = new List<StageTerrainDef>
                {
                    new StageTerrainDef { TileId = defaultId, Weight = 100 },
                };
            }
            int totalWeight = rules.Sum(t => t.Weight);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int roll = rand.Next(0, totalWeight);
                    int currentSum = 0;
                    MapData[x, y] = rules[0].TileId;
                    foreach (var t in rules)
                    {
                        currentSum += t.Weight;
                        if (roll < currentSum)
                        {
                            MapData[x, y] = t.TileId;
                            break;
                        }
                    }
                }
            }
            if (stageInfo != null)
            {
                if (!string.IsNullOrEmpty(stageInfo.TileMapData))
                {
                    try
                    {
                        var ids = stageInfo
                            .TileMapData.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(int.Parse)
                            .ToList();
                        int dataCols = (int)(stageInfo.MapWidth / TileSize);
                        for (int i = 0; i < ids.Count; i++)
                        {
                            int c = i % dataCols;
                            int r = i / dataCols;
                            if (c < width && r < height)
                            {
                                MapData[c, r] = ids[i];
                            }
                        }
                    }
                    catch { }
                }
                foreach (var rect in stageInfo.TileOverrides)
                {
                    for (int i = 0; i < rect.W; i++)
                    {
                        for (int j = 0; j < rect.H; j++)
                        {
                            int targetX = rect.X + i;
                            int targetY = rect.Y + j;
                            if (targetX >= 0 && targetX < width && targetY >= 0 && targetY < height)
                            {
                                MapData[targetX, targetY] = rect.TileId;
                            }
                        }
                    }
                }
            }

            EntityManager.Initialize(WorldWidth, MapDepth);
            // Initialize Pathfinder
            Pathfinder = new Pathfinder((int)WorldWidth, (int)MapDepth);
            Fog.Initialize((int)WorldWidth, (int)MapDepth);
            GameServices.Register<IPathfinder>(Pathfinder);
        }





        public void Start(int stageId)
        {
            if (stageId > Player.MaxUnlockedStage)
                return;
            if (!Stages.ContainsKey(stageId))
                stageId = 1;

            Stage = stageId;
            CurrentStageInfo = Stages[stageId];
            InitializeMap(CurrentStageInfo);
            State = GameState.Playing;
            IsBuildMode = false;
            EntityManager.Clear();
            Projectiles.Clear();
            _particleEmitters.Clear();
            _particleCount = 0; // Reset particle count
            VisualEffects.Clear();
            _floaterQueue.Clear();
            CameraX = 0;
            CameraY = 0;
            Zoom = 1.0;
            AiWaveLevel = 1 + (stageId - 1) * 0.5;
            SelectedEntity = null;
            Buildings.Clear();
            foreach (var bp in BuildingBlueprints)
                Buildings.Add(bp.Clone());
            Gold = InitialGold + (Player.PermanentUpgradeIds.Contains("gold") ? 100 : 0);
            MaxPop = BaseMaxPop;
            OnResourceUpdate?.Invoke();

            foreach (var p in CurrentStageInfo.Placements)
            {
                TeamType team;
                switch (p.Team.ToLower())
                {
                    case "human":
                        team = TeamType.Human;
                        break;
                    case "orc":
                        team = TeamType.Orc;
                        break;
                    default:
                        team = TeamType.Neutral;
                        break;
                }

                if (p.Type == "Building")
                {
                    BuildingInfo bp = null;
                    if (BuildingRegistry.ContainsKey(p.Key))
                        bp = BuildingRegistry[p.Key];

                    double w = bp?.Width ?? 120;
                    double h = bp?.Height ?? 120;
                    string name = bp?.Name ?? "Unknown";
                    double hp =
                        (p.Key == "stronghold")
                            ? CurrentStageInfo.EnemyBaseHp
                            : (bp?.Hp > 0 ? bp.Hp : 1500);
                    double offY = bp?.OffsetY ?? 0;

                    var info =
                        bp != null
                            ? bp.Clone()
                            : new BuildingInfo
                            {
                                Id = p.Key,
                                Name = name,
                                Width = w,
                                Height = h,
                                OffsetY = offY,
                                Hp = hp,
                            };
                    if (p.Key == "stronghold")
                        info.Hp = CurrentStageInfo.EnemyBaseHp;

                    var b = new Building(p.X, p.Y, team, info);
                    b.Rotation = p.Rotation;
                    if (p.Width > 0) b.Width = p.Width;
                    if (p.Height > 0) b.Height = p.Height;
                    EntityManager.Add(b);
                }
                else if (p.Type == "Unit")
                { /* 鍙墿灞?*/
                }
            }

            foreach (var obs in CurrentStageInfo.FixedObstacles)
            {
                var obstacle = new Obstacle(obs.X, obs.Y, obs.Type);
                obstacle.Rotation = obs.Rotation;
                if (obs.Width > 0) obstacle.Width = obs.Width;
                if (obs.Height > 0) obstacle.Height = obs.Height;
                EntityManager.Add(obstacle);
            }
            GenerateObstacles((int)CurrentStageInfo.RandomObstacleCount);
            var playerBase = Entities.FirstOrDefault(e =>
                e.Team == TeamType.Human && e is Building b && b.Id == "castle"
            );
            if (playerBase != null)
            {
                var isoPos = WorldToIso(playerBase.X, playerBase.Y);
                double visibleW = ViewportWidth / Zoom;
                double visibleH = ViewportHeight / Zoom;
                CameraX = isoPos.X - visibleW / 2;
                CameraY = isoPos.Y - visibleH / 2;
                ClampCamera();
            }
        }

        public void SpawnUnit(string key, TeamType team)
        {
            if (!BaseUnitStats.ContainsKey(key))
                return;

            UnitStats stats;
            if (team == TeamType.Human)
            {
                if (Gold < GetUnitStats(key).Cost)
                    return;
                stats = GetUnitStats(key);
            }
            else
            {
                stats = BaseUnitStats[key].Clone();
                stats.HP *= (1 + AiWaveLevel * 0.05);
                stats.Dmg *= (1 + AiWaveLevel * 0.05);
            }

            // 1. 纭畾鍑虹敓鐐?(Spawn Position)
            double spawnX = team == TeamType.Human ? 200 : WorldWidth - 200;
            double spawnY = MapDepth / 2;

            // 鏌ユ壘鐢熶骇寤虹瓚 (鍏佃惀绛?
            _tempBuildings.Clear();
            for (int i = 0; i < Entities.Count; i++)
            {
                if (Entities[i] is Building b && b.Team == team && b.HP > 0)
                    _tempBuildings.Add(b);
            }

            _validSpawnersCache.Clear();
            foreach (var b in _tempBuildings)
            {
                BuildingInfo blueprint = null;
                if (BuildingRegistry.ContainsKey(b.Id))
                    blueprint = BuildingRegistry[b.Id];
                else
                    blueprint = BuildingRegistry.Values.FirstOrDefault(bp => bp.Name == b.Name);

                if (blueprint != null && blueprint.Produces.Contains(key))
                    _validSpawnersCache.Add(b);
            }

            // 鍏滃簳锛氬鏋滄病鏈夊叺钀ワ紝鍏戒汉浠庡熀鍦板嚭鍏?
            if (_validSpawnersCache.Count == 0)
            {
                if (team == TeamType.Human)
                {
                    AddFloater("闇€瑕佸搴旂殑寤虹瓚!", 0, 0, "Red");
                    return;
                }
                // Fallback: Find stronghold in _tempBuildings
                foreach (var b in _tempBuildings)
                {
                    if (b.Id == "stronghold")
                        _validSpawnersCache.Add(b);
                }
            }

            if (_validSpawnersCache.Count > 0)
            {
                var spawner = _validSpawnersCache[new Random().Next(_validSpawnersCache.Count)];
                // [Fix] Find a free position around the spawner
                var freePos = FindFreeSpawnPosition(
                    spawner.X,
                    spawner.Y + (spawner.Height / 2) + 30,
                    stats.Width
                );
                spawnX = freePos.X;
                spawnY = freePos.Y;
            }
            else
                return;

            Unit u = new Unit(spawnX, spawnY, team, key, stats);

            // ========================================================
            // [鏍稿績淇敼] 鑷姩杩涙敾閫昏緫 (Auto Attack Move)
            // ========================================================

            // 璁＄畻杩涙敾鐩爣鐐癸細濡傛灉鏄汉绫伙紝寰€鍦板浘鏈€鍙宠竟鎵擄紱濡傛灉鏄吔浜猴紝寰€鏈€宸﹁竟鎵撱€?
            double targetX = (team == TeamType.Human) ? WorldWidth : 0;
            double targetY = MapDepth / 2; // 榛樿鎺ㄤ腑璺?

            // [Fix] 绉婚櫎闅忔満鍋忕Щ锛岀‘淇濇墍鏈夊崟浣嶄娇鐢ㄧ浉鍚岀殑杩涙敾鐩爣鐐广€?
            // 鍥犱负 Pathfinder 鐩墠鍙敮鎸佺紦瀛樹竴涓?FlowField锛屽鏋滄瘡涓崟浣嶇洰鏍囦笉鍚岋紝浼氬鑷?FlowField 棰戠箒閲嶇畻锛屽崟浣嶅崱椤裤€?
            // targetY += new Random().Next(-150, 150);

            // 鐩存帴涓嬭揪鈥滄敾鍑荤Щ鍔ㄢ€濆懡浠?
            // 鏁堟灉锛氬崟浣嶄細鑷姩瀵昏矾鍘荤洰鏍囩偣锛岃矾涓婇亣鍒版晫浜轰細鑷姩鍋滀笅鏉ユ敾鍑伙紝鎵撳畬缁х画璧?
            u.OrderAttackMove(targetX, targetY, this);

            // ========================================================

            EntityManager.Add(u);

            if (team == TeamType.Human)
            {
                if (GetUnitCount() > MaxPop) // 娉ㄦ剰锛氳繖閲岄€氬父妫€鏌ユ槸鍚﹁秴杩囦汉鍙ｄ笂闄?
                {
                    return; // 濡傛灉鏈変汉鍙ｉ檺鍒堕€昏緫锛屽彲浠ュ湪杩欓噷 return锛屾垨鑰呬粎浠呮槸鎵ｉ挶
                }

                Gold -= stats.Cost;
                OnResourceUpdate?.Invoke();
            }
        }

        private void GenerateObstacles(int count)
        {
            var r = new Random();
            for (int i = 0; i < count; i++)
            {
                double x = r.Next(400, (int)WorldWidth - 400);
                double y = r.Next(50, (int)MapDepth - 50);
                string type = r.NextDouble() < 0.6 ? "Rock" : "Tree";
                EntityManager.Add(new Obstacle(x, y, type));
            }
        }

        private PointD FindFreeSpawnPosition(double startX, double startY, double radius)
        {
            // 1. Check the initial position first
            var checkRect = new Rect(startX - radius, startY - radius, radius * 2, radius * 2);
            GetEntitiesInRectNonAlloc(checkRect, _tempEntities);
            if (_tempEntities.Count == 0)
            {
                return new PointD(startX, startY);
            }

            // 2. Spiral Search
            double step = radius * 2; // Step size based on unit size
            int maxSteps = 100; // Limit search to avoid infinite loops
            int currentStep = 1;
            int direction = 0; // 0: right, 1: down, 2: left, 3: up
            int stepsInDirection = 1;
            int turnCount = 0;

            double x = startX;
            double y = startY;

            for (int i = 0; i < maxSteps; i++)
            {
                for (int j = 0; j < stepsInDirection; j++)
                {
                    if (direction == 0)
                        x += step;
                    else if (direction == 1)
                        y += step;
                    else if (direction == 2)
                        x -= step;
                    else
                        y -= step;

                    checkRect = new Rect(x - radius, y - radius, radius * 2, radius * 2);
                    GetEntitiesInRectNonAlloc(checkRect, _tempEntities);
                    if (_tempEntities.Count == 0)
                        return new PointD(x, y);
                }
                direction = (direction + 1) % 4;
                if (direction % 2 == 0)
                    stepsInDirection++;
            }

            // Fallback: return original position if no free spot is found
            return new PointD(startX, startY);
        }

        public void Update(double dt)
        {
            if (State != GameState.Playing)
                return;

            Audio.Update(dt);

            // [Fix] Handle HitStop in real-time to avoid issues with low TimeScale
            if (_hitStopTimer > 0)
            {
                _hitStopTimer -= dt;
                if (_hitStopTimer <= 0)
                    TimeScale = _originalTimeScale;
            }

            // [Fix] Spiral of Death protection
            if (dt > MaxFrameTime)
                dt = MaxFrameTime;

            _accumulator += dt * TimeScale;

            while (_accumulator >= TimeStep)
            {
                FixedUpdate(TimeStep);
                _accumulator -= TimeStep;
            }
        }

        private void FixedUpdate(double dt)
        {
            TotalTime += dt;

            // Update AI
            AI.Update(dt);

            // [鍒嗙墖瀵昏矾] 椹卞姩瀵昏矾璁＄畻
            Pathfinder?.Update();

            // [Optimization] Zero GC Fog Update
            _tempEntities.Clear();
            for (int i = 0; i < Entities.Count; i++)
            {
                var e = Entities[i];
                if (e.Team == TeamType.Human && e.HP > 0)
                    _tempEntities.Add(e);
            }
            Fog.Update(_tempEntities);

            // Update Atmosphere
            UpdateAtmosphere(dt);

            // [浼樺寲] 鏇存柊瀵昏矾缃戞牸闃绘尅淇℃伅 (姣忕鏇存柊涓€娆★紝鎴栬€呭湪寤虹瓚鍙樺寲鏃舵洿鏂帮紵涓轰簡绠€鍗曪紝姣?0.5s 鏇存柊涓€娆?
            if (_isPathfindingDirty)
            {
                Pathfinder?.UpdateCollision(Entities);
                _isPathfindingDirty = false;
            }

            if (ShakeIntensity > 0)
            {
                ShakeIntensity -= dt * 20;
                if (ShakeIntensity < 0)
                    ShakeIntensity = 0;
            }

            int houseCount = 0;
            for (int i = 0; i < Entities.Count; i++)
            {
                if (Entities[i] is Building b && b.Team == TeamType.Human && b.Name == "姘戝眳")
                    houseCount++;
            }
            Gold += (GoldPerSecond + houseCount * HouseGoldPerSec) * dt;
            OnResourceUpdate?.Invoke();

            _aiTimer += dt;
            double waveInterval = Math.Max(2.0, 8.0 - Stage * 0.5 - AiWaveLevel * 0.1);
            if (_aiTimer > waveInterval)
            {
                _aiTimer = 0;
                SpawnEnemyWave();
            }

            EntityManager.Update(dt, this);

            // Particle Emitters
            for (int i = _particleEmitters.Count - 1; i >= 0; i--)
            {
                var emitter = _particleEmitters[i];
                emitter.Update(dt);
                if (emitter.IsFinished)
                {
                    _particleEmitters.RemoveAt(i);
                }
            }

            // 鐗规晥鍒楄〃娓呯悊涓庢洿鏂?
            for (int i = VisualEffects.Count - 1; i >= 0; i--)
            {
                var v = VisualEffects[i];
                if (v.HP <= 0)
                {
                    VisualEffects.RemoveAt(i);
                    VisualEffectPool.Return(v);
                    continue;
                }
                v.Update(dt, this);
            }

            // 鎶曞皠鐗╁垪琛ㄦ竻鐞嗕笌鏇存柊
            for (int i = Projectiles.Count - 1; i >= 0; i--)
            {
                var p = Projectiles[i];
                if (!p.Active)
                {
                    Projectiles.RemoveAt(i);
                    ProjectilePool.Return(p); // Recycle
                    continue;
                }
                p.Update(dt, this);
            }

            // 绮掑瓙鍒楄〃娓呯悊涓庢洿鏂?
            for (int i = 0; i < _particleCount; ++i)
            {
                // Update life first
                Particles[i].Life -= dt;
                if (Particles[i].Life <= 0)
                {
                    Particles[i].Active = false;
                }

                // Compact the array: if a particle is dead, swap it with the last living one
                if (!Particles[i].Active)
                {
                    // Swap with the last active particle
                    Particles[i] = Particles[_particleCount - 1];
                    _particleCount--;
                    i--; // Re-evaluate the particle that was just swapped into this slot
                }
                else
                {
                    // Update remaining properties in a tight loop
                    Particles[i].VelY += Particles[i].Gravity * dt;
                    Particles[i].X += Particles[i].VelX * dt;
                    Particles[i].Y += Particles[i].VelY * dt;
                    Particles[i].Rotation += Particles[i].AngularVelocity * dt;

                    float t = 1.0f - (float)(Particles[i].Life / Particles[i].MaxLife);
                    Particles[i].Scale =
                        Particles[i].StartScale
                        + (Particles[i].EndScale - Particles[i].StartScale) * t;
                }
            }

            // 閫変腑瀵硅薄濡傛灉姝讳骸鍒欏彇娑堥€変腑
            if (SelectedEntity != null && SelectedEntity.HP <= 0)
            {
                SelectedEntity = null;
                OnSelectionChanged?.Invoke();
            }

            // [淇] 娓告垙缁撴潫鍒ゅ畾缁熶竴浣跨敤 Key/Id 妫€鏌ワ紝閬垮厤鍥犲悕绉颁慨鏀瑰鑷村垽瀹氬け鏁?
            bool castleAlive = false;
            bool strongholdAlive = false;
            for (int i = 0; i < Entities.Count; i++)
            {
                var e = Entities[i];
                if (e.Team == TeamType.Human && e is Building b && b.Id == "castle")
                    castleAlive = true;
                else if (e.Team == TeamType.Orc && e is Building b2 && b2.Id == "stronghold")
                    strongholdAlive = true;
            }

            if (!castleAlive)
                GameOver(false);
            else if (!strongholdAlive)
                GameOver(true);
        }

        // [DOD] Method to get a new particle index
        public int GetNewParticleIndex()
        {
            if (_particleCount < MAX_PARTICLES)
            {
                return _particleCount++;
            }
            return -1; // Pool is full
        }

        public Color GetParticleColor(ref ParticleData p)
        {
            float t = 1.0f - (float)(p.Life / p.MaxLife);
            if (t < 0)
                t = 0;
            if (t > 1)
                t = 1;

            return Color.FromArgb(
                (byte)(p.StartColor.A + (p.EndColor.A - p.StartColor.A) * t),
                (byte)(p.StartColor.R + (p.EndColor.R - p.StartColor.R) * t),
                (byte)(p.StartColor.G + (p.EndColor.G - p.StartColor.G) * t),
                (byte)(p.StartColor.B + (p.EndColor.B - p.StartColor.B) * t)
            );
        }

        private void UpdateAtmosphere(double dt)
        {
            // Day/Night Cycle & Color Interpolation
            DayNightTime = (DayNightTime + dt * 0.1) % 24; // 240 seconds for a full day
            if (DayNightTime > 18)
                DayNightFactor = Math.Min(1.0, (DayNightTime - 18) / 2.0); // 18-20 fade to night
            else if (DayNightTime < 6)
                DayNightFactor = Math.Min(1.0, (6 - DayNightTime) / 2.0); // 4-6 fade to day
            else
                DayNightFactor = 0;
            Color nightColor = Color.FromArgb(178, 20, 30, 80);
            AmbientColor = Lerp(Colors.Transparent, nightColor, (float)DayNightFactor);

            // Decay Combat Intensity
            if (CombatIntensity > 0)
            {
                CombatIntensity -= dt * 0.1; // Slow decay
                if (CombatIntensity < 0)
                    CombatIntensity = 0;
            }
        }

        private Color Lerp(Color from, Color to, float amount)
        {
            amount = Math.Clamp(amount, 0, 1);
            return Color.FromArgb(
                (byte)(from.A + (to.A - from.A) * amount),
                (byte)(from.R + (to.R - from.R) * amount),
                (byte)(from.G + (to.G - from.G) * amount),
                (byte)(from.B + (to.B - from.B) * amount)
            );
        }

        //

        public void ClampCamera()
        {
            // 1. 鑾峰彇灞忓箷鍥涗釜瑙掔殑閫昏緫鍧愭爣
            // 鎽勫儚鏈烘槸浠?ISO 鍧愭爣 (CameraX, CameraY) 涓哄乏涓婅鐨?
            // 灞忓箷瀹介珮 (viewport) / Zoom 鏄疄闄呮樉绀虹殑鍖哄煙澶у皬

            // 绠€鍗旵lamp绛栫暐锛氶檺鍒?Camera 鐨勪腑蹇冪偣锛屼娇鍏朵笉浼氳瑙嗛噹瓒呭嚭鍦板浘澶繙銆?
            // 浣嗘渶瀹岀編鐨勫仛娉曟槸璁＄畻 "鍙鍖哄煙" 涓?"鍦板浘鑿卞舰" 鐨勪氦闆嗐€?
            // 閴翠簬 2.5D 鐨勫鏉傛€э紝鎴戜滑閲囩敤闄愬埗涓績鐐圭殑鏂瑰紡锛屽苟鏍规嵁 Zoom 鍔ㄦ€佽皟鏁?Margin銆?

            // 璁＄畻褰撳墠瑙嗛噹鐨?ISO 瀹介珮
            double viewW = ViewportWidth / Zoom;
            double viewH = ViewportHeight / Zoom;

            // 璁＄畻灞忓箷涓績 ISO 鍧愭爣
            double isoCenterX = CameraX + viewW / 2;
            double isoCenterY = CameraY + viewH / 2;

            // 灏嗕腑蹇冭浆鍥為€昏緫鍧愭爣
            PointD logicCenter = IsoToWorld(isoCenterX, isoCenterY);

            // 璁＄畻鍙鑼冨洿鍦ㄩ€昏緫鍧愭爣绯讳笅鐨勫ぇ鑷粹€滃崐寰勨€?
            // 杩欐槸涓€涓及绠楀€硷紝鍥犱负瑙嗛噹鏄棆杞殑鐭╁舰銆?
            // 涓轰簡涓嶉湶鍑洪粦杈癸紝鎴戜滑闇€瑕佺‘淇濅腑蹇冪偣璺濈杈圭紭鏈夎冻澶熺殑璺濈銆?
            // 杩欎釜璺濈鍙栧喅浜?Zoom銆俍oom 瓒婂皬锛堣閲庤秺澶э級锛岄渶瑕佺殑 Margin 瓒婂ぇ銆?
            // 浣嗗鏋?Margin 澶ぇ瀵艰嚧 Max < Min锛屽氨璇存槑 Zoom 澶皬浜嗭紝鍦板浘濉笉婊″睆骞曘€?

            // 鎴戜滑灏濊瘯璁＄畻瑙嗗浘鐭╁舰鍦ㄩ€昏緫绌洪棿鐨勫鎺ョ煩褰㈠崐瀹?鍗婇珮
            // 灞忓箷鐭╁舰 (w, h) 瀵瑰簲閫昏緫鍚戦噺:
            // V1 = ScreenToWorld(w, 0) - ScreenToWorld(0, 0)
            // V2 = ScreenToWorld(0, h) - ScreenToWorld(0, 0)
            // 閫昏緫瀹藉害瑕嗙洊绾︿负 (w + h) * factor

            // 绠€鍗曡捣瑙侊紝鎴戜滑鐩存帴闄愬埗閫昏緫涓績鐐?
            // 璁╀腑蹇冪偣濮嬬粓鍦ㄥ湴鍥惧唴閮紝涓旂暀鍑轰竴瀹氱殑浣欓噺
            double margin = 0;

            // 濡傛灉甯屾湜灏藉彲鑳戒笉鏄剧ず榛戣竟锛屾垜浠簲璇ラ檺鍒朵腑蹇冪偣浣垮緱瑙嗗彛瑙掕惤鍒氬ソ纰板埌鍦板浘杈圭紭銆?
            // 浣嗙敱浜庡湴鍥炬槸鑿卞舰锛岃鍙ｆ槸鐭╁舰锛孼oom Out 鍒颁竴瀹氱▼搴﹀繀鐒朵細鏈夐粦杈癸紙闄ら潪鍦板浘鏃犻檺澶э級銆?
            // 杩欓噷鐨勪紭鍖栫洰鏍囨槸锛氬湪 Zoom In 鏃讹紝涓嶈璁╃帺瀹舵妸瑙嗚绉诲埌鍦板浘澶栧お澶氥€?

            // 闄愬埗涓績鐐瑰湪 [0, WorldWidth] x [0, MapDepth] 鑼冨洿鍐?
            // 鍙互绋嶅井鍐呯缉涓€鐐?
            double minX = 0;
            double maxX = WorldWidth;
            double minY = 0;
            double maxY = MapDepth;

            // 搴旂敤闄愬埗
            double clampedX = Math.Max(minX, Math.Min(maxX, logicCenter.X));
            double clampedY = Math.Max(minY, Math.Min(maxY, logicCenter.Y));

            // 濡傛灉鍙戠敓浜嗛檺鍒讹紝閲嶆柊璁＄畻 CameraX/Y
            if (clampedX != logicCenter.X || clampedY != logicCenter.Y)
            {
                PointD newIsoCenter = WorldToIso(clampedX, clampedY);
                CameraX = newIsoCenter.X - viewW / 2;
                CameraY = newIsoCenter.Y - viewH / 2;
            }
        }

        public void CenterCameraOnPoint(PointD worldPoint)
        {
            PointD isoPos = WorldToIso(worldPoint.X, worldPoint.Y);
            double viewW = ViewportWidth / Zoom;
            double viewH = ViewportHeight / Zoom;
            CameraX = isoPos.X - viewW / 2;
            CameraY = isoPos.Y - viewH / 2;
            ClampCamera();
        }

        private void SpawnEnemyWave()
        {
            var r = new Random();
            int humanHeavy = 0;
            int humanLight = 0;
            for (int i = 0; i < Entities.Count; i++)
            {
                var e = Entities[i];
                if (e.Team == TeamType.Human && e is Unit u)
                {
                    if (u.Stats.DefType == ArmorType.Heavy)
                        humanHeavy++;
                    else if (u.Stats.DefType == ArmorType.Light)
                        humanLight++;
                }
            }

            string unitType = "grunt";
            if (humanHeavy > 3 && r.NextDouble() < 0.7)
                unitType = "shaman";
            else if (humanLight > 3 && r.NextDouble() < 0.7)
                unitType = "spearman";
            else
            {
                if (Stage == 2)
                    unitType = r.NextDouble() < 0.6 ? "grunt" : "spearman";
                else if (Stage >= 3)
                {
                    double roll = r.NextDouble();
                    unitType = roll < 0.4 ? "grunt" : (roll < 0.7 ? "spearman" : "rider");
                }
            }
            SpawnUnit(unitType, TeamType.Orc);
            AiWaveLevel += 0.05;
            OnResourceUpdate?.Invoke();
        }

        public void ClearSelection()
        {
            foreach (var e in SelectedEntities)
                e.IsSelected = false;
            SelectedEntities.Clear();
            SelectedEntity = null;
        }

        public void AddToSelection(Entity e)
        {
            if (!SelectedEntities.Contains(e))
            {
                SelectedEntities.Add(e);
                e.IsSelected = true;
            }
        }

        public void RefreshSelectionState()
        {
            if (SelectedEntities.Count > 0)
                SelectedEntity = SelectedEntities[0];
            else
                SelectedEntity = null;

            OnSelectionChanged?.Invoke();
        }

        public void RemoveSelectedEntities()
        {
            if (SelectedEntities.Count == 0)
                return;

            var toRemove = SelectedEntities.ToList();
            ClearSelection();
            foreach (var entity in toRemove)
            {
                EntityManager.Remove(entity);
            }
            RefreshSelectionState();
        }

        public void RotateSelectedEntities(double deltaDegrees)
        {
            if (SelectedEntities.Count == 0)
                return;

            foreach (var entity in SelectedEntities)
            {
                entity.Rotation = (entity.Rotation + deltaDegrees) % 360;
            }
        }

        public void DuplicateEntities(IEnumerable<Entity> sourceEntities, PointD targetCenter)
        {
            if (sourceEntities == null)
                return;

            var sourceList = sourceEntities.Where(e => e != null).ToList();
            if (sourceList.Count == 0)
                return;

            var centerX = sourceList.Average(e => e.X);
            var centerY = sourceList.Average(e => e.Y);
            var offsetX = targetCenter.X - centerX;
            var offsetY = targetCenter.Y - centerY;

            var newEntities = new List<Entity>();
            foreach (var source in sourceList)
            {
                var clone = CreateEntityCopy(source, source.X + offsetX, source.Y + offsetY);
                if (clone != null)
                {
                    EntityManager.Add(clone);
                    newEntities.Add(clone);
                }
            }

            if (newEntities.Count > 0)
            {
                ClearSelection();
                foreach (var entity in newEntities)
                {
                    AddToSelection(entity);
                }
                RefreshSelectionState();
            }
        }

        private Entity? CreateEntityCopy(Entity source, double x, double y)
        {
            switch (source)
            {
                case Unit unit:
                    var stats = unit.Stats.Clone();
                    var newUnit = new Unit(x, y, unit.Team, unit.Key, stats)
                    {
                        Rotation = unit.Rotation,
                        Facing = unit.Facing
                    };
                    newUnit.HP = unit.HP;
                    var mana = unit.GetComponent<ManaComponent>();
                    if (mana != null)
                    {
                        var newMana = newUnit.GetComponent<ManaComponent>();
                        if (newMana != null)
                            newMana.Mana = mana.Mana;
                    }
                    return newUnit;
                case Building building:
                    var info = ResolveBuildingInfo(building);
                    if (info == null)
                        return null;
                    var newBuilding = new Building(x, y, building.Team, info.Clone())
                    {
                        Rotation = building.Rotation
                    };
                    newBuilding.Width = building.Width;
                    newBuilding.Height = building.Height;
                    newBuilding.RenderOffsetY = building.RenderOffsetY;
                    newBuilding.HP = building.HP;
                    return newBuilding;
                case Obstacle obstacle:
                    var newObstacle = new Obstacle(x, y, obstacle.Type, "", obstacle.Width, obstacle.Height)
                    {
                        Rotation = obstacle.Rotation
                    };
                    newObstacle.Width = obstacle.Width;
                    newObstacle.Height = obstacle.Height;
                    newObstacle.RenderOffsetY = obstacle.RenderOffsetY;
                    return newObstacle;
                default:
                    return null;
            }
        }

        private BuildingInfo? ResolveBuildingInfo(Building building)
        {
            if (BuildingRegistry.TryGetValue(building.Id, out var info))
                return info;

            return BuildingRegistry.Values.FirstOrDefault(bp => bp.Name == building.Name);
        }

        public void HandleMouseClick()
        { /* ... [淇濇寔鍘熸牱] ... */
            if (IsBuildMode && PendingBuildingInfo != null && CanBuildAtGhost)
            {
                Gold -= PendingBuildingInfo.Cost;
                var b = new Building(
                    GhostPosition.X,
                    GhostPosition.Y,
                    TeamType.Human,
                    PendingBuildingInfo
                );
                EntityManager.Add(b);
                InvalidatePathfinding();
                AddFloater(
                    $"{PendingBuildingInfo.Name} 寤烘垚!",
                    GhostPosition.X,
                    GhostPosition.Y - 50,
                    "Gold"
                );
                IsBuildMode = false;
                PendingBuildingInfo = null;
                OnResourceUpdate?.Invoke();
            }
            else if (IsBuildMode)
            {
                IsBuildMode = false;
                PendingBuildingInfo = null;
            }
        }

        public void StartPlacingBuilding(string buildingId)
        { /* ... [淇濇寔鍘熸牱] ... */
            var b = Buildings.Find(x => x.Id == buildingId); /*if (b == null || b.Level >= b.Max) return;*/
            int count = Entities.Count(e =>
                e is Building bd && bd.Name == b.Name && bd.Team == TeamType.Human
            );
            if (count >= b.Max)
                return;
            if (Gold < b.Cost)
            {
                AddFloater("閲戝竵涓嶈冻!", 0, 0, "Red");
                return;
            }
            PendingBuildingInfo = b;
            IsBuildMode = true;
        }

        public bool CheckBuildValidity(double x, double y, double w, double h)
        {
            // [淇] 鏍稿績閫昏緫淇锛?
            // 鍥剧墖鐨?h (Height) 閫氬父鍖呭惈寤虹瓚鐨勫瀭鐩撮珮搴︼紙澧欏+灞嬮《锛夛紝涓嶈兘鐩存帴浣滀负鍦伴潰鐨勫崰鍦版繁搴︺€?
            // 鎴戜滑鍋囪缁濆ぇ澶氭暟寤虹瓚鐨勫簳搴ф槸鏂瑰舰鐨勶紝鎴栬€呭叾鍗犲湴娣卞害涓昏鐢卞搴﹀喅瀹氥€?
            // 濡傛灉鎯宠鏇寸簿鍑嗭紝搴旇鍦?XML 閲岄厤缃?"FootprintDepth"锛岃繖閲屾垜浠殏鏃剁敤 Width 浠ｆ浛 Height 浣滀负鍗犲湴娣卞害銆?

            double footprintSize = w; // 鍋囪搴曞骇鏄鏂瑰舰锛岃竟闀?= 瀹藉害

            // 绋嶅井缂╁皬涓€鐐瑰垽瀹氳寖鍥?(0.8)锛屽厑璁歌瑙変笂鐨勮竟缂樿交寰噸鍙狅紝浣撻獙鏇村ソ
            double collisionW = w * 0.8;
            double collisionH = footprintSize * 0.8;

            Rect buildRect = new Rect(
                x - collisionW / 2,
                y - collisionH / 2,
                collisionW,
                collisionH
            );

            // [Optimization] Use spatial query. Inflate rect to catch overlapping entities whose centers are outside.
            Rect queryRect = buildRect;
            queryRect.Inflate(100, 100);

            GetEntitiesInRectNonAlloc(queryRect, _tempEntities);
            foreach (var e in _tempEntities)
            {
                // 鎺掗櫎鎺夎嚜宸憋紙濡傛灉鏄Щ鍔ㄦā寮忥級鍜屾浜″崟浣?
                if (e.HP <= 0)
                    continue;

                // 鍚屾牱鐨勯€昏緫搴旂敤浜庡凡瀛樺湪鐨勫崟浣嶏細
                // 鑾峰彇瀹冧滑鐪熷疄鐨勨€滃崰鍦扮煩褰⑩€濓紝鑰屼笉鏄法澶х殑鍥剧墖鐭╁舰
                double eFootprintDepth = e.Width; // 鍚屾牱鍋囪搴曞骇鏄鏂瑰舰

                double exW = e.Width * 0.8;
                double exH = eFootprintDepth * 0.8;

                Rect existing = new Rect(e.X - exW / 2, e.Y - exH / 2, exW, exH);

                if (buildRect.IntersectsWith(existing))
                    return false;
            }

            // 妫€鏌ユ槸鍚︾鍩哄湴澶繙 (淇濇寔鍘熼€昏緫)
            bool nearBase = false;
            foreach (var e in Entities)
            {
                if (e is Building b && b.Id == "castle")
                {
                    double d = Math.Sqrt(Math.Pow(e.X - x, 2) + Math.Pow(e.Y - y, 2));
                    if (d < 800)
                        nearBase = true; // 绋嶅井鏀瑰ぇ浜嗚寖鍥?
                }
            }
            // 濡傛灉娌℃湁鍩哄湴锛堟瘮濡傜涓€搴э級锛屽垯鍏佽寤洪€?
            if (!Entities.Any(e => e is Building b && b.Id == "castle"))
                nearBase = true;

            return nearBase;
        }

        public int GetFloaterCount() => _floaterQueue.Count;

        public FloaterInfo? PopFloater() =>
            _floaterQueue.Count > 0 ? _floaterQueue.Dequeue() : null;

        public int GetUnitCount() => Entities.Count(e => e.Team == TeamType.Human && e is Unit);

        private void GameOver(bool win)
        {
            State = GameState.Over;
            int reward = 50 + Stage * 10;
            if (Stage < Player.MaxUnlockedStage)
                reward /= 2;
            if (win)
            {
                Player.Honor += reward;
                if (Stage == Player.MaxUnlockedStage)
                    Player.MaxUnlockedStage++;
                DataManager.Save(Player);
            }
            else
            {
                Player.Honor += 5;
                DataManager.Save(Player);
            }
            OnGameOver?.Invoke(win, reward);
        }

        public void EnterTargetingMode()
        {
            IsTargetingMode = true;
        }

        public void EnterAttackMoveMode()
        {
            IsAttackMoveMode = true;
            IsTargetingMode = false;
            PendingSkill = null;
            PendingBuildingInfo = null;
            IsBuildMode = false;
        }

        public void HandleWorldClick(double worldX, double logicY)
        {
            Input.HandleMouseClickInput(worldX, logicY);
        }

        public List<PointD> CalculateFormation(PointD center, int count, double spacing)
        {
            var points = new List<PointD>();
            if (count <= 0)
                return points;

            // 璁＄畻琛屾暟鍜屽垪鏁?(灏介噺鎺ヨ繎姝ｆ柟褰?
            int cols = (int)Math.Ceiling(Math.Sqrt(count));
            int rows = (int)Math.Ceiling((double)count / cols);

            // 璁＄畻闃靛垪鐨勬€诲鍜屾€婚珮
            double totalW = (cols - 1) * spacing;
            double totalH = (rows - 1) * spacing;

            // 璁＄畻宸︿笂瑙掕捣鐐癸紝浣垮緱 center 浣嶄簬闃靛瀷涓績
            double startX = center.X - totalW / 2;
            double startY = center.Y - totalH / 2;

            int added = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (added >= count)
                        break;

                    // 鐢熸垚鐐瑰潗鏍?
                    double px = startX + c * spacing;
                    double py = startY + r * spacing;

                    // [鍙€塢 妫€鏌ヨ鐐规槸鍚﹀彲琛岃蛋 (Pathfinder.IsWalkable)
                    // 濡傛灉涓嶅彲琛岃蛋锛屽彲浠ュ皾璇曞悜鍛ㄥ洿鎸姩锛屾垨鑰呮殏涓斾笉绠′氦缁欏璺郴缁熷鐞?
                    points.Add(new PointD(px, py));
                    added++;
                }
            }
            return points;
        }

        // 鍦?GameEngine.cs 涓坊鍔?
        public bool IsMouseOverEntity(Entity e, double screenX, double screenY)
        {
            // 1. 鑾峰彇鍗曚綅鍦ㄥ睆骞曚笂鐨勯敋鐐逛綅缃?(鑴氬簳涓績)
            PointD isoPos = WorldToIso(e.X, e.Y);
            double anchorX = (isoPos.X - CameraX) * Zoom;
            double anchorY = (isoPos.Y - CameraY) * Zoom;

            // 2. 璁＄畻鈥滆瑙夊垽瀹氭鈥?(蹇呴』鍜?GameSurface.cs 閲岀殑缁樺埗绠楁硶涓€妯′竴鏍凤紒)
            double visualW,
                visualH,
                offsetY;

            if (e is Building b)
            {
                // === 妯℃嫙 DrawBuilding 鐨勯€昏緫 ===

                // 鍩虹缂╂斁
                double scale = 1.0;
                // 濡傛灉鏈?SpriteConfig 缂╂斁 (GameSurface閲岀敤鍒颁簡杩欎釜)
                // 娉ㄦ剰锛氳繖閲岄渶瑕佷綘鑳借闂埌 BuildingInfo 鎴栬€?SpriteConfig
                // 鍋囪 Entity 涓婁繚瀛樹簡杩欎簺杩愯鏃舵暟鎹紝鎴栬€呮垜浠畝鍖栧鐞嗭細

                // 绠€鍗曚及绠楋細澶у鏁板缓绛戠粯鍒跺搴﹀氨鏄€昏緫瀹藉害
                // 濡傛灉浣犵殑 XML 閲?scale != 1.0锛岃繖閲屽繀椤讳箻涓?
                visualW = b.Width * Zoom;

                // [鍏抽敭淇] 楂樺害涓嶈兘鐢?b.Height锛佽鐢ㄨ瑙夋瘮渚嬶紒
                // 缁濆ぇ澶氭暟 2.5D 寤虹瓚鍥剧墖楂樺害閮藉湪瀹藉害鐨?0.8 鍒?1.5 鍊嶄箣闂?
                // 濡傛灉娌℃硶鑾峰彇鍥剧墖鐪熷疄 ratio锛岀粰涓€涓€氱敤鐨勨€滈珮寤虹瓚鈥濅慨姝ｇ郴鏁?
                // 姣斿鍋囪楂樺害鏄搴︾殑 1.2 鍊?(鍖呭惈灞嬮《)
                double approximateRatio = 1.2;

                // 濡傛灉浣犺兘璁块棶鍒板叿浣撶殑 Animator 鎴?Sprite锛屾渶濂界敤鐪熷疄鐨?ratio锛?
                // if (b.Animator != null) approximateRatio = b.Animator.Height / b.Animator.Width;

                visualH = visualW * approximateRatio;

                // 鍨傜洿鍋忕Щ锛欸ameSurface 閲岀敤浜?RenderOffsetY
                offsetY = b.RenderOffsetY * Zoom;
            }
            else
            {
                // 鍗曚綅鐨勯€昏緫 (Unit)
                visualW = e.Width * Zoom;
                visualH = e.Height * Zoom; // 鍗曚綅閫氬父鍙互鐢ㄩ€昏緫楂樺害锛屾垨鑰呬篃涔樹釜绯绘暟
                offsetY = 0;
            }

            // 3. 鏋勫缓鍒ゅ畾鐭╁舰
            // 缁樺埗閫昏緫鏄細x - w/2, y - h + offY
            double left = anchorX - visualW / 2;
            double right = anchorX + visualW / 2;
            // 椤堕儴 = 鑴氬簳 - 瑙嗚楂樺害 + 鍋忕Щ
            double top = anchorY - visualH + offsetY;
            // 搴曢儴 = 鑴氬簳 (鎴栬€呯◢寰悜涓婁竴鐐癸紝鍥犱负鑴氬簳鏄腑蹇?
            double bottom = anchorY;

            // [璋冭瘯寤鸿] 浣犲彲浠ュ湪 GameSurface 閲屾妸杩欎釜鐭╁舰鐢诲嚭鏉?Debug妯″紡)锛岀湅鐪嬪涓嶅

            // 4. 鍒ゅ畾
            return screenX >= left && screenX <= right && screenY >= top && screenY <= bottom;
        }







        public void LoadGameConfig(GameConfigData config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            ApplyConfigs(new List<GameConfigData> { config });
        }

        private void LoadGameConfigFromFiles()
        {
            try
            {
                if (!File.Exists("GameConfig.xml"))
                {
                    MessageBox.Show("GameConfig.xml not found");
                    return;
                }

                var configs = new List<GameConfigData>();

                // 1. Load Main Config
                configs.Add(LoadConfigFromFile("GameConfig.xml"));

                // 2. Load Data Configs
                if (Directory.Exists("Data"))
                {
                    foreach (var file in Directory.GetFiles("Data", "*.xml", SearchOption.AllDirectories))
                    {
                        configs.Add(LoadConfigFromFile(file));
                    }
                }

                ApplyConfigs(configs);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Config Error: {ex.Message}");
            }
        }

        private void ApplyConfigs(List<GameConfigData> configs)
        {
            if (configs == null || configs.Count == 0)
            {
                return;
            }

            try
            {
                // Use the first config (GameConfig.xml) as the base for globals, or merge if needed.
                // For simplicity, we take globals from the first config that has them.
                var config = configs.FirstOrDefault(c => c.GlobalSettings != null) ?? configs[0];

                // 1. Global Settings
                if (config.GlobalSettings != null)
                {
                    GameTitle = config.GlobalSettings.Title;
                    InitialGold = config.GlobalSettings.InitialGold;
                    GoldPerSecond = config.GlobalSettings.BaseGoldPerSec;
                    BaseMaxPop = config.GlobalSettings.MaxPop;
                    HouseGoldPerSec = config.GlobalSettings.HouseGoldPerSec;
                    FloaterGravity = config.GlobalSettings.FloaterGravity;
                    FloaterUpForce = config.GlobalSettings.FloaterUpForce;
                    FloaterSpread = config.GlobalSettings.FloaterSpread;
                    EnableFog = config.GlobalSettings.EnableFog;
                    FogStart = config.GlobalSettings.FogStart;
                    FogFade = config.GlobalSettings.FogFade;
                }

                // 2. AI Profile (Merge or Override? Let's assume override by last loaded)
                var aiConfig = configs.LastOrDefault(c => c.AIProfile != null);
                if (aiConfig != null && aiConfig.AIProfile != null)
                {
                    AIProfile.Aggressiveness = aiConfig.AIProfile.Aggressiveness;
                    AIProfile.WaveInterval = aiConfig.AIProfile.WaveInterval;
                    AIProfile.BuildOrder = new List<string>(aiConfig.AIProfile.BuildOrder);
                }

                // 3. Environment (Override by last)
                var envConfig = configs.LastOrDefault(c => c.Environment != null);
                if (envConfig != null && envConfig.Environment != null)
                {
                    EnvConfig.BackgroundHex = envConfig.Environment.BackgroundHex;
                    EnvConfig.TerrainTextures.Clear();
                    foreach (var t in envConfig.Environment.TerrainTextures)
                    {
                        EnvConfig.TerrainTextures.Add(new TerrainDef { Id = t.Id, Path = t.Path, Weight = t.Weight });
                    }
                }

                // 4. Obstacle Definitions (Merge)
                ObstacleInfo.Clear();
                foreach (var cfg in configs)
                {
                    foreach (var obs in cfg.ObstacleDefinitions)
                    {
                        var def = new ObstacleDef
                        {
                            Key = obs.Key,
                            Name = obs.Name,
                            Image = obs.Image,
                            Folder = obs.Folder,
                            Width = obs.Width,
                            Height = obs.Height,
                            OffsetY = obs.OffsetY,
                            HasCollision = obs.HasCollision
                        };
                        if (obs.SpriteSheet != null)
                        {
                            def.SpriteConfig = new SpriteSheetConfig
                            {
                                Image = obs.SpriteSheet.Image,
                                FrameW = obs.SpriteSheet.FrameW,
                                FrameH = obs.SpriteSheet.FrameH,
                                Count = obs.SpriteSheet.Count,
                                Scale = obs.SpriteSheet.Scale
                            };
                        }
                        if (!string.IsNullOrEmpty(def.Key)) ObstacleInfo[def.Key] = def;
                    }
                }

                // 5. Effects (Merge)
                EffectConfigs.Clear();
                foreach (var cfg in configs)
                {
                    foreach (var eff in cfg.Effects)
                    {
                        var ec = new EffectConfig
                        {
                            Key = eff.Key,
                            Image = eff.Image,
                            FrameW = eff.FrameW,
                            FrameH = eff.FrameH,
                            Count = eff.Count,
                            Speed = eff.Speed,
                            Scale = eff.Scale
                        };
                        if (eff.Light != null)
                        {
                            ec.Light = new LightSourceDef
                            {
                                Color = eff.Light.Color,
                                Radius = eff.Light.Radius,
                                FlickerIntensity = eff.Light.FlickerIntensity,
                                FlickerFrequency = eff.Light.FlickerFrequency
                            };
                        }
                        if (!string.IsNullOrEmpty(ec.Key)) EffectConfigs[ec.Key] = ec;
                    }
                }

                // 6. Units (Merge with Inheritance)
                BaseUnitStats.Clear();
                var rawUnits = new Dictionary<string, UnitConfig>();
                foreach (var cfg in configs)
                {
                    foreach (var u in cfg.Units)
                    {
                        if (!string.IsNullOrEmpty(u.Key))
                            rawUnits[u.Key] = u; // Last loaded overwrites previous with same key
                    }
                }

                foreach (var key in rawUnits.Keys)
                {
                    GetOrLoadUnitStats(key, rawUnits, new HashSet<string>());
                }

                // 7. Unit Upgrades (Merge)
                UnitUpgradeDefs.Clear();
                foreach (var cfg in configs)
                {
                    foreach (var tree in cfg.UnitUpgrades)
                    {
                        if (string.IsNullOrEmpty(tree.Key)) continue;
                        var list = new List<UpgradeLevelDef>();
                        foreach (var lvl in tree.Levels)
                        {
                            list.Add(new UpgradeLevelDef
                            {
                                Cost = lvl.Cost,
                                Type = lvl.Type,
                                Value = lvl.Value,
                                Desc = lvl.Desc
                            });
                        }
                        UnitUpgradeDefs[tree.Key] = list;
                    }
                }

                // 8. Buildings (Merge)
                BuildingBlueprints.Clear();
                BuildingRegistry.Clear();
                foreach (var cfg in configs)
                {
                    foreach (var b in cfg.Buildings)
                    {
                        var info = new BuildingInfo
                        {
                            Id = b.Id,
                            Name = b.Name,
                            Cost = b.Cost,
                            Max = b.Max,
                            Width = b.Width,
                            Height = b.Height,
                            Damage = b.Damage,
                            Range = b.Range,
                            CD = b.CD,
                            Hp = b.Hp,
                            Image = b.Image,
                            Scale = b.Scale,
                            OffsetY = b.OffsetY
                        };
                        if (!string.IsNullOrEmpty(b.Produces))
                        {
                            info.Produces.AddRange(b.Produces.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                        }

                        if (b.Light != null)
                        {
                            info.Light = new LightSourceDef
                            {
                                Color = b.Light.Color,
                                Radius = b.Light.Radius,
                                FlickerIntensity = b.Light.FlickerIntensity,
                                FlickerFrequency = b.Light.FlickerFrequency
                            };
                        }

                        if (b.SpriteSheet != null)
                        {
                            info.SpriteConfig = new SpriteSheetConfig
                            {
                                Image = b.SpriteSheet.Image,
                                FrameW = b.SpriteSheet.FrameW,
                                FrameH = b.SpriteSheet.FrameH,
                                Count = b.SpriteSheet.Count,
                                Scale = b.SpriteSheet.Scale
                            };
                            foreach (var st in b.SpriteSheet.States)
                            {
                                info.SpriteConfig.States.Add(new SpriteAnimDef
                                {
                                    Name = st.Name,
                                    Row = st.Row,
                                    Count = st.Count,
                                    Speed = st.Speed,
                                    Loop = st.Loop
                                });
                            }
                        }

                        if (b.ProjectileSprite != null)
                        {
                            info.ProjectileConfig = new ProjectileSpriteConfig
                            {
                                Image = b.ProjectileSprite.Image,
                                FrameW = b.ProjectileSprite.FrameW,
                                FrameH = b.ProjectileSprite.FrameH,
                                Count = b.ProjectileSprite.Count,
                                Speed = b.ProjectileSprite.Speed,
                                Scale = b.ProjectileSprite.Scale
                            };
                        }

                        if (!string.IsNullOrEmpty(info.Id)) BuildingRegistry[info.Id] = info;
                        if (info.Id != "castle" && info.Id != "stronghold" && !info.Id.StartsWith("orc"))
                        {
                            BuildingBlueprints.Add(info);
                        }
                    }
                }

                // 9. Permanent Upgrades (Merge)
                PermanentUpgrades.Clear();
                foreach (var cfg in configs)
                {
                    foreach (var upg in cfg.Upgrades)
                    {
                        PermanentUpgrades.Add(new UpgradeInfo { Id = upg.Id, Name = upg.Name, Desc = upg.Desc, Cost = upg.Cost });
                    }
                }

                // 10. Stages (Merge)
                Stages.Clear();
                foreach (var cfg in configs)
                {
                    foreach (var s in cfg.Stages)
                    {
                        var stage = new StageInfo
                        {
                            Id = s.Id,
                            Title = s.Title,
                            EnemyBaseHp = s.Hp,
                            MapWidth = s.Width,
                            MapHeight = s.Height,
                            RandomObstacleCount = s.ObstaclesCount,
                            TileMapData = s.TileMapData
                        };

                        foreach (var t in s.Terrain)
                        {
                            stage.TerrainRules.Add(new StageTerrainDef { TileId = t.Id, Weight = t.Weight });
                        }

                        foreach (var r in s.MapTiles)
                        {
                            stage.TileOverrides.Add(new TileRectDef { X = r.X, Y = r.Y, W = r.W, H = r.H, TileId = r.Id });
                        }

                        foreach (var p in s.Placements)
                        {
                            stage.Placements.Add(new EntityPlacement
                            {
                                Team = p.Team,
                                Type = p.Type,
                                Key = p.Key,
                                X = p.X,
                                Y = p.Y,
                                Rotation = p.Rotation,
                                Width = p.Width,
                                Height = p.Height
                            });
                        }

                        foreach (var o in s.Obstacles)
                        {
                            stage.FixedObstacles.Add(new LevelObstacleDef
                            {
                                X = o.X,
                                Y = o.Y,
                                Type = o.Type,
                                Rotation = o.Rotation,
                                Width = o.Width,
                                Height = o.Height
                            });
                        }

                        // Visual Road generation (keep existing logic)
                        for (int x = 0; x <= stage.MapWidth; x += 50)
                        {
                            double y = stage.MapHeight / 2 + Math.Sin(x / 400.0) * (stage.MapHeight / 6);
                            stage.VisualRoad.Add(new PointD(x, y));
                        }

                        Stages[stage.Id] = stage;
                    }
                }
                ValidateConfig();
            }
            catch (Exception ex) { }
            }






        public Entity? GetEntityAtScreenPos(double screenX, double screenY)
        {
            var candidates = Entities.Where(e => e.HP > 0).OrderByDescending(e => e.X + e.Y);

            foreach (var e in candidates)
            {
                PointD isoPos = WorldToIso(e.X, e.Y);
                double anchorX = (isoPos.X - CameraX) * Zoom;
                double anchorY = (isoPos.Y - CameraY) * Zoom;

                double left = 0, right = 0, top = 0, bottom = 0;
                bool hasVisualBounds = false;

                if (e is Building b && b.Animator != null)
                {
                    var frame = b.Animator.GetCurrentFrame();
                    if (frame != null && frame.Value.PixelWidth > 0)
                    {
                        var f = frame.Value;
                        double ratio = (double)f.PixelHeight / f.PixelWidth;
                        double spriteScale = b.Animator.GetScale();
                        double worldW = b.Width * spriteScale;
                        double worldH = worldW * ratio;

                        double visualW = worldW * Zoom;
                        double visualH = worldH * Zoom;
                        double offsetY = b.RenderOffsetY * Zoom;

                        left = anchorX - visualW / 2;
                        right = anchorX + visualW / 2;
                        top = anchorY - visualH + offsetY;
                        bottom = anchorY + offsetY + 10 * Zoom;

                        hasVisualBounds = true;
                    }
                }
                else if (e is Unit u && u.Animator != null)
                {
                    var frame = u.Animator.GetCurrentFrame();
                    if (frame != null && frame.Value.PixelWidth > 0)
                    {
                        var f = frame.Value;
                        double finalScale = 1.5;
                        double totalScale = Math.Abs(u.Animator.GetScale()) * finalScale;

                        double screenImgW = f.PixelWidth * totalScale * Zoom;
                        double screenImgH = f.PixelHeight * totalScale * Zoom;

                        double yOffsetBase = 45.0;
                        double screenYOffset = yOffsetBase * totalScale * Zoom;

                        left = anchorX - screenImgW / 2;
                        right = anchorX + screenImgW / 2;
                        bottom = anchorY + screenYOffset;
                        top = bottom - screenImgH;

                        hasVisualBounds = true;
                    }
                }

                if (!hasVisualBounds)
                {
                    double visualW = e.Width * Zoom;
                    double visualH = e.Height * Zoom;
                    left = anchorX - visualW / 2;
                    right = anchorX + visualW / 2;
                    top = anchorY - visualH;
                    bottom = anchorY;
                }

                if (screenX >= left && screenX <= right &&
                    screenY >= top && screenY <= bottom)
                {
                    return e;
                }
            }
            return null;
        }


        private GameConfigData LoadConfigFromFile(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(GameConfigData));
            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                return (GameConfigData)serializer.Deserialize(fs);
            }
        }

        private UnitStats GetOrLoadUnitStats(string key, Dictionary<string, UnitConfig> rawUnits, HashSet<string> visiting)
        {
            if (BaseUnitStats.ContainsKey(key)) return BaseUnitStats[key];
            if (!rawUnits.ContainsKey(key)) return null;

            if (visiting.Contains(key))
            {
                System.Diagnostics.Debug.WriteLine($"Circular inheritance detected for {key}");
                return new UnitStats { Key = key }; // Fallback
            }

            visiting.Add(key);
            var config = rawUnits[key];
            UnitStats stats = new UnitStats();

            if (!string.IsNullOrEmpty(config.Inherits))
            {
                var parent = GetOrLoadUnitStats(config.Inherits, rawUnits, visiting);
                if (parent != null)
                {
                    stats = parent.Clone();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Parent {config.Inherits} not found for {key}");
                }
            }

            ApplyUnitConfig(stats, config);

            BaseUnitStats[key] = stats;
            visiting.Remove(key);
            return stats;
        }

        private void ApplyUnitConfig(UnitStats stats, UnitConfig config)
        {
            stats.Key = config.Key;
            if (config.Name != null) stats.Name = config.Name;
            if (config.Faction != null) stats.Faction = config.Faction;
            if (config.Cost.HasValue) stats.Cost = config.Cost.Value;
            if (config.HP.HasValue) stats.HP = config.HP.Value;
            if (config.Dmg.HasValue) stats.Dmg = config.Dmg.Value;
            if (config.Range.HasValue) stats.Range = config.Range.Value;
            if (config.Speed.HasValue) stats.Speed = config.Speed.Value;
            if (config.CD.HasValue) stats.CD = (int)config.CD.Value;

            if (config.Type.HasValue) stats.Type = config.Type.Value;
            if (config.AtkType.HasValue) stats.AtkType = config.AtkType.Value;
            if (config.DefType.HasValue) stats.DefType = config.DefType.Value;

            if (config.IsHero.HasValue) stats.IsHero = config.IsHero.Value;
            if (config.IsMounted.HasValue) stats.IsMounted = config.IsMounted.Value;
            if (config.MaxMana.HasValue) stats.MaxMana = config.MaxMana.Value;
            if (config.ManaRegen.HasValue) stats.ManaRegen = config.ManaRegen.Value;
            if (config.BlockChance.HasValue) stats.BlockChance = config.BlockChance.Value;
            if (config.CritChance.HasValue) stats.CritChance = config.CritChance.Value;
            if (config.SightRadius.HasValue) stats.SightRadius = config.SightRadius.Value;
            if (config.Width.HasValue) stats.Width = config.Width.Value;
            if (config.Height.HasValue) stats.Height = config.Height.Value;
            if (config.SkillName != null) stats.SkillName = config.SkillName;

            // Complex objects: Replace if present
            if (config.SpriteSheet != null)
            {
                stats.SpriteConfig = new SpriteSheetConfig
                {
                    Image = config.SpriteSheet.Image,
                    FrameW = config.SpriteSheet.FrameW,
                    FrameH = config.SpriteSheet.FrameH,
                    Scale = config.SpriteSheet.Scale
                };
                foreach (var st in config.SpriteSheet.States)
                {
                    stats.SpriteConfig.States.Add(new SpriteAnimDef
                    {
                        Name = st.Name,
                        Row = st.Row,
                        Count = st.Count,
                        Speed = st.Speed,
                        Loop = st.Loop
                    });
                }
            }

            if (config.ProjectileSprite != null)
            {
                stats.ProjectileConfig = new ProjectileSpriteConfig
                {
                    Image = config.ProjectileSprite.Image,
                    FrameW = config.ProjectileSprite.FrameW,
                    FrameH = config.ProjectileSprite.FrameH,
                    Count = config.ProjectileSprite.Count,
                    Speed = config.ProjectileSprite.Speed,
                    Scale = config.ProjectileSprite.Scale
                };
                if (config.ProjectileSprite.Light != null)
                {
                    stats.ProjectileConfig.Light = new LightSourceDef
                    {
                        Color = config.ProjectileSprite.Light.Color,
                        Radius = config.ProjectileSprite.Light.Radius,
                        FlickerIntensity = config.ProjectileSprite.Light.FlickerIntensity,
                        FlickerFrequency = config.ProjectileSprite.Light.FlickerFrequency
                    };
                }
            }

            if (config.Skills != null && config.Skills.Count > 0)
            {
                stats.SkillDefs.Clear();
                foreach (var s in config.Skills)
                {
                    var skillDef = new SkillDef
                    {
                        Key = s.Key,
                        Name = s.Name,
                        Cost = s.Cost,
                        CD = s.CD,
                        Desc = s.Desc,
                        Target = s.Target,
                        Range = s.Range,
                        Radius = s.Radius,
                        CastTime = s.CastTime
                    };
                    foreach (var e in s.Effects)
                    {
                        skillDef.Effects.Add(new SkillEffectDef
                        {
                            Type = e.Type,
                            TargetType = e.TargetType,
                            Value = e.Value,
                            Radius = e.Radius,
                            Duration = e.Duration,
                            VisualKey = e.VisualKey,
                            ProjectileId = e.ProjectileId
                        });
                    }
                    stats.SkillDefs.Add(skillDef);
                }
            }
        }

        private void ValidateConfig()
        {
            // 1. Check Building Productions
            foreach (var kvp in BuildingRegistry)
            {
                var building = kvp.Value;
                foreach (var unitKey in building.Produces)
                {
                    if (!string.IsNullOrEmpty(unitKey) && !BaseUnitStats.ContainsKey(unitKey))
                    {
                        System.Diagnostics.Debug.WriteLine($"[Config Error] Building '{building.Name}' ({building.Id}) produces unknown unit '{unitKey}'.");
                    }
                }
            }

            // 2. Check Unit Skills
            foreach (var kvp in BaseUnitStats)
            {
                var unit = kvp.Value;
                if (!string.IsNullOrEmpty(unit.SkillName))
                {
                    bool skillExists = unit.SkillDefs.Any(s => s.Key == unit.SkillName);
                    if (!skillExists)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Config Error] Unit '{unit.Name}' ({unit.Key}) references missing skill '{unit.SkillName}'.");
                    }
                }
            }
        }


        public event Action? OnMapChanged;

        public void InvalidateMap()
        {
            OnMapChanged?.Invoke();
        }


        public void SetTile(int x,int y,int tileId)
        {
            if (MapData != null && x >= 0 && y >= 0 && x < MapData.GetLength(0) && y < MapData.GetLength(1))
            {
                if (MapData[x, y] != tileId)
                {
                    MapData[x, y] = tileId;
                    OnMapChanged?.Invoke();
                }
            }
        }

    }
}
