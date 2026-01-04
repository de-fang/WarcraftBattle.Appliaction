using System;
using System.Collections.Generic;

namespace WarcraftBattle3D.Core
{
    public class RuntimeSkill
    {
        public SkillDef Definition { get; }
        public double CurrentCooldown { get; private set; }

        public RuntimeSkill(SkillDef def)
        {
            Definition = def ?? throw new ArgumentNullException(nameof(def));
        }

        public void Update(double dt)
        {
            if (CurrentCooldown > 0)
            {
                CurrentCooldown = Math.Max(0, CurrentCooldown - dt);
            }
        }

        public void TriggerCooldown()
        {
            CurrentCooldown = Definition.CD;
        }

        public bool IsReady => CurrentCooldown <= 0;
    }

    public class Unit : Entity
    {
        private static readonly Random Rng = new Random();
        private const double PathRepathInterval = 0.5;
        private const double PathTargetRepathDistance = 100.0;
        private const double FlowFieldStepDistance = 50.0;
        private const double FlowFieldGoalThreshold = 20.0;

        public string Key { get; }
        public UnitStats Stats { get; }
        public UnitState State { get; private set; } = UnitState.Idle;
        public Entity Target { get; private set; }
        public bool HasMoveTarget => _hasMoveTarget;

        public double CurrentCD { get; private set; }
        public double Mana { get; private set; }
        public List<RuntimeSkill> SkillBook { get; } = new List<RuntimeSkill>();

        private double _moveTargetX;
        private double _moveTargetY;
        private bool _hasMoveTarget;
        private bool _attackMove;
        private bool _manualStop;
        private List<PointD> _path;
        private int _pathIndex;
        private double _pathTargetX;
        private double _pathTargetY;
        private double _pathRepathTimer;

