using System;
using System.Collections.Generic;
using WarcraftBattle.Engine.AI;
using WarcraftBattle.Engine.Animation;
using WarcraftBattle.Engine.Components;
using WarcraftBattle.Shared.Enums;
using WarcraftBattle.Shared.Models;

namespace WarcraftBattle.Engine
{
    // [RVO] Simplified Reciprocal Velocity Obstacles Solver
    // [RVO] 改进版 Reciprocal Velocity Obstacles Solver
    public static class RVOSolver
    {
        private static Random _rnd = new Random();
        private static List<PointD> _candidatesCache = new List<PointD>(64); // 稍微加大缓存

        public static PointD CalculateVelocity(
            Unit agent,
            PointD desiredVel,
            List<Entity> neighbors,
            double dt
        )
        {
            double radius = agent.Width * 0.5;
            double timeHorizon = 1.0; // 稍微缩短预测时间，反应更灵敏

            if (neighbors.Count == 0)
                return desiredVel;

            PointD bestVel = desiredVel;
            double minPenalty = double.MaxValue;
            double desiredSpeed = Math.Sqrt(desiredVel.X * desiredVel.X + desiredVel.Y * desiredVel.Y);

            _candidatesCache.Clear();
            _candidatesCache.Add(desiredVel);

            // [新增] 如果处于静止或被卡住，尝试保留当前动量或停止
            if (desiredSpeed > 0.1)
                _candidatesCache.Add(agent.Velocity);
            else
                _candidatesCache.Add(new PointD(0, 0));

            // [核心改进] 生成采样点
            // 1. 围绕期望方向的扇形采样
            if (desiredSpeed > 0.1)
            {
                // 角度采样：包含小角度避让和大角度转向
                double[] angles = { 0.2, -0.2, 0.5, -0.5, 0.9, -0.9, 1.5, -1.5, 2.0, -2.0, 2.5, -2.5 };
                foreach (var ang in angles)
                {
                    double ca = Math.Cos(ang);
                    double sa = Math.Sin(ang);
                    double rx = desiredVel.X * ca - desiredVel.Y * sa;
                    double ry = desiredVel.X * sa + desiredVel.Y * ca;
                    _candidatesCache.Add(new PointD(rx, ry));
                    // 添加半速采样，方便在拥挤时减速
                    _candidatesCache.Add(new PointD(rx * 0.5, ry * 0.5));
                }
            }

            // [核心改进] 2. 针对最近邻居的“反向逃逸”采样
            // 如果离得很近，强制添加一个背离邻居的速度作为候选
            Entity closest = null;
            double minDstSq = double.MaxValue;

            foreach (var n in neighbors)
            {
                if (n == agent || n.HP <= 0 || !n.IsSolid) continue;
                double d2 = (n.X - agent.X) * (n.X - agent.X) + (n.Y - agent.Y) * (n.Y - agent.Y);
                if (d2 < minDstSq) { minDstSq = d2; closest = n; }
            }

            if (closest != null)
            {
                // 这是一个从邻居指向自己的向量
                double ex = agent.X - closest.X;
                double ey = agent.Y - closest.Y;
                double len = Math.Sqrt(ex * ex + ey * ey);
                if (len < 0.001) { ex = _rnd.NextDouble() - 0.5; ey = _rnd.NextDouble() - 0.5; len = 1; } // 防止完全重叠

                // 添加一个以最大速度逃离邻居的向量
                double maxSpeed = agent.Speed;
                _candidatesCache.Add(new PointD((ex / len) * maxSpeed, (ey / len) * maxSpeed));
            }


            // --- 评估所有候选速度 ---
            foreach (var v in _candidatesCache)
            {
                double penalty = 0;
                double vLen = Math.Sqrt(v.X * v.X + v.Y * v.Y);

                // 1. 偏离期望速度的惩罚
                double dx = v.X - desiredVel.X;
                double dy = v.Y - desiredVel.Y;
                penalty += Math.Sqrt(dx * dx + dy * dy) * 1.0; // 稍微提高权重

                // [修改] “回头”惩罚逻辑
                // 只有当不需要避让时，回头才应该被重罚。如果为了避让，回头是允许的。
                // 我们降低这个权值，从 200 降到 10，防止它压倒避让逻辑
                if (desiredVel.X * v.X + desiredVel.Y * v.Y < 0)
                {
                    penalty += 10.0;
                }

                // 2. 碰撞惩罚计算
                double maxCollisionPenalty = 0; // 记录最严重的单一碰撞

                foreach (var n in neighbors)
                {
                    if (n == agent || n.HP <= 0 || !n.IsSolid) continue;

                    PointD nVel = (n is Unit u) ? u.Velocity : new PointD(0, 0);
                    PointD relPos = new PointD(n.X - agent.X, n.Y - agent.Y);
                    PointD relVel = new PointD(v.X - nVel.X, v.Y - nVel.Y);

                    double distSq = relPos.X * relPos.X + relPos.Y * relPos.Y;
                    double combinedRadius = radius + n.Width * 0.45; // 稍微缩小一点半径，允许轻微重叠，减少抖动
                    double combinedRadiusSq = combinedRadius * combinedRadius;
                    double rvSq = relVel.X * relVel.X + relVel.Y * relVel.Y;

                    // 计算碰撞时间 t
                    // t = Dot(relPos, relVel) / |relVel|^2
                    // 但我们需要的是 Ray-Circle Intersection 的近似
                    // 简化版 RVO: 检查是否正在接近

                    double dot = relPos.X * relVel.X + relPos.Y * relVel.Y;

                    // 情况 A: 已经重叠
                    if (distSq < combinedRadiusSq)
                    {
                        if (dot < 0)
                        {
                            // 正在远离 (relVel 和 relPos 方向相反)
                            // [核心修复] 这是一个好的候选！不要惩罚，或者给奖励！
                            // 之前的代码在这里 penalty += 5000，这是导致卡住的元凶
                            penalty -= 50; // 奖励这种行为
                        }
                        else
                        {
                            // 正在靠近或保持重叠
                            penalty += 10000;
                        }
                    }
                    // 情况 B: 尚未重叠，但可能相撞
                    else if (dot > 0) // 只有当相对速度方向指向彼此时才可能相撞
                    {
                        // 简单的线性预测
                        // 如果 (relPos + relVel * t) 的长度小于半径，则碰撞
                        // 这里的 dot > 0 意味着我们在靠近。
                        // 估算最近点距离

                        if (rvSq > 0.0001)
                        {
                            double t = dot / rvSq; // 到达最近点的时间
                            if (t < timeHorizon)
                            {
                                // 最近点距离平方
                                double distCpSq = distSq - t * t * rvSq;
                                if (distCpSq < combinedRadiusSq)
                                {
                                    // 将在 t 秒后碰撞
                                    double urgency = (timeHorizon - t); // 越快撞上，惩罚越大
                                    maxCollisionPenalty = Math.Max(maxCollisionPenalty, 1000 * urgency);
                                }
                            }
                        }
                    }
                }

                penalty += maxCollisionPenalty;

                if (penalty < minPenalty)
                {
                    minPenalty = penalty;
                    bestVel = v;
                }
            }

            return bestVel;
        }
    }

