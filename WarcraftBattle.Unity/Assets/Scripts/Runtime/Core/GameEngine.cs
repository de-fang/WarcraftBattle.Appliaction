using System;
using System.Collections.Generic;
using System.Linq;
using WarcraftBattle3D.Core.Config;

namespace WarcraftBattle3D.Core
{
    public class GameEngine
    {
        private const double TimeStep = 1.0 / 60.0;
        private const double MaxFrameTime = 0.25;
        private double _accumulator;

        public GameConfigBundle Config { get; }
        public PlayerData Player { get; }
        public EntityManager EntityManager { get; } = new EntityManager();
        public List<Entity> Entities => EntityManager.Entities;
        public List<Projectile> Projectiles { get; } = new List<Projectile>();
        public List<VisualEffect> VisualEffects { get; } = new List<VisualEffect>();
        public IPathfinder Pathfinder { get; private set; }
        public FogOfWar Fog { get; } = new FogOfWar();

        public GameState State { get; private set; } = GameState.Menu;
        public double TimeScale { get; set; } = 1.0;
        public double TotalTime { get; private set; }

        public string GameTitle { get; private set; } = "Warcraft";
        public double InitialGold { get; private set; } = 200;
        public double GoldPerSecond { get; private set; } = 5;
        public double HouseGoldPerSec { get; private set; } = 2;
        public int BaseMaxPop { get; private set; } = 10;

        public double Gold { get; private set; }
        public int MaxPop { get; private set; }

        public int Stage { get; private set; }
        public StageInfo CurrentStageInfo { get; private set; }

        public double WorldWidth { get; private set; } = 5000;
        public double MapDepth { get; private set; } = 5000;
        public int TileSize { get; set; } = 100;
        public int[,] MapData { get; private set; }

        public event Action OnResourceUpdate;
        public event Action<Entity> OnEntitySpawned;

        private readonly List<Entity> _visionSources = new List<Entity>();
        private bool _isPathfindingDirty = true;

        public GameEngine(GameConfigBundle config, PlayerData player = null)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Player = player ?? new PlayerData();

            ApplyGlobals(config.GlobalSettings);
        }

        public void StartGame(int stageId)
        {
            if (stageId > Player.MaxUnlockedStage)
            {
                return;
            }

            if (!Config.Stages.ContainsKey(stageId))
            {
                stageId = 1;
            }

            Stage = stageId;
            CurrentStageInfo = Config.Stages[stageId];
            InitializeMap(CurrentStageInfo);

            State = GameState.Playing;
            TimeScale = 1.0;
            TotalTime = 0;
            _accumulator = 0;

            Gold = InitialGold + (Player.PermanentUpgradeIds.Contains("gold") ? 100 : 0);
            MaxPop = BaseMaxPop;
            EntityManager.Clear();
            Projectiles.Clear();
            VisualEffects.Clear();
            SpawnStageEntities(CurrentStageInfo);
            InvalidatePathfinding();
            UpdatePathfinding();
            OnResourceUpdate?.Invoke();
        }