        public Unit(double x, double y, TeamType team, string key, UnitStats stats)
            : base(x, y, team, stats?.HP ?? 1)
        {
            if (stats == null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            Key = key;
            Stats = stats.Clone();
            Width = stats.Width;
            Height = stats.Height;
            HP = stats.HP;
            MaxHP = stats.HP;
            SightRadius = stats.SightRadius;
            Mana = stats.MaxMana;

            foreach (var skill in Stats.SkillDefs)
            {
                SkillBook.Add(new RuntimeSkill(skill));
            }

            ResetPath();
        }

        public override void Update(GameEngine engine, double dt)
        {
            base.Update(engine, dt);

            if (!IsAlive)
            {
                State = UnitState.Die;
                return;
            }

            if (CurrentCD > 0)
            {
                CurrentCD = Math.Max(0, CurrentCD - dt * 60.0);
            }

            if (Stats.MaxMana > 0)
            {
                Mana = Math.Min(Stats.MaxMana, Mana + Stats.ManaRegen * dt);
            }

            foreach (var skill in SkillBook)
            {
                skill.Update(dt);
            }

            if (_manualStop)
            {
                State = UnitState.Idle;
                return;
            }

            if (Target != null && !Target.IsAlive)
            {
                Target = null;
            }

            if (Target != null)
            {
                UpdateCombat(engine, dt);
                return;
            }

            if (_attackMove)
            {
                AcquireTarget(engine, Stats.SightRadius);
                if (Target != null)
                {
                    UpdateCombat(engine, dt);
                    return;
                }
            }

            if (_hasMoveTarget)
            {
                State = _attackMove ? UnitState.AttackMove : UnitState.Move;
                MoveToward(engine, _moveTargetX, _moveTargetY, dt, true);
            }
            else
            {
                State = UnitState.Idle;
            }
        }

        public void OrderMove(double x, double y)
        {
            _moveTargetX = x;
            _moveTargetY = y;
            _hasMoveTarget = true;
            _attackMove = false;
            _manualStop = false;
            Target = null;
            ResetPath();
        }

        public void OrderAttack(Entity target)
        {
            if (target == null)
            {
                return;
            }

            Target = target;
            _hasMoveTarget = false;
            _attackMove = false;
            _manualStop = false;
            ResetPath();
        }

        public void OrderAttackMove(double x, double y)
        {
            _moveTargetX = x;
            _moveTargetY = y;
            _hasMoveTarget = true;
            _attackMove = true;
            _manualStop = false;
            Target = null;
            ResetPath();
        }

        public void OrderStop()
        {
            _manualStop = true;
            _hasMoveTarget = false;
            _attackMove = false;
            Target = null;
            ResetPath();
        }

        public bool HasMana(double amount)
        {
            return Mana >= amount;
        }

        public void SpendMana(double amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Mana = Math.Max(0, Mana - amount);
        }

        private void UpdateCombat(GameEngine engine, double dt)
        {
            if (Target == null)
            {
                return;
            }

            double range = Stats.Range + (Width + Target.Width) * 0.45;
            double distSq = GetDistSq(Target.X, Target.Y);

            if (distSq <= range * range)
            {
                State = UnitState.Attack;
                TryAttack(engine, Target);
            }
            else
            {
                State = UnitState.Move;
                MoveToward(engine, Target.X, Target.Y, dt, false);
            }
        }

        private void TryAttack(GameEngine engine, Entity target)
        {
            if (CurrentCD > 0)
            {
                return;
            }

            double damage = Stats.Dmg;
            if (Stats.CritChance > 0 && Rng.NextDouble() < Stats.CritChance)
            {
                damage *= 1.5;
            }

            bool isProjectile = Stats.Type == UnitType.Ranged || Stats.Type == UnitType.Siege || Stats.Type == UnitType.Support;
            if (isProjectile)
            {
                if (engine != null)
                {
                    engine.SpawnProjectile(this, target, damage, Stats.AtkType, Stats.Type, 0, false, Key);
                    if (Stats.DoubleShot)
                    {
                        engine.SpawnProjectileAt(
                            X + 5,
                            Y + 5,
                            target,
                            damage,
                            Stats.AtkType,
                            Stats.Type,
                            Team,
                            0,
                            false,
                            Key);
                    }
                }
                else
                {
                    ApplyDamage(target, damage, Stats.AtkType);
                    if (Stats.DoubleShot)
                    {
                        ApplyDamage(target, damage, Stats.AtkType);
                    }
                }
            }
            else
            {
                ApplyDamage(target, damage, Stats.AtkType);
            }
            CurrentCD = Stats.CD;
        }

        private void AcquireTarget(GameEngine engine, double range)
        {
            if (engine?.EntityManager == null)
            {
                return;
            }

            Target = engine.EntityManager.FindClosestEnemy(this, range);
        }

        private void MoveToward(GameEngine engine, double targetX, double targetY, double dt, bool clearMoveTarget)
        {
            bool useFlowField = clearMoveTarget && engine?.Pathfinder != null;
            if (useFlowField)
            {
                var flow = engine.Pathfinder.GetFlowField((int)targetX, (int)targetY);
                if (!flow.IsComplete)
                {
                    return;
                }

                var dir = flow.GetDirection(X, Y);
                double tx;
                double ty;
                bool isFinal = false;

                if (dir.X != 0 || dir.Y != 0)
                {
                    tx = X + dir.X * FlowFieldStepDistance;
                    ty = Y + dir.Y * FlowFieldStepDistance;

                    double distGoalSq = GetDistSq(targetX, targetY);
                    if (distGoalSq < FlowFieldGoalThreshold * FlowFieldGoalThreshold)
                    {
                        tx = targetX;
                        ty = targetY;
                        isFinal = true;
                    }
                }
                else
                {
                    tx = targetX;
                    ty = targetY;
                    isFinal = true;
                }

                bool reached = MoveStep(tx, ty, dt, engine);
                if (isFinal && reached && clearMoveTarget)
                {
                    _hasMoveTarget = false;
                }
                return;
            }

            if (engine?.Pathfinder != null)
            {
                _pathRepathTimer -= dt;
                double targetDeltaSq = GetDistSq(targetX, targetY, _pathTargetX, _pathTargetY);
                bool needsPath = _path == null || _pathIndex >= _path.Count;
                if (needsPath || _pathRepathTimer <= 0 || targetDeltaSq > PathTargetRepathDistance * PathTargetRepathDistance)
                {
                    _path = engine.Pathfinder.FindPath(new PointD(X, Y), new PointD(targetX, targetY));
                    _pathIndex = 0;
                    _pathTargetX = targetX;
                    _pathTargetY = targetY;
                    _pathRepathTimer = PathRepathInterval;
                }

                if (_path != null && _pathIndex < _path.Count)
                {
                    var waypoint = _path[_pathIndex];
                bool reached = MoveStep(waypoint.X, waypoint.Y, dt, engine);
                    if (reached)
                    {
                        _pathIndex++;
                        if (_pathIndex >= _path.Count && clearMoveTarget)
                        {
                            _hasMoveTarget = false;
                        }
                    }
                    return;
                }
            }

            bool arrived = MoveStep(targetX, targetY, dt, engine);
            if (arrived && clearMoveTarget)
            {
                _hasMoveTarget = false;
            }
        }

        private double GetDistSq(double tx, double ty)
        {
            double dx = tx - X;
            double dy = ty - Y;
            return dx * dx + dy * dy;
        }

        private static double GetDistSq(double ax, double ay, double bx, double by)
        {
            double dx = ax - bx;
            double dy = ay - by;
            return dx * dx + dy * dy;
        }

        private bool MoveStep(double targetX, double targetY, double dt, GameEngine engine)
        {
            double dx = targetX - X;
            double dy = targetY - Y;
            double distSq = dx * dx + dy * dy;
            if (distSq < 0.001)
            {
                return true;
            }

            double dist = Math.Sqrt(distSq);
            double step = Stats.Speed * dt;
            if (dist <= step)
            {
                X = targetX;
                Y = targetY;
                return true;
            }

            double dirX = dx / dist;
            double dirY = dy / dist;

            if (engine?.EntityManager != null)
            {
                double sepX = 0;
                double sepY = 0;
                int sepCount = 0;
                double sepRadius = Width * 1.5;
                double sepRadiusSq = sepRadius * sepRadius;

                var neighbors = engine.EntityManager.FindEntitiesInRange(X, Y, sepRadius);
                for (int i = 0; i < neighbors.Count; i++)
                {
                    if (neighbors[i] is Unit neighbor && neighbor != this && neighbor.IsAlive)
                    {
                        double ndx = X - neighbor.X;
                        double ndy = Y - neighbor.Y;
                        double nd2 = ndx * ndx + ndy * ndy;
                        if (nd2 > 0.001 && nd2 < sepRadiusSq)
                        {
                            double nd = Math.Sqrt(nd2);
                            double force = (sepRadius - nd) / sepRadius;
                            sepX += (ndx / nd) * force;
                            sepY += (ndy / nd) * force;
                            sepCount++;
                        }
                    }
                }

                if (sepCount > 0)
                {
                    double separationStrength = Stats.Speed * 2.0;
                    double velX = dirX * Stats.Speed + sepX * separationStrength;
                    double velY = dirY * Stats.Speed + sepY * separationStrength;
                    double velLen = Math.Sqrt(velX * velX + velY * velY);
                    if (velLen > Stats.Speed && velLen > 0.001)
                    {
                        velX = velX / velLen * Stats.Speed;
                        velY = velY / velLen * Stats.Speed;
                    }
                    X += velX * dt;
                    Y += velY * dt;
                    return false;
                }
            }

            X += dirX * step;
            Y += dirY * step;
            return false;
        }

        private void ResetPath()
        {
            if (_path != null)
            {
                _path.Clear();
            }
            _pathIndex = 0;
            _pathTargetX = X;
            _pathTargetY = Y;
            _pathRepathTimer = 0;
        }

        private static void ApplyDamage(Entity target, double amount, AttackType attackType)
        {
            if (target == null || !target.IsAlive)
            {
                return;
            }

            var armor = GetArmorType(target);
            double finalDamage = amount * CombatRules.GetMultiplier(attackType, armor);
            target.TakeDamage(finalDamage);
        }

        private static ArmorType GetArmorType(Entity target)
        {
            if (target is Unit unit)
            {
                return unit.Stats.DefType;
            }

            return ArmorType.Fortified;
        }
    }