    public class RuntimeSkill
    {
        public SkillDef Definition;
        public double CurrentCooldown;

        public RuntimeSkill(SkillDef def)
        {
            Definition = def;
            CurrentCooldown = 0;
        }

        public void Update(double dt)
        {
            if (CurrentCooldown > 0)
                CurrentCooldown = Math.Max(0, CurrentCooldown - dt);
        }

        public bool IsReady => CurrentCooldown <= 0;
    }

    public class LootItem : Entity
    {
        public string Type;
        public int Value;
        public double LifeTime = 15.0;
        public bool IsCollected = false;

        public LootItem(double x, double y, string type, int value)
            : base(x, y, TeamType.Neutral, 1)
        {
            Type = type;
            Value = value;
            Width = 20;
            Height = 20;
        }

        public override void Update(double dt, GameEngine engine)
        {
            LifeTime -= dt;
            if (LifeTime <= 0 || IsCollected)
                HP = 0;
        }
    }

    public class VisualEffect : Entity
    {
        public Animator Animator;
        public bool IsLoop = false;
        public Entity? FollowTarget;
        private double _safetyTimer = 0;

        public VisualEffect()
            : base(0, 0, TeamType.Neutral, 1) { }

        public VisualEffect(
            double x,
            double y,
            string effectName,
            GameEngine engine,
            bool loop = false,
            Entity? follow = null
        )
            : base(x, y, TeamType.Neutral, 1)
        {
            Init(x, y, effectName, engine, loop, follow);
        }

        public void Init(
            double x,
            double y,
            string effectName,
            GameEngine engine,
            bool loop = false,
            Entity? follow = null
        )
        {
            X = x;
            Y = y;
            HP = 1;
            _safetyTimer = 0;
            ActiveBuffs.Clear();
            _components.Clear();
            AddComponent(new HealthComponent(1));

            Animator = AssetManager.CreateEffectAnimator(effectName);
            Animator.Play(effectName);
            IsLoop = loop;
            FollowTarget = follow;
            Width = 60;
            Height = 60;
            if (
                engine.EffectConfigs.TryGetValue(effectName, out var config)
                && config.Light != null
            )
            {
                var lightDef = config.Light;
                var color = (System.Windows.Media.Color)
                    System.Windows.Media.ColorConverter.ConvertFromString(lightDef.Color);
                AddComponent(
                    new LightSourceComponent(
                        color,
                        lightDef.Radius,
                        lightDef.FlickerIntensity,
                        lightDef.FlickerFrequency
                    )
                );
            }
        }

        public override void Update(double dt, GameEngine engine)
        {
            Animator.Update(dt);
            _safetyTimer += dt;
            if (FollowTarget != null)
            {
                if (FollowTarget.HP <= 0)
                {
                    HP = 0;
                    return;
                }
                X = FollowTarget.X;
                Y = FollowTarget.Y;
            }

            if (!IsLoop && Animator.IsFinished)
            {
                HP = 0;
            }
            else if (!IsLoop && _safetyTimer > 5.0)
            {
                HP = 0;
            }
        }
    }

    public class StatModifier
    {
        public string Stat;
        public double Value;

        public StatModifier(string stat, double value)
        {
            Stat = stat;
            Value = value;
        }
    }

    public class Buff
    {
        public double Duration;
        public double TimeRemaining;
        public List<StatModifier> Modifiers = new List<StatModifier>();

        public Buff(double duration)
        {
            Duration = duration;
            TimeRemaining = duration;
        }

        public virtual void OnApply(Entity target) { }

        public virtual void OnRemove(Entity target) { }

        public virtual void OnTick(Entity target, double dt)
        {
            TimeRemaining -= dt;
        }
    }

    public class SlowEffect : Buff
    {
        public SlowEffect(double duration)
            : base(duration)
        {
            Modifiers.Add(new StatModifier("Speed", -0.5));
        }
    }

    public abstract class Entity
    {
        public double X;
        public double Y;
        public TeamType Team = TeamType.Neutral;
        public double Width,
            Height;
        public bool IsSelected = false;
        public double HitFlashTime = 0;
        public bool IsSolid = true;

        public List<Buff> ActiveBuffs = new List<Buff>();

        public void AddBuff(Buff buff)
        {
            ActiveBuffs.Add(buff);
            buff.OnApply(this);
        }

        public void RemoveBuff(Buff buff)
        {
            if (ActiveBuffs.Contains(buff))
            {
                buff.OnRemove(this);
                ActiveBuffs.Remove(buff);
            }
        }

        public double GetStatMultiplier(string stat)
        {
            double mod = 0;
            foreach (var b in ActiveBuffs)
                foreach (var m in b.Modifiers)
                    if (m.Stat == stat)
                        mod += m.Value;
            return Math.Max(0, 1.0 + mod);
        }

        protected Dictionary<Type, IComponent> _components = new Dictionary<Type, IComponent>();

        public T GetComponent<T>()
            where T : class, IComponent
        {
            return _components.TryGetValue(typeof(T), out var c) ? (T)c : null;
        }

        public void AddComponent(IComponent c)
        {
            _components[c.GetType()] = c;
        }

        // [兼容性封装] 仍然保留 HP 属性，但底层指向 HealthComponent
        public double HP
        {
            get => GetComponent<HealthComponent>()?.HP ?? 0;
            set
            {
                var c = GetComponent<HealthComponent>();
                if (c != null)
                    c.HP = value;
            }
        }
        public double MaxHP
        {
            get => GetComponent<HealthComponent>()?.MaxHP ?? 0;
            set
            {
                var c = GetComponent<HealthComponent>();
                if (c != null)
                    c.MaxHP = value;
            }
        }
        public double DeathTimer
        {
            get => GetComponent<HealthComponent>()?.DeathTimer ?? 0;
            set
            {
                var c = GetComponent<HealthComponent>();
                if (c != null)
                    c.DeathTimer = value;
            }
        }

        public double SightRange
        {
            get => GetComponent<VisionComponent>()?.SightRadius ?? 0;
            set
            {
                var c = GetComponent<VisionComponent>();
                if (c != null)
                    c.SightRadius = value;
            }
        }

        public Entity(double x, double y, TeamType team, double hp)
        {
            X = x;
            Y = y;
            Team = team;
            Width = 40;
            Height = 40;
            AddComponent(new HealthComponent(hp));
        }

        public double Rotation { get; set; }
        public Guid EditorId { get; set; } = Guid.NewGuid();

