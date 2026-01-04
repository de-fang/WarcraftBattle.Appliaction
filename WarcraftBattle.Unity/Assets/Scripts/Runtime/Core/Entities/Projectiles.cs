using System;
using System.Collections.Generic;

namespace WarcraftBattle3D.Core
{
    public class Projectile
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public double TX { get; private set; }
        public double TY { get; private set; }
        public double Damage { get; private set; }
        public AttackType AttackType { get; private set; }
        public UnitType SourceType { get; private set; }
        public TeamType SourceTeam { get; private set; }
        public bool Active { get; private set; } = true;
        public Entity Target { get; private set; }
        public double Speed { get; private set; }
        public double ArcHeight { get; private set; }
        public bool IsHoming { get; private set; } = true;
        public double Progress { get; private set; }
        public double Height { get; private set; }
        public double ImpactRadius { get; private set; }
        public bool HitAllTeams { get; private set; }
        public string VisualKey { get; private set; } = string.Empty;

        private double _startX;
        private double _startY;
        private double _distTotal;

        public void Init(
            double x,
            double y,
            Entity target,
            double damage,
            AttackType attackType,
            UnitType sourceType,
            TeamType sourceTeam,
            double impactRadius = 0,
            bool hitAllTeams = false,
            string visualKey = null)
        {
            X = x;
            Y = y;
            _startX = x;
            _startY = y;
            Target = target;
            Damage = damage;
            AttackType = attackType;
            SourceType = sourceType;
            SourceTeam = sourceTeam;
            ImpactRadius = impactRadius;
            HitAllTeams = hitAllTeams;
            VisualKey = visualKey ?? string.Empty;
            Active = true;
            Progress = 0;

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

            if (sourceType == UnitType.Siege)
            {
                Speed = 300;
                ArcHeight = 250;
                IsHoming = false;
                if (ImpactRadius <= 0)
                {
                    ImpactRadius = 120;
                }
            }
            else
            {
                Speed = 600;
                ArcHeight = 100;
                IsHoming = true;
            }

            _distTotal = Distance(_startX, _startY, TX, TY);
            if (_distTotal < 1)
            {
                _distTotal = 1;
            }
        }

        public void Init(
            double x,
            double y,
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
            X = x;
            Y = y;
            _startX = x;
            _startY = y;
            Target = null;
            Damage = damage;
            AttackType = attackType;
            SourceType = sourceType;
            SourceTeam = sourceTeam;
            ImpactRadius = impactRadius;
            HitAllTeams = hitAllTeams;
            VisualKey = visualKey ?? string.Empty;
            Active = true;
            Progress = 0;

            TX = targetX;
            TY = targetY;
            IsHoming = false;

            if (sourceType == UnitType.Siege)
            {
                Speed = 300;
                ArcHeight = 250;
                if (ImpactRadius <= 0)
                {
                    ImpactRadius = 120;
                }
            }
            else
            {
                Speed = 600;
                ArcHeight = 100;
            }

            _distTotal = Distance(_startX, _startY, TX, TY);
            if (_distTotal < 1)
            {
                _distTotal = 1;
            }
        }

        public void Update(double dt, GameEngine engine)
        {
            if (!Active)
            {
                return;
            }

            if (IsHoming && Target != null && Target.IsAlive)
            {
                TX = Target.X;
                TY = Target.Y;
                _distTotal = Distance(_startX, _startY, TX, TY);
                if (_distTotal < 1)
                {
                    _distTotal = 1;
                }
            }

            double step = (Speed * dt) / _distTotal;
            Progress += step;

            if (Progress >= 1.0)
            {
                Progress = 1.0;
                X = TX;
                Y = TY;
                Hit(engine);
                return;
            }

            X = _startX + (TX - _startX) * Progress;
            Y = _startY + (TY - _startY) * Progress;
            Height = Math.Sin(Progress * Math.PI) * ArcHeight;
        }

        private void Hit(GameEngine engine)
        {
            Active = false;
            if (engine == null)
            {
                return;
            }

            if (ImpactRadius > 0)
            {
                var targets = engine.EntityManager.FindEntitiesInRange(X, Y, ImpactRadius);
                for (int i = 0; i < targets.Count; i++)
                {
                    ApplyDamage(targets[i]);
                }
            }
            else if (Target != null && Target.IsAlive)
            {
                ApplyDamage(Target);
            }
        }

        private void ApplyDamage(Entity target)
        {
            if (target == null || !target.IsAlive)
            {
                return;
            }

            if (!HitAllTeams && target.Team == SourceTeam)
            {
                return;
            }

            double finalDamage = Damage;
            var armor = target is Unit unit ? unit.Stats.DefType : ArmorType.Fortified;
            finalDamage *= CombatRules.GetMultiplier(AttackType, armor);
            target.TakeDamage(finalDamage);
        }

        private static double Distance(double ax, double ay, double bx, double by)
        {
            double dx = bx - ax;
            double dy = by - ay;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }

    public class VisualEffect
    {
        public string Key { get; }
        public double X { get; }
        public double Y { get; }
        public double Life { get; private set; }

        public VisualEffect(string key, double x, double y, double life = 1.0)
        {
            Key = key ?? string.Empty;
            X = x;
            Y = y;
            Life = life;
        }

        public bool IsAlive => Life > 0;

        public void Update(double dt)
        {
            Life -= dt;
            if (Life < 0)
            {
                Life = 0;
            }
        }
    }
}