    public class Building : Entity
    {
        private static readonly Random Rng = new Random();

        public BuildingInfo Info { get; }
        public double Damage { get; }
        public double Range { get; }
        public double CurrentCD { get; private set; }
        public AttackType AttackType { get; set; } = AttackType.Normal;
        public Entity Target { get; private set; }

        public Building(double x, double y, TeamType team, BuildingInfo info)
            : base(x, y, team, info?.Hp ?? 1)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            Info = info.Clone();
            Width = info.Width;
            Height = info.Height;
            HP = info.Hp;
            MaxHP = info.Hp;
            Damage = info.Damage;
            Range = info.Range;
        }

        public override void Update(GameEngine engine, double dt)
        {
            base.Update(engine, dt);

            if (!IsAlive)
            {
                return;
            }

            if (Damage <= 0)
            {
                return;
            }

            if (CurrentCD > 0)
            {
                CurrentCD = Math.Max(0, CurrentCD - dt * 60.0);
            }

            if (Target != null && !Target.IsAlive)
            {
                Target = null;
            }

            if (Target == null)
            {
                Target = engine.EntityManager?.FindClosestEnemy(this, Range);
            }

            if (Target == null)
            {
                return;
            }

            double distSq = GetDistSq(Target.X, Target.Y);
            if (distSq > Range * Range)
            {
                Target = null;
                return;
            }

            if (CurrentCD <= 0)
            {
                TryAttack(Target);
            }
        }

        private void TryAttack(Entity target)
        {
            double damage = Damage;
            double multiplier = CombatRules.GetMultiplier(AttackType, GetArmorType(target));
            damage *= multiplier;

            target.TakeDamage(damage);
            CurrentCD = Info.CD;
        }

        private double GetDistSq(double tx, double ty)
        {
            double dx = tx - X;
            double dy = ty - Y;
            return dx * dx + dy * dy;
        }

        private static ArmorType GetArmorType(Entity target)
        {
            if (target is Unit unit)
            {
                return unit.Stats.DefType;
            }

            return ArmorType.Fortified;
        }
    }

    public class Obstacle : Entity
    {
        public string Type { get; }
        public ObstacleDef Definition { get; }

        public Obstacle(double x, double y, string type, ObstacleDef def = null)
            : base(x, y, TeamType.Neutral, 1)
        {
            Type = type ?? "";
            Definition = def;
            Width = def?.Width ?? 50;
            Height = def?.Height ?? 50;
            IsSolid = def?.HasCollision ?? false;
        }

        public override void Update(GameEngine engine, double dt)
        {
            base.Update(engine, dt);
        }
    }
}