        public bool Contains(PointD p)
        {
            // 1. 计算鼠标点击点相对于单位脚底(X,Y)的偏移量
            double dx = p.X - X;
            double dy = p.Y - Y;

            // 2. 将这个世界坐标偏移量，投影回等距视角(ISO)坐标系
            // 公式与 GameEngine.WorldToIso 保持一致：
            // isoX 对应屏幕的水平方向 (宽)
            // isoY 对应屏幕的垂直方向 (高)
            double isoX = dx - dy;
            double isoY = (dx + dy) * 0.5;

            // 3. 设定判定的视觉高度
            // 建议：不要写死 2.5 倍，最好读取配置。
            // 如果没有配置，对于建筑(Building)给大一点，单位给小一点
            double visualHeight = Width * 2.0; // 默认高度系数
            if (this is Building)
                visualHeight = Width * 2.5; // 建筑通常更高

            // 稍微放宽一点水平判定范围 (1.2倍宽度)，方便点击
            double hitWidth = Width * 1.2;

            // 4. 进行判定 (现在是在“屏幕空间”做判定，非常直观)

            // 水平判定：鼠标是否在中心线左右两侧范围内
            bool xIn = Math.Abs(isoX) <= hitWidth / 2;

            // 垂直判定：
            // isoY <= 10 : 允许点击脚底稍微往下一点点的地方 (容错)
            // isoY >= -visualHeight : 点击位置不能超过头顶
            bool yIn = isoY <= 20 && isoY >= -visualHeight;

            return xIn && yIn;
        }

        public virtual void TakeDamage(double amount, GameEngine engine)
        {
            double preHp = HP;
            GetComponent<HealthComponent>()?.TakeDamage(amount);
            HitFlashTime = 0.1;

            if (preHp > 0 && HP <= 0)
            {
                EventBus.Publish(new EntityDiedEvent(this, null));
            }

            // Blood Splatter Emitter
            var bloodConfig = new EmitterConfig
            {
                IsOneShot = true,
                OneShotCount = (int)(amount / 5) + 5,
                ParticleLifeMin = 2.5,
                ParticleLifeMax = 4.0,
                AngleMin = -135,
                AngleMax = -45,
                SpeedMin = 50,
                SpeedMax = 150,
                Gravity = 400,
                StartColor = System.Windows.Media.Colors.Red,
                EndColor = System.Windows.Media.Color.FromArgb(255, 80, 0, 0),
                StartScaleMin = 1.5,
                StartScaleMax = 2.5,
                EndScaleMin = 2.0,
                EndScaleMax = 3.0,
                StaysOnGround = true,
            };
            engine.AddEmitter(new ParticleEmitter(X, Y, bloodConfig, engine));
        }

        public virtual void Update(double dt, GameEngine engine)
        {
            for (int i = ActiveBuffs.Count - 1; i >= 0; i--)
            {
                var b = ActiveBuffs[i];
                b.OnTick(this, dt);
                if (b.TimeRemaining <= 0)
                {
                    RemoveBuff(b);
                }
            }
            foreach (var c in _components.Values)
                c.Update(dt, engine);
        }
    }

    public class Obstacle : Entity
    {
        public string Type;
        public int Variant;
        public Animator? Animator;
        public double RenderOffsetY = 0;

        public Obstacle(
            double x,
            double y,
            string typeOrKey,
            string customImagePath = "",
            double w = 0,
            double h = 0
        )
            : base(x, y, TeamType.Neutral, 9999)
        {
            Type = typeOrKey;
            Variant = new Random().Next(0, 5);
            IsSolid = true;

            // 1. 尝试直接加载自定义图
            if (!string.IsNullOrEmpty(customImagePath))
            {
                AssetManager.EnsureImage(typeOrKey, customImagePath);
                Width = w > 0 ? w : 50;
                Height = h > 0 ? h : 50;
            }
            // 2. 尝试从定义中加载
            else if (GameEngine.ObstacleInfo.ContainsKey(typeOrKey))
            {
                var def = GameEngine.ObstacleInfo[typeOrKey];
                Width = def.Width;
                Height = def.Height;
                RenderOffsetY = def.OffsetY;
                this.IsSolid = def.HasCollision;

                // [修正] 使用统一工厂方法创建动画，不再直接访问私有字典
                Animator = AssetManager.CreateAnimator(typeOrKey);
                // 只要 Animator 不为空，就播放默认 Idle（CreateAnimator 内部会处理兜底静态图）
                Animator?.Play("Idle");
            }
            // 3. 兜底
            else
            {
                Width = 50;
                Height = 50;
            }
        }

        public override void Update(double dt, GameEngine engine)
        {
            Animator?.Update(dt);
        }

        public void ApplyDefinition(ObstacleDef def)
        {
            if (def == null)
                return;

            Width = def.Width;
            Height = def.Height;
            RenderOffsetY = def.OffsetY;
            IsSolid = def.HasCollision;
            Animator = AssetManager.CreateAnimator(def.Key) ?? Animator;
            Animator?.Play("Idle");
        }
    }

    public class Building : Entity
    {
        public string Id;
        public string Name;
        public Animator Animator;
        private bool _isSmoking = false;
        public double RenderOffsetY = 0;
        public List<string> Produces = new List<string>();

        public Building(double x, double y, TeamType team, BuildingInfo info)
            : base(x, y, team, info.Hp)
        {
            Id = info.Id;
            Name = info.Name;
            Width = info.Width;
            Height = info.Height;
            IsSolid = true;
            Animator = AssetManager.CreateBuildingAnimator(info.Id);
            RenderOffsetY = info.OffsetY;

            // [Fix] 建筑物默认添加视野组件，否则无法驱散迷雾
            AddComponent(new VisionComponent(600));

            if (info.Damage > 0)
            {
                AddComponent(
                    new AttackComponent(info.Damage, info.Range, info.CD, AttackType.Pierce)
                );
            }
            if (info.Light != null)
            {
                var color = (System.Windows.Media.Color)
                    System.Windows.Media.ColorConverter.ConvertFromString(info.Light.Color);
                AddComponent(
                    new LightSourceComponent(
                        color,
                        info.Light.Radius,
                        info.Light.FlickerIntensity,
                        info.Light.FlickerFrequency
                    )
                );
            }
        }

        public void ApplyInfo(BuildingInfo info)
        {
            if (info == null)
                return;

            Width = info.Width;
            Height = info.Height;
            RenderOffsetY = info.OffsetY;
            Name = info.Name;
            MaxHP = info.Hp;
            HP = Math.Min(HP, MaxHP);
            Animator = AssetManager.CreateBuildingAnimator(info.Id) ?? Animator;
        }

        public double Damage =>
            (GetComponent<AttackComponent>()?.Damage ?? 0) * GetStatMultiplier("Damage");
        public double Range => GetComponent<AttackComponent>()?.Range ?? 0;
        public double MaxCD => GetComponent<AttackComponent>()?.MaxCD ?? 0;
        public double CurrentCD
        {
            get => GetComponent<AttackComponent>()?.CurrentCD ?? 0;
            set
            {
                var c = GetComponent<AttackComponent>();
                if (c != null)
                    c.CurrentCD = value;
            }
        }

