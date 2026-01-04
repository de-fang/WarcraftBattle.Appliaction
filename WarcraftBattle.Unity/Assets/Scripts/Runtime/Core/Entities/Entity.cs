using System;

namespace WarcraftBattle3D.Core
{
    public abstract class Entity
    {
        private static int _nextId = 1;

        public int Id { get; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; } = 40;
        public double Height { get; set; } = 40;
        public double Rotation { get; set; }
        public TeamType Team { get; set; }
        public bool IsSolid { get; set; } = true;
        public double SightRadius { get; set; }

        public double HP { get; protected set; }
        public double MaxHP { get; protected set; }
        public double DeathTimer { get; private set; }

        public bool IsAlive => HP > 0;

        protected Entity(double x, double y, TeamType team, double hp)
        {
            Id = _nextId++;
            X = x;
            Y = y;
            Team = team;
            HP = hp;
            MaxHP = hp;
            SightRadius = 0;
        }

        public virtual void Update(GameEngine engine, double dt)
        {
            if (HP <= 0)
            {
                DeathTimer += dt;
            }
        }

        public virtual void TakeDamage(double amount)
        {
            if (HP <= 0)
            {
                return;
            }

            HP -= amount;
            if (HP < 0)
            {
                HP = 0;
            }
        }

        public void Heal(double amount)
        {
            if (HP <= 0 || amount <= 0)
            {
                return;
            }

            HP = Math.Min(MaxHP, HP + amount);
        }

        public void AddMaxHP(double amount)
        {
            if (amount == 0)
            {
                return;
            }

            MaxHP = Math.Max(1, MaxHP + amount);
            HP = Math.Min(MaxHP, HP + amount);
        }
    }
}