        public void InitializeMap(StageInfo stageInfo = null)
        {
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
                int defaultId = Config.Environment.TerrainTextures.Count > 0
                    ? Config.Environment.TerrainTextures[0].Id
                    : 0;
                rules = new List<StageTerrainDef>
                {
                    new StageTerrainDef { TileId = defaultId, Weight = 100 }
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

            if (stageInfo == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(stageInfo.TileMapData))
            {
                var ids = stageInfo.TileMapData.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                int dataCols = (int)(stageInfo.MapWidth / TileSize);
                int dataIndex = 0;
                foreach (var token in ids)
                {
                    if (!int.TryParse(token, out int tileId))
                    {
                        dataIndex++;
                        continue;
                    }

                    int c = dataIndex % dataCols;
                    int r = dataIndex / dataCols;
                    if (c < width && r < height)
                    {
                        MapData[c, r] = tileId;
                    }
                    dataIndex++;
                }
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

            if (Pathfinder == null)
            {
                Pathfinder = new Pathfinder((int)WorldWidth, (int)MapDepth);
            }
            else
            {
                Pathfinder.Initialize((int)WorldWidth, (int)MapDepth);
            }

            Fog.Initialize((int)WorldWidth, (int)MapDepth);
        }

        public void Update(double dt)
        {
            if (State != GameState.Playing)
            {
                return;
            }

            if (dt > MaxFrameTime)
            {
                dt = MaxFrameTime;
            }

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

            Gold += GoldPerSecond * dt;
            OnResourceUpdate?.Invoke();

            UpdatePathfinding();
            UpdateFog();

            EntityManager.Update(this, dt);
            UpdateProjectiles(dt);
            UpdateVisualEffects(dt);
        }

        private void ApplyGlobals(GlobalSettingsConfig globals)
        {
            if (globals == null)
            {
                return;
            }

            GameTitle = globals.Title;
            InitialGold = globals.InitialGold;
            GoldPerSecond = globals.BaseGoldPerSec;
            BaseMaxPop = globals.MaxPop;
            HouseGoldPerSec = globals.HouseGoldPerSec;
        }

        private void SpawnStageEntities(StageInfo stage)
        {
            if (stage == null)
            {
                return;
            }

            foreach (var placement in stage.Placements)
            {
                TeamType team = ParseTeam(placement.Team);

                if (string.Equals(placement.Type, "Building", StringComparison.OrdinalIgnoreCase))
                {
                    SpawnBuilding(placement, team);
                }
                else if (string.Equals(placement.Type, "Unit", StringComparison.OrdinalIgnoreCase))
                {
                    SpawnUnit(placement, team);
                }
            }

            foreach (var obstacle in stage.FixedObstacles)
            {
                var def = Config.ObstacleInfo.TryGetValue(obstacle.Type, out var entry) ? entry : null;
                var obs = new Obstacle(obstacle.X, obstacle.Y, obstacle.Type, def);
                obs.Rotation = obstacle.Rotation;
                obs.Width = obstacle.Width > 0 ? obstacle.Width : obs.Width;
                obs.Height = obstacle.Height > 0 ? obstacle.Height : obs.Height;
                AddEntity(obs);
            }
        }

        private void SpawnBuilding(EntityPlacement placement, TeamType team)
        {
            if (!Config.BuildingRegistry.TryGetValue(placement.Key, out var info))
            {
                return;
            }

            var buildingInfo = info.Clone();
            if (placement.Key == "stronghold")
            {
                buildingInfo.Hp = CurrentStageInfo.EnemyBaseHp;
            }

            var building = new Building(placement.X, placement.Y, team, buildingInfo);
            building.Rotation = placement.Rotation;

            if (placement.Width > 0)
            {
                building.Width = placement.Width;
            }

            if (placement.Height > 0)
            {
                building.Height = placement.Height;
            }

            AddEntity(building);
        }

        private void SpawnUnit(EntityPlacement placement, TeamType team)
        {
            if (!Config.BaseUnitStats.TryGetValue(placement.Key, out var stats))
            {
                return;
            }

            var unit = new Unit(placement.X, placement.Y, team, placement.Key, stats);
            unit.Rotation = placement.Rotation;

            if (placement.Width > 0)
            {
                unit.Width = placement.Width;
            }

            if (placement.Height > 0)
            {
                unit.Height = placement.Height;
            }

            AddEntity(unit);
        }

        public void AddEntity(Entity entity)
        {
            if (entity == null)
            {
                return;
            }

            EntityManager.Add(entity);
            if (entity.IsSolid)
            {
                InvalidatePathfinding();
            }
            OnEntitySpawned?.Invoke(entity);
        }

        public Projectile SpawnProjectile(
            Unit source,
            Entity target,
            double damage,
            AttackType attackType,
            UnitType sourceType,
            double impactRadius = 0,
            bool hitAllTeams = false,
            string visualKey = null)
        {
            if (source == null)
            {
                return null;
            }

            string key = string.IsNullOrEmpty(visualKey) ? source.Key : visualKey;
            return SpawnProjectileAt(source.X, source.Y, target, damage, attackType, sourceType, source.Team, impactRadius, hitAllTeams, key);
        }

        public Projectile SpawnProjectileAt(
            double startX,
            double startY,
            Entity target,
            double damage,
            AttackType attackType,
            UnitType sourceType,
            TeamType sourceTeam,
            double impactRadius = 0,
            bool hitAllTeams = false,
            string visualKey = null)
        {
            var projectile = new Projectile();
            projectile.Init(startX, startY, target, damage, attackType, sourceType, sourceTeam, impactRadius, hitAllTeams, visualKey);
            Projectiles.Add(projectile);
            return projectile;
        }

        public Projectile SpawnProjectileAt(
            double startX,
            double startY,
            double targetX,
            double targetY,
            double damage,
            AttackType attackType,
            UnitType sourceType,
            TeamType sourceTeam,
            double impactRadius = 0,
            bool hitAllTeams = false,
            string visualKey = null)
        {
            var projectile = new Projectile();
            projectile.Init(startX, startY, targetX, targetY, damage, attackType, sourceType, sourceTeam, impactRadius, hitAllTeams, visualKey);
            Projectiles.Add(projectile);
            return projectile;
        }

        public VisualEffect AddVisualEffect(string key, double x, double y, double life = 1.0)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            var effect = new VisualEffect(key, x, y, life);
            VisualEffects.Add(effect);
            return effect;
        }

        public bool TryCastSkill(Unit caster, string skillKey, Entity target = null, PointD? point = null)
        {
            if (caster == null || string.IsNullOrEmpty(skillKey) || !caster.IsAlive)
            {
                return false;
            }

            var runtimeSkill = caster.SkillBook.FirstOrDefault(s => s.Definition.Key == skillKey);
            if (runtimeSkill == null)
            {
                return false;
            }

            var skill = runtimeSkill.Definition;
            if (!runtimeSkill.IsReady || !caster.HasMana(skill.Cost))
            {
                return false;
            }

            if (RequiresUnitTarget(skill) && target == null)
            {
                return false;
            }

            PointD? castPoint = point;
            if (!castPoint.HasValue && target != null)
            {
                castPoint = new PointD(target.X, target.Y);
            }

            if (RequiresPointTarget(skill) && !castPoint.HasValue)
            {
                return false;
            }

            if (skill.Range > 0 && castPoint.HasValue)
            {
                double distSq = GetDistSq(caster.X, caster.Y, castPoint.Value.X, castPoint.Value.Y);
                if (distSq > skill.Range * skill.Range)
                {
                    return false;
                }
            }

            caster.SpendMana(skill.Cost);
            runtimeSkill.TriggerCooldown();
            ApplySkillEffects(caster, skill, target, castPoint);
            return true;
        }

        private static TeamType ParseTeam(string team)
        {
            if (string.Equals(team, "human", StringComparison.OrdinalIgnoreCase))
            {
                return TeamType.Human;
            }

            if (string.Equals(team, "orc", StringComparison.OrdinalIgnoreCase))
            {
                return TeamType.Orc;
            }

            return TeamType.Neutral;
        }

        private void ApplySkillEffects(Unit caster, SkillDef skill, Entity target, PointD? point)
        {
            if (caster == null || skill == null)
            {
                return;
            }

            foreach (var effect in skill.Effects)
            {
                ProcessSkillEffect(caster, skill, effect, target, point);
            }
        }

        private void ProcessSkillEffect(Unit caster, SkillDef skill, SkillEffectDef effect, Entity target, PointD? point)
        {
            if (effect == null)
            {
                return;
            }

            ResolveSkillOrigin(caster, target, point, out double originX, out double originY);
            var targets = ResolveSkillTargets(caster, skill, effect, target, originX, originY);
            string effectType = effect.Type ?? string.Empty;

            if (targets.Count == 0)
            {
                if (string.Equals(effectType, "Visual", StringComparison.OrdinalIgnoreCase))
                {
                    AddVisualEffect(effect.VisualKey, originX, originY, ResolveEffectLife(effect));
                }
                else if (string.Equals(effectType, "Projectile", StringComparison.OrdinalIgnoreCase))
                {
                    SpawnProjectileAt(
                        caster.X,
                        caster.Y,
                        originX,
                        originY,
                        effect.Value,
                        AttackType.Normal,
                        UnitType.Ranged,
                        caster.Team,
                        0,
                        false,
                        effect.ProjectileId);
                }
                return;
            }

            foreach (var recipient in targets)
            {
                if (string.Equals(effectType, "Damage", StringComparison.OrdinalIgnoreCase))
                {
                    if (recipient.Team == caster.Team && recipient != caster)
                    {
                        continue;
                    }

                    recipient.TakeDamage(effect.Value);
                    if (!string.IsNullOrEmpty(effect.VisualKey))
                    {
                        AddVisualEffect(effect.VisualKey, recipient.X, recipient.Y, ResolveEffectLife(effect));
                    }
                }
                else if (string.Equals(effectType, "Heal", StringComparison.OrdinalIgnoreCase))
                {
                    if (recipient.Team != caster.Team && recipient.Team != TeamType.Neutral)
                    {
                        continue;
                    }

                    recipient.Heal(effect.Value);
                    if (!string.IsNullOrEmpty(effect.VisualKey))
                    {
                        AddVisualEffect(effect.VisualKey, recipient.X, recipient.Y, ResolveEffectLife(effect));
                    }
                }
                else if (string.Equals(effectType, "Visual", StringComparison.OrdinalIgnoreCase))
                {
                    AddVisualEffect(effect.VisualKey, recipient.X, recipient.Y, ResolveEffectLife(effect));
                }
                else if (string.Equals(effectType, "Projectile", StringComparison.OrdinalIgnoreCase))
                {
                    SpawnProjectile(
                        caster,
                        recipient,
                        effect.Value,
                        AttackType.Normal,
                        UnitType.Ranged,
                        0,
                        false,
                        effect.ProjectileId);
                }
                else if (string.Equals(effectType, "ModifyStat", StringComparison.OrdinalIgnoreCase))
                {
                    recipient.AddMaxHP(effect.Value);
                    if (!string.IsNullOrEmpty(effect.VisualKey))
                    {
                        AddVisualEffect(effect.VisualKey, recipient.X, recipient.Y, ResolveEffectLife(effect));
                    }
                }
            }
        }

        private List<Entity> ResolveSkillTargets(
            Unit caster,
            SkillDef skill,
            SkillEffectDef effect,
            Entity target,
            double originX,
            double originY)
        {
            var targets = new List<Entity>();
            string targetType = effect.TargetType ?? string.Empty;
            double radius = effect.Radius > 0 ? effect.Radius : skill.Radius;

            if (string.Equals(targetType, "Self", StringComparison.OrdinalIgnoreCase))
            {
                targets.Add(caster);
                return targets;
            }

            if (string.Equals(targetType, "Target", StringComparison.OrdinalIgnoreCase))
            {
                if (target != null && target.IsAlive)
                {
                    targets.Add(target);
                    return targets;
                }

                double selectRange = effect.Range > 0
                    ? effect.Range
                    : (skill.Range > 0 ? skill.Range : 80);
                var closest = FindClosestEntity(originX, originY, selectRange);
                if (closest != null)
                {
                    targets.Add(closest);
                }
                return targets;
            }

            if (string.Equals(targetType, "AreaEnemy", StringComparison.OrdinalIgnoreCase))
            {
                var enemyTeam = GetEnemyTeam(caster.Team);
                if (enemyTeam.HasValue)
                {
                    targets.AddRange(EntityManager.FindEntitiesInRange(originX, originY, radius, enemyTeam.Value));
                }
                return targets;
            }

            if (string.Equals(targetType, "AreaAlly", StringComparison.OrdinalIgnoreCase))
            {
                targets.AddRange(EntityManager.FindEntitiesInRange(originX, originY, radius, caster.Team));
                return targets;
            }

            if (string.Equals(targetType, "Area", StringComparison.OrdinalIgnoreCase))
            {
                targets.AddRange(EntityManager.FindEntitiesInRange(originX, originY, radius));
                return targets;
            }

            if (target != null && target.IsAlive)
            {
                targets.Add(target);
                return targets;
            }

            if (radius > 0)
            {
                targets.AddRange(EntityManager.FindEntitiesInRange(originX, originY, radius));
                return targets;
            }

            targets.Add(caster);
            return targets;
        }

        private Entity FindClosestEntity(double x, double y, double maxRange, TeamType? teamFilter = null)
        {
            double maxRangeSq = maxRange * maxRange;
            Entity best = null;
            double bestDistSq = maxRangeSq;

            foreach (var e in Entities)
            {
                if (!e.IsAlive)
                {
                    continue;
                }

                if (teamFilter.HasValue && e.Team != teamFilter.Value)
                {
                    continue;
                }

                double dx = e.X - x;
                double dy = e.Y - y;
                double distSq = dx * dx + dy * dy;
                if (distSq <= bestDistSq)
                {
                    bestDistSq = distSq;
                    best = e;
                }
            }

            return best;
        }

        private static void ResolveSkillOrigin(
            Unit caster,
            Entity target,
            PointD? point,
            out double originX,
            out double originY)
        {
            if (point.HasValue)
            {
                originX = point.Value.X;
                originY = point.Value.Y;
                return;
            }

            if (target != null)
            {
                originX = target.X;
                originY = target.Y;
                return;
            }

            originX = caster.X;
            originY = caster.Y;
        }

        private static TeamType? GetEnemyTeam(TeamType team)
        {
            if (team == TeamType.Human)
            {
                return TeamType.Orc;
            }

            if (team == TeamType.Orc)
            {
                return TeamType.Human;
            }

            return null;
        }

        private static double ResolveEffectLife(SkillEffectDef effect)
        {
            if (effect != null && effect.Duration > 0)
            {
                return effect.Duration;
            }

            return 1.0;
        }

        private static bool RequiresUnitTarget(SkillDef skill)
        {
            return skill != null && string.Equals(skill.Target, "Unit", StringComparison.OrdinalIgnoreCase);
        }

        private static bool RequiresPointTarget(SkillDef skill)
        {
            return skill != null && string.Equals(skill.Target, "Point", StringComparison.OrdinalIgnoreCase);
        }

        private static double GetDistSq(double ax, double ay, double bx, double by)
        {
            double dx = ax - bx;
            double dy = ay - by;
            return dx * dx + dy * dy;
        }

        public void InvalidatePathfinding()
        {
            _isPathfindingDirty = true;
        }

        private void UpdatePathfinding()
        {
            Pathfinder?.Update();

            if (_isPathfindingDirty && Pathfinder != null)
            {
                Pathfinder.UpdateCollision(Entities);
                _isPathfindingDirty = false;
            }
        }

        private void UpdateFog()
        {
            _visionSources.Clear();
            for (int i = 0; i < Entities.Count; i++)
            {
                var e = Entities[i];
                if (e.IsAlive && e.Team == TeamType.Human && e.SightRadius > 0)
                {
                    _visionSources.Add(e);
                }
            }

            Fog.Update(_visionSources);
        }

        private void UpdateProjectiles(double dt)
        {
            for (int i = Projectiles.Count - 1; i >= 0; i--)
            {
                var projectile = Projectiles[i];
                projectile.Update(dt, this);
                if (!projectile.Active)
                {
                    Projectiles.RemoveAt(i);
                }
            }
        }

        private void UpdateVisualEffects(double dt)
        {
            for (int i = VisualEffects.Count - 1; i >= 0; i--)
            {
                var effect = VisualEffects[i];
                effect.Update(dt);
                if (!effect.IsAlive)
                {
                    VisualEffects.RemoveAt(i);
                }
            }
        }
    }
}