        public override void Update(double dt, GameEngine engine)
        {
            base.Update(dt, engine);
            if (HitFlashTime > 0)
                HitFlashTime -= dt;
            Animator?.Update(dt);

            // Smoking when low HP
            if (HP > 0 && HP / MaxHP < 0.3 && !_isSmoking)
            {
                var smokeConfig = new EmitterConfig
                {
                    EmitRate = 15,
                    ParticleLifeMin = 1.5,
                    ParticleLifeMax = 2.5,
                    AngleMin = -110,
                    AngleMax = -70,
                    SpeedMin = 20,
                    SpeedMax = 40,
                    StartColor = System.Windows.Media.Color.FromArgb(150, 40, 40, 40),
                    EndColor = System.Windows.Media.Color.FromArgb(0, 80, 80, 80),
                    StartScaleMin = 2,
                    StartScaleMax = 4,
                    EndScaleMin = 8,
                    EndScaleMax = 12,
                };
                engine.AddEmitter(
                    new ParticleEmitter(X, Y - Height * 0.6, smokeConfig, engine, this)
                );
                _isSmoking = true;
            }
        }
    }

    public class Unit : Entity
    {
        private static Random _rnd = new Random();
        private Node<Unit> _aiRoot;

        public double CastLockTime = 0;
        public Animator Animator;
        public List<RuntimeSkill> SkillBook = new List<RuntimeSkill>();
        public double Facing = 1;

        public UnitState State = UnitState.Move;
        public Entity? Target;

        private PointD? _commandTargetPos;
        private Queue<UnitCommand> _pendingCommands = new Queue<UnitCommand>();
        private Queue<PointD> _currentPath = new Queue<PointD>();

        // [Logic Throttling]
        private double _updateTimer;
        private const double LogicInterval = 0.2;
        private List<Entity> _neighborCache = new List<Entity>(); // [Optimization] Cache for steering

        // [核心修复] 明确的布尔变量，标记是否按了S键
        private bool _isManuallyStopped = false;

        // [核心修复] 标记当前命令是否为攻击移动
        private bool _isAttackMoveCommand = false;

        private double _reactionTimer = 0;
        private double _footprintTimer = 0;
        private const double FootprintInterval = 0.3;

        public bool HasCommand => _commandTargetPos.HasValue;
        public PointD? CommandTargetPos => _commandTargetPos;
        public bool IsAttackMove => _isAttackMoveCommand;
        public IEnumerable<UnitCommand> PendingCommands => _pendingCommands;
        public bool IsManuallyStopped => _isManuallyStopped;

        public string Key;
        public UnitStats Stats;

        // Component Wrappers
        public double Mana
        {
            get => GetComponent<ManaComponent>()?.Mana ?? 0;
            set
            {
                var c = GetComponent<ManaComponent>();
                if (c != null)
                    c.Mana = value;
            }
        }
        public double Speed =>
            (GetComponent<MovementComponent>()?.Speed ?? 0) * GetStatMultiplier("Speed");
        public PointD Velocity
        {
            get => GetComponent<MovementComponent>()?.Velocity ?? new PointD(0, 0);
            set
            {
                var c = GetComponent<MovementComponent>();
                if (c != null)
                    c.Velocity = value;
            }
        }
        public double Damage =>
            (GetComponent<AttackComponent>()?.Damage ?? 0) * GetStatMultiplier("Damage");
        public double Range => GetComponent<AttackComponent>()?.Range ?? 0;
        public double CurrentCD
        {
            get => GetComponent<AttackComponent>()?.CurrentCD ?? 0;
            set
            {
                var c = GetComponent<AttackComponent>();
                if (c != null)
                    c.CurrentCD = value;
            }
        }

        public Unit(double x, double y, TeamType team, string key, UnitStats Stats)
            : base(x, y, team, Stats.HP)
        {
            this.Key = key;
            this.Stats = Stats;
            Width = Stats.Width;
            Height = Stats.Height;
            Animator = AssetManager.CreateAnimator(key);
            foreach (var def in Stats.SkillDefs)
                SkillBook.Add(new RuntimeSkill(def));
            IsSolid = true;
            Facing = (team == TeamType.Orc) ? -1 : 1;

            AddComponent(new MovementComponent(Stats.Speed));
            AddComponent(new AttackComponent(Stats.Dmg, Stats.Range, Stats.CD, Stats.AtkType));
            AddComponent(new ManaComponent(Stats.MaxMana, Stats.ManaRegen));
            AddComponent(new VisionComponent(Stats.SightRadius));

            // [Logic Throttling] Randomize start time to prevent CPU spikes
            _updateTimer = new Random().NextDouble() * LogicInterval;

            // [Optimization] Use Flyweight pattern for Behavior Tree
            // All units of the same type share one tree instance.
            _aiRoot = BehaviorTreeFactory.GetTreeForUnit(this.Key);

            // [New] Subscribe to animator events
            if (Animator != null)
            {
                Animator.OnFrameEvent += HandleAnimationEvent;
            }
        }

        public void ApplyStats(UnitStats stats)
        {
            Stats = stats;
            Width = stats.Width;
            Height = stats.Height;

            MaxHP = stats.HP;
            HP = Math.Min(HP, MaxHP);

            var movement = GetComponent<MovementComponent>();
            if (movement != null)
                movement.Speed = stats.Speed;

            var attack = GetComponent<AttackComponent>();
            if (attack != null)
            {
                attack.Damage = stats.Dmg;
                attack.Range = stats.Range;
                attack.MaxCD = stats.CD;
                attack.Type = stats.AtkType;
            }

            var mana = GetComponent<ManaComponent>();
            if (mana != null)
            {
                mana.MaxMana = stats.MaxMana;
                mana.Regen = stats.ManaRegen;
                mana.Mana = Math.Min(mana.Mana, mana.MaxMana);
            }

            var vision = GetComponent<VisionComponent>();
            if (vision != null)
                vision.SightRadius = stats.SightRadius;

            SkillBook.Clear();
            foreach (var def in stats.SkillDefs)
                SkillBook.Add(new RuntimeSkill(def));
        }

        private void HandleAnimationEvent(string eventName)
        {
            if (eventName == "Hit" && State == UnitState.Attack)
            {
                // This is where we would get the GameEngine instance to call ExecuteDamage
                // Since we don't have it here, we need to pass it in Update or have it globally accessible.
                // For now, we'll assume we can get it.
                var engine = GameServices.Get<GameEngine>();
                if (engine != null)
                {
                    ExecuteDamage(engine);
                }
            }
        }

        public override void Update(double dt, GameEngine engine)
        {
            if (HP <= 0)
            {
                if (State != UnitState.Die)
                {
                    State = UnitState.Die;
                    Animator?.Play("Die");
                    IsSolid = false;
                }
                if (Animator != null && !Animator.IsFinished)
                    Animator.Update(dt);
                return;
            }
            if (CastLockTime > 0)
            {
                CastLockTime -= dt;
                Animator?.Update(dt);
                return;
            }
            if (HitFlashTime > 0)
                HitFlashTime -= dt;
            foreach (var s in SkillBook)
                s.Update(dt);

            base.Update(dt, engine);

            if (!this.HasCommand && _pendingCommands.Count > 0)
            {
                var nextCmd = _pendingCommands.Dequeue();
                nextCmd.Execute(this, engine);
            }

            // [Logic Throttling] AI & Decision Making
            _updateTimer -= dt;
            if (_updateTimer <= 0)
            {
                // Execute AI with LogicInterval as delta time to ensure steering forces scale appropriately
                _aiRoot.Evaluate(this, engine, LogicInterval);
                _updateTimer = LogicInterval;
            }

            // [Fix] Hard Collision / Separation for Idle units
            if (CurrentCD > 0)
            {
                Velocity = new PointD(0, 0);
            }
            else
            {
                // If not on cooldown, perform normal collision and movement.
                // [Fix] The RVO solver is now the primary mechanism for collision avoidance.
                // The hard push from ResolveOverlaps was conflicting with RVO's calculated velocity,
                // causing jittery movement and oscillations. Disabling it for moving units.
                // ResolveOverlaps(engine, dt);

                if (Velocity.X != 0 || Velocity.Y != 0)
                {
                    X += Velocity.X * dt;
                    Y += Velocity.Y * dt;

                    if (Velocity.X > 0.1)
                        Facing = 1;
                    else if (Velocity.X < -0.1)
                        Facing = -1;
                }
            }
            Y = Math.Max(20, Math.Min(engine.MapDepth - 20, Y));
            X = Math.Max(0, Math.Min(engine.WorldWidth, X));

            Animator?.Update(dt);

            UpdateFootprints(dt, engine);
        }

        private void ResolveOverlaps(GameEngine engine, double dt)
        {
            // Use a smaller range for collision check to save perf
            engine.GetNearbyEntitiesNonAlloc(this, _neighborCache, 1);

            foreach (var e in _neighborCache)
            {
                if (e == this || e.HP <= 0 || !e.IsSolid)
                    continue;

                double dx = X - e.X;
                double dy = Y - e.Y;
                double distSq = dx * dx + dy * dy;

                // Use a slightly smaller radius for hard collision to allow some overlap (soft body feel)
                double r = (Width + e.Width) * 0.4;
                double minSq = r * r;

                if (distSq < minSq)
                {
                    // Handle zero distance
                    if (distSq < 0.001)
                    {
                        dx = (_rnd.NextDouble() - 0.5) * 0.1;
                        dy = (_rnd.NextDouble() - 0.5) * 0.1;
                        distSq = dx * dx + dy * dy;
                        if (distSq == 0)
                        {
                            dx = 0.1;
                            distSq = 0.01;
                        }
                    }

                    double dist = Math.Sqrt(distSq);
                    double overlap = r - dist;

                    // Push out directly (Hard Collision)
                    double pushFactor = 40.0 * dt; // [Fix] Increased push factor significantly
                    X += (dx / dist) * overlap * pushFactor;
                    Y += (dy / dist) * overlap * pushFactor;
                }
            }
        }

        public void OrderMove(double x, double y, GameEngine engine)
        {
            // [Fix] Prevent accepting a move command while on attack cooldown.
            // This stops the AI from re-issuing a move command right after an attack, which causes the "walk-idle-walk" stutter.
            if (CurrentCD > 0)
                return;

            Target = null;
            _commandTargetPos = new PointD(x, y);
            _isAttackMoveCommand = false;
            _isManuallyStopped = false; // 解除停止
            State = UnitState.Move;
            CalculatePath(engine);
        }

        public void OrderAttackMove(double x, double y, GameEngine engine)
        {
            // [Fix] Also apply the cooldown check to AttackMove commands.
            if (CurrentCD > 0)
                return;

            Target = null;
            _commandTargetPos = new PointD(x, y);
            _isAttackMoveCommand = true; // 标记为攻击移动
            _isManuallyStopped = false; // 解除停止
            State = UnitState.AttackMove;
            CalculatePath(engine);
        }

        public void OrderAttack(Entity target)
        {
            Target = target;
            _commandTargetPos = null;
            _isManuallyStopped = false;
            _currentPath.Clear();
        }

        public void OrderStop()
        {
            Target = null;
            _commandTargetPos = null;
            _currentPath.Clear();
            _isManuallyStopped = true; // 明确标记为手动停止
            State = UnitState.Idle;
            Velocity = new PointD(0, 0);
        }

        public void IssueCommand(UnitCommand cmd, GameEngine engine, bool queue = false)
        {
            if (queue)
            {
                // 如果单位正在执行指令或队列中已有指令，则将新指令入队
                if (HasCommand || _pendingCommands.Count > 0)
                {
                    _pendingCommands.Enqueue(cmd);
                }
                else // 否则，单位处于空闲状态，立即执行
                {
                    cmd.Execute(this, engine);
                }
            }
            else // 如果不排队，则这是覆盖一切的最高优先级指令
            {
                _pendingCommands.Clear();
                cmd.Execute(this, engine);
            }
        }

        private void CalculatePath(GameEngine engine)
        {
            if (IsAttackMove)
                return; // [优化] 攻击移动使用流场，不需要预计算 A* 路径

            if (_commandTargetPos.HasValue)
            {
                var pathList = engine.Pathfinder.FindPath(
                    new PointD(X, Y),
                    _commandTargetPos.Value
                );
                _currentPath.Clear();
                foreach (var p in pathList)
                    _currentPath.Enqueue(p);
            }
        }

        // [供 ExecuteCommandMove 调用]
        // 返回 true 表示到达终点
        public bool MoveToCommandTarget(GameEngine engine, double dt)
        {
            // [Fix] 如果单位正处于攻击冷却中，则不执行移动指令，防止攻击间隙发生“口吃”移动。
            // 单位会原地等待冷却结束，这是大多数RTS游戏的标准行为。
            if (CurrentCD > 0)
            {
                Velocity = new PointD(0, 0);
                // 确保单位在等待时播放站立动画
                Animator?.Play("Idle");
                return false; // 还未到达目的地，但当前不移动
            }

            if (!_commandTargetPos.HasValue)
                return true;

            Animator?.Play("Move");

            // 路径处理
            double tx,
                ty;
            bool isFinal = false;

            // [优化] 如果是攻击移动，使用流场寻路
            bool useFlowField = IsAttackMove;

            if (useFlowField)
            {
                var flow = engine.Pathfinder.GetFlowField(
                    (int)_commandTargetPos.Value.X,
                    (int)_commandTargetPos.Value.Y
                );

                // [分片寻路] 如果流场还没计算完，保持 Idle 或播放思考动画
                if (!flow.IsComplete)
                {
                    // 可以在这里播放 "Idle" 或 "Thinking" 动画
                    Animator?.Play("Idle");
                    Velocity = new PointD(0, 0);
                    return false;
                }

                var dir = flow.GetDirection(X, Y);

                if (dir.X != 0 || dir.Y != 0)
                {
                    // 沿着流场方向前进 (向前投射一个目标点)
                    tx = X + dir.X * 50;
                    ty = Y + dir.Y * 50;

                    // 检查是否接近最终目标
                    double dGoal = Math.Sqrt(
                        Math.Pow(_commandTargetPos.Value.X - X, 2)
                            + Math.Pow(_commandTargetPos.Value.Y - Y, 2)
                    );
                    if (dGoal < 20.0)
                    {
                        tx = _commandTargetPos.Value.X;
                        ty = _commandTargetPos.Value.Y;
                        isFinal = true;
                    }
                }
                else
                {
                    // 流场无效（如在墙内），回退到直接导航
                    tx = _commandTargetPos.Value.X;
                    ty = _commandTargetPos.Value.Y;
                    isFinal = true;
                }
            }
            else
            {
                // 传统 A* 路径跟随
                if (_currentPath.Count > 0)
                {
                    var wp = _currentPath.Peek();
                    double d = Math.Sqrt(Math.Pow(wp.X - X, 2) + Math.Pow(wp.Y - Y, 2));
                    // [Fix] Reduced threshold to prevent skipping waypoints around corners too early.
                    // A large threshold can cause the unit to think it reached a corner waypoint
                    // while still on the wrong side of an obstacle.
                    double threshold = (_currentPath.Count > 1) ? 20.0 : 5.0;
                    if (d < threshold)
                    {
                        _currentPath.Dequeue();
                        if (_currentPath.Count > 0)
                        {
                            tx = _currentPath.Peek().X;
                            ty = _currentPath.Peek().Y;
                        }
                        else
                        {
                            tx = _commandTargetPos.Value.X;
                            ty = _commandTargetPos.Value.Y;
                            isFinal = true;
                        }
                    }
                    else
                    {
                        tx = wp.X;
                        ty = wp.Y;
                    }
                }
                else
                {
                    tx = _commandTargetPos.Value.X;
                    ty = _commandTargetPos.Value.Y;
                    isFinal = true;
                }
            }

            // 到达判定
            if (isFinal)
            {
                double dGoal = Math.Sqrt(Math.Pow(tx - X, 2) + Math.Pow(ty - Y, 2));
                if (dGoal < 5.0)
                {
                    _commandTargetPos = null;
                    State = UnitState.Idle;
                    Velocity = new PointD(0, 0);
                    // 注意：这里我们不设置 _isManuallyStopped = true
                    // 这样当命令结束时，aiRoot 会自动切换到 LanePushNode 继续推线
                    return true;
                }
            }

            MoveWithSteering(tx, ty, dt, engine);
            return false;
        }

        // [供 FindTargetNode 调用]
        public bool CheckScanForEnemy(GameEngine engine, double dt, bool force = false)
        {
            _reactionTimer -= dt;
            if (_reactionTimer <= 0 || force)
            {
                _reactionTimer = 0.3;
                bool isTargetInvalid = (
                    Target == null || Target.HP <= 0 || (Target is LootItem l && l.IsCollected)
                );
                bool shouldScan = isTargetInvalid || (Target is Building); // 总是尝试找更好的目标(比如从打塔切换到打兵)

                if (shouldScan)
                {
                    FindBestTarget(engine);
                    return Target != null;
                }
            }
            return Target != null && Target.HP > 0;
        }

        public void InvokeMoveWithSteering(double x, double y, double dt, GameEngine engine)
        {
            MoveWithSteering(x, y, dt, engine);
        }

        public void FaceTarget()
        {
            if (Target != null)
            {
                if (Target.X < X)
                    Facing = -1;
                else
                    Facing = 1;
            }
        }

        public void UpdateAttackLogic(GameEngine engine, double dt)
        {
            // The damage is now dealt by the "Hit" animation event.
            // We just need to check when the animation is over to return to Idle.
            bool animFinished = (Animator != null && Animator.IsFinished);
            if (animFinished)
            {
                State = UnitState.Idle;
            }
        }

        public void StartAttack(GameEngine engine)
        {
            State = UnitState.Attack;
            Animator?.Play("Attack");
            CurrentCD = Stats.CD;
            FaceTarget();
        }

        private void UpdateFootprints(double dt, GameEngine engine)
        {
            double vlen = Math.Sqrt(Velocity.X * Velocity.X + Velocity.Y * Velocity.Y);
            if (vlen > 10.0)
            {
                _footprintTimer -= dt;
                if (_footprintTimer <= 0)
                {
                    _footprintTimer = FootprintInterval;

                    int index = engine.GetNewParticleIndex();
                    if (index != -1)
                    {
                        ref var p = ref engine.Particles[index];
                        p.Active = true;
                        p.X = X;
                        p.Y = Y;
                        p.VelX = 0;
                        p.VelY = 0;
                        p.Life = 3.0;
                        p.MaxLife = 3.0;
                        p.StartScale = 0.5;
                        p.EndScale = 0.5;
                        p.StartColor = System.Windows.Media.Color.FromArgb(50, 0, 0, 0);
                        p.EndColor = System.Windows.Media.Colors.Transparent;
                        p.StaysOnGround = true;
                    }
                }
            }
        }

        private void MoveWithSteering(double tx, double ty, double dt, GameEngine engine)
        {
            var moveComp = GetComponent<MovementComponent>();
            if (moveComp == null) return;

            // 1. 基础期望速度（指向目标）
            double dx = tx - X;
            double dy = ty - Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            // 防止在终点附近抖动
            if (dist < 2.0)
            {
                Velocity = new PointD(0, 0);
                return;
            }

            // 归一化并乘以最大速度
            PointD desired = new PointD((dx / dist) * Speed, (dy / dist) * Speed);

            // 获取邻居
            engine.GetNearbyEntitiesNonAlloc(this, _neighborCache, 2);

            // [新增] 预先施加分离力 (Separation Force)
            // 这有助于解决死锁，让单位自然地散开，而不是完全依赖 RVO 的采样
            double sepX = 0, sepY = 0;
            int sepCount = 0;
            double sepRadius = Width * 1.5; // 分离半径
            double sepRadiusSq = sepRadius * sepRadius;

            foreach (var n in _neighborCache)
            {
                if (n == this || n.HP <= 0 || !n.IsSolid) continue;

                double ndx = X - n.X;
                double ndy = Y - n.Y;
                double nd2 = ndx * ndx + ndy * ndy;

                if (nd2 > 0.001 && nd2 < sepRadiusSq)
                {
                    double nd = Math.Sqrt(nd2);
                    // 距离越近，推力越大
                    double force = (sepRadius - nd) / sepRadius;
                    sepX += (ndx / nd) * force;
                    sepY += (ndy / nd) * force;
                    sepCount++;
                }
            }

            if (sepCount > 0)
            {
                // 将分离力混合进期望速度
                // 权重 0.4 表示：寻路占 60%，不让别人占 40%
                double separationStrength = Speed * 2.0;
                desired.X += sepX * separationStrength;
                desired.Y += sepY * separationStrength;

                // 重新归一化以保持速度恒定 (可选，如果不归一化，挤的时候会跑得快一点，通常手感更好)
                double newLen = Math.Sqrt(desired.X * desired.X + desired.Y * desired.Y);
                if (newLen > Speed)
                {
                    desired.X = (desired.X / newLen) * Speed;
                    desired.Y = (desired.Y / newLen) * Speed;
                }
            }

            // 2. 通过 RVO 修正最终速度
            PointD newVel = RVOSolver.CalculateVelocity(this, desired, _neighborCache, dt);

            Velocity = newVel;
        }

        private void FindBestTarget(GameEngine engine)
        {
            double scanRange = 600;
            if (Stats.Type == UnitType.Melee)
                scanRange = 400;
            double scanRangeSq = scanRange * scanRange;
            Entity bestUnit = null;
            double bestUnitDistSq = double.MaxValue;
            Entity bestTower = null;
            double bestTowerDistSq = double.MaxValue;
            Entity bestBuilding = null;
            double bestBuildingDistSq = double.MaxValue;

            if (Stats.Type == UnitType.Support)
            {
                double bestFriendDist = double.MaxValue;
                Target = null;
                foreach (var f in engine.Entities)
                {
                    if (f.Team != Team || f == this || f.HP >= f.MaxHP || f is Obstacle)
                        continue;

                    double d2 = GetDistSq(f);
                    if (d2 < scanRangeSq && d2 < bestFriendDist)
                    {
                        bestFriendDist = d2;
                        Target = f;
                    }
                }
                return;
            }

            foreach (var e in engine.Entities)
            {
                if (e.Team == Team || e.HP <= 0 || e is Obstacle || e is LootItem)
                    continue;
                if (Team == TeamType.Human && !engine.IsVisibleToPlayer(e))
                    continue;
                double distSq = GetDistSq(e);
                if (distSq > scanRangeSq)
                    continue;

                if (e is Unit)
                {
                    if (distSq < bestUnitDistSq)
                    {
                        bestUnitDistSq = distSq;
                        bestUnit = e;
                    }
                }
                else if (e is Building b)
                {
                    if (b.Damage > 0)
                    {
                        if (distSq < bestTowerDistSq)
                        {
                            bestTowerDistSq = distSq;
                            bestTower = b;
                        }
                    }
                    else
                    {
                        if (distSq < bestBuildingDistSq)
                        {
                            bestBuildingDistSq = distSq;
                            bestBuilding = e;
                        }
                    }
                }
            }

            if (bestUnit != null)
            {
                Target = bestUnit;
            } // 发现单位，切换目标 (即使正在打塔)
            else if (bestTower != null)
                Target = bestTower;
            else if (bestBuilding != null)
                Target = bestBuilding;
            else if (Target != null && (Target.HP <= 0 || GetDistSq(Target) > scanRangeSq))
                Target = null;
        }

        private double GetDistSq(Entity e)
        {
            return (e.X - X) * (e.X - X) + (e.Y - Y) * (e.Y - Y);
        }

        private void ExecuteDamage(GameEngine engine)
        {
            if (Target == null || Target.HP <= 0)
                return;
            if (
                Stats.Type == UnitType.Ranged
                || Stats.Type == UnitType.Siege
                || Stats.Type == UnitType.Support
            )
            {
                double myLaunchZ = this.Height * 0.6;
                double targetHitZ = Target.Height * 0.5;
                var p = engine.ProjectilePool.Get();
                p.Init(X, Y, myLaunchZ, Target, targetHitZ, Damage, Stats.Type, engine, Key);
                engine.Projectiles.Add(p);
                if (Stats.DoubleShot)
                {
                    var p2 = engine.ProjectilePool.Get();
                    p2.Init(
                        X + 5,
                        Y + 5,
                        myLaunchZ,
                        Target,
                        targetHitZ,
                        Damage,
                        Stats.Type,
                        engine,
                        Key
                    );
                    engine.Projectiles.Add(p2);
                }
            }
            else
            {
                double mul = GameEngine.GetDamageMultiplier(
                    Stats.AtkType,
                    (Target as Unit)?.Stats.DefType ?? ArmorType.Fortified
                );
                double val = Damage * mul;
                bool crit = (Stats.CritChance > 0 && new Random().NextDouble() < Stats.CritChance);
                if (crit)
                {
                    val *= 1.5;
                    engine.ShakeIntensity = 3.0;
                    engine.TriggerHitStop(); // CRITICAL HIT STOP
                }
                Target.TakeDamage(val, engine);
                engine.AddFloater(
                    crit ? $"-{val:0}!" : $"-{val:0}",
                    Target.X,
                    Target.Y,
                    crit ? "OrangeRed" : "White",
                    crit ? 32 : 20,
                    0,
                    -4.0
                );
                if (Target.HP <= 0 && Target.Team == TeamType.Orc)
                {
                    engine.Gold += 15;
                    engine.AddFloater("+15g", X, Y - 40, "Gold");
                    engine.TriggerHitStop(0.08, 0.05);
                } // KILL HIT STOP
            }
        }

        public override void TakeDamage(double amount, GameEngine engine)
        {
            if (Stats.BlockChance > 0 && new Random().NextDouble() < Stats.BlockChance)
            {
                engine.AddFloater("Block!", X, Y - 40, "Gray");
                return;
            }

            base.TakeDamage(amount, engine);

            if (HP <= 0 && HP + amount > 0) // Just died
            {
                EventBus.Publish(new UnitDiedEvent(this, null));
            }
        }
    }

    public class Projectile
    {
        public double X,
            Y,
            TX,
            TY,
            StartX,
            StartY,
            Damage,
            Speed; // Projectile maintains its own X,Y for simplicity
        public Entity? Target;
        public UnitType Type;
        public bool Active = true;
        public LightSourceComponent Light { get; private set; }
        public Animator? Animator;
        public double Rotation = 0;
        public double Height = 0,
            ArcHeight = 150,
            Progress = 0,
            DistTotal,
            StartZ,
            TargetZ;
        public bool IsHoming = true;
        public string SourceKey;

        public Projectile() { }

        public Projectile(
            double x,
            double y,
            double startZ,
            Entity? target,
            double targetZ,
            double dmg,
            UnitType type,
            string sourceKey = ""
        )
        {
            // This constructor is not used with the pool, Init is called instead.
        }

        public void Init(
            double x,
            double y,
            double startZ,
            Entity? target,
            double targetZ,
            double dmg,
            UnitType type,
            GameEngine engine,
            string sourceKey
        )
        {
            X = x;
            Y = y;
            StartX = x;
            StartY = y;
            StartZ = startZ;
            TargetZ = targetZ;
            Target = target;
            Damage = dmg;
            Type = type;
            Active = true;
            Progress = 0;
            SourceKey = sourceKey;

            if (target != null)
            {
                TX = target.X;
                TY = target.Y;
            }
            else
            {
                TX = x;
                TY = y;
            }

            if (type == UnitType.Siege)
            {
                Speed = 300;
                ArcHeight = 250;
                IsHoming = false;
            }
            else
            {
                Speed = 600;
                ArcHeight = 100;
                IsHoming = true;
            }

            DistTotal = Math.Sqrt(Math.Pow(TX - X, 2) + Math.Pow(TY - Y, 2));
            if (DistTotal < 1)
                DistTotal = 1;

            if (
                !string.IsNullOrEmpty(sourceKey)
                && engine.BaseUnitStats.TryGetValue(sourceKey, out var stats)
            )
            {
                Animator = AssetManager.CreateProjectileAnimator(sourceKey);
                if (stats.ProjectileConfig?.Light != null)
                {
                    var lightDef = stats.ProjectileConfig.Light;
                    var color = (System.Windows.Media.Color)
                        System.Windows.Media.ColorConverter.ConvertFromString(lightDef.Color);
                    Light = new LightSourceComponent(
                        color,
                        lightDef.Radius,
                        lightDef.FlickerIntensity,
                        lightDef.FlickerFrequency
                    );
                }
            }
            else
                Animator = null;

            Height = StartZ;
        }

        public void Update(double dt, GameEngine engine)
        {
            if (!Active)
                return;

            if (IsHoming && Target != null)
            {
                TX = Target.X;
                TY = Target.Y;
                DistTotal = Math.Sqrt(Math.Pow(TX - StartX, 2) + Math.Pow(TY - StartY, 2));
            }

            double step = (Speed * dt) / Math.Max(1, DistTotal);
            Progress += step;

            if (Progress >= 1.0)
            {
                Progress = 1.0;
                Hit(engine);
                return;
            }

            X = StartX + (TX - StartX) * Progress;
            Y = StartY + (TY - StartY) * Progress;
            double linearHeight = StartZ + (TargetZ - StartZ) * Progress;
            Height = linearHeight + Math.Sin(Progress * Math.PI) * ArcHeight;

            double dx = TX - StartX;
            double dy = TY - StartY;
            Rotation = Math.Atan2(dy, dx) * 180 / Math.PI;
            Animator?.Update(dt);
        }

        private void Hit(GameEngine engine)
        {
            Active = false;
            EventBus.Publish(new ProjectileHitEvent(this, Target));

            List<Entity> hitTargets = new List<Entity>();

            if (Type == UnitType.Siege)
            {
                TeamType targetTeam = Target?.Team ?? TeamType.Orc;
                var enemies = engine.FindEntitiesInRange(X, Y, 120, targetTeam);
                hitTargets.AddRange(enemies);

                // Wood splinter effect for arrows hitting non-heavy units
                if (
                    SourceKey == "archer"
                    && Target is Unit u
                    && u.Stats.DefType != ArmorType.Heavy
                    && u.Stats.DefType != ArmorType.Fortified
                )
                {
                    var woodConfig = new EmitterConfig
                    {
                        IsOneShot = true,
                        OneShotCount = 10,
                        ParticleLifeMin = 0.3,
                        ParticleLifeMax = 0.6,
                        AngleMin = 0,
                        AngleMax = 360,
                        SpeedMin = 80,
                        SpeedMax = 200,
                        Gravity = 300,
                        StartColor = System.Windows.Media.Color.FromRgb(180, 140, 100),
                        EndColor = System.Windows.Media.Color.FromRgb(100, 80, 60),
                        StartScaleMin = 0.5,
                        StartScaleMax = 1.5,
                        EndScaleMin = 0.2,
                        EndScaleMax = 0.5,
                        AngularVelocityMin = -360,
                        AngularVelocityMax = 360,
                    };
                    engine.AddEmitter(new ParticleEmitter(X, Y, woodConfig, engine));
                }
                var boom = engine.VisualEffectPool.Get();
                boom.Init(X, Y, "SuperExplosion", engine);
                boom.Width = 80;
                boom.Height = 80;
                engine.VisualEffects.Add(boom);
                engine.ShakeIntensity = 10.0;
            }
            else
            {
                if (Target != null && Target.HP > 0)
                    hitTargets.Add(Target);
                if (Target != null)
                {
                    var vfx = engine.VisualEffectPool.Get();
                    vfx.Init(Target.X, Target.Y, "Explosion", engine);
                    vfx.Width = 40;
                    vfx.Height = 40;
                    engine.VisualEffects.Add(vfx);
                }
            }

            foreach (var victim in hitTargets)
            {
                victim.TakeDamage(Damage, engine);

                if (Type == UnitType.Siege && victim is Unit uVictim)
                {
                    double pushDirX = victim.X - X;
                    double pushDirY = victim.Y - Y;
                    double len = Math.Sqrt(pushDirX * pushDirX + pushDirY * pushDirY);
                    if (len > 0.1)
                    {
                        uVictim.X += (pushDirX / len) * 30;
                        uVictim.Y += (pushDirY / len) * 30;
                    }
                }

                if (victim.HP <= 0 && victim.Team == TeamType.Orc)
                {
                    engine.Gold += 10;
                    engine.AddFloater("+10g", victim.X, victim.Y - 40, "Gold");
                }
            }
        }
    }

    public enum ParticleBlendMode
    {
        Normal,
        Additive,
    }

    public class Particle
    {
        public bool Active;
        public double X,
            Y;
        public double VelX,
            VelY;
        public double Gravity;
        public double Life,
            MaxLife;
        public double Rotation,
            AngularVelocity;
        public double Scale,
            StartScale,
            EndScale;
        public System.Windows.Media.Color StartColor,
            EndColor;
        public ParticleBlendMode BlendMode;
        public bool StaysOnGround;
        private bool _onGround;

        public Particle() { }

        public void Init(
            double x,
            double y,
            double velX,
            double velY,
            double life,
            double gravity,
            double startScale,
            double endScale,
            double rotation,
            double angularVelocity,
            System.Windows.Media.Color startColor,
            System.Windows.Media.Color endColor,
            ParticleBlendMode blendMode,
            bool staysOnGround
        )
        {
            X = x;
            Y = y;
            VelX = velX;
            VelY = velY;
            MaxLife = life;
            Life = life;
            Gravity = gravity;
            StartScale = startScale;
            EndScale = endScale;
            Scale = startScale;
            Rotation = rotation;
            AngularVelocity = angularVelocity;
            StartColor = startColor;
            EndColor = endColor;
            BlendMode = blendMode;
            StaysOnGround = staysOnGround;
            _onGround = false;
            Active = true;
        }

        public void Update(double dt)
        {
            if (!Active)
                return;

            Life -= dt;
            if (Life <= 0)
            {
                Active = false;
                return;
            }

            if (!_onGround)
            {
                VelY += Gravity * dt;
                X += VelX * dt;
                Y += VelY * dt;
            }

            // Simplified ground check. A real implementation would use terrain height.
            if (StaysOnGround && Y > 300) // Assume ground is around Y=300 for this example
            {
                _onGround = true;
                Life = Math.Min(Life, 2.0); // Ground particles fade after 2s
            }

            float t = 1.0f - (float)(Life / MaxLife);
            Scale = StartScale + (EndScale - StartScale) * t;
            Rotation += AngularVelocity * dt;
        }

        public System.Windows.Media.Color GetCurrentColor()
        {
            float t = 1.0f - (float)(Life / MaxLife);
            return System.Windows.Media.Color.FromArgb(
                (byte)(StartColor.A + (EndColor.A - StartColor.A) * t),
                (byte)(StartColor.R + (EndColor.R - StartColor.R) * t),
                (byte)(StartColor.G + (EndColor.G - StartColor.G) * t),
                (byte)(StartColor.B + (EndColor.B - StartColor.B) * t)
            );
        }
    }
}
