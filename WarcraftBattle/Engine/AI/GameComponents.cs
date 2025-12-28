using System;
using System.Windows.Media;
using WarcraftBattle.Shared.Enums;
using WarcraftBattle.Shared.Models;

namespace WarcraftBattle.Engine.Components
{
    public interface IComponent
    {
        void Update(double dt, GameEngine engine);
    }

    public class HealthComponent : IComponent
    {
        public double HP;
        public double MaxHP;
        public double DeathTimer;

        public HealthComponent(double hp) { HP = hp; MaxHP = hp; }
        public void Update(double dt, GameEngine engine) { if (HP <= 0) DeathTimer += dt; }
        public void TakeDamage(double amount) { HP -= amount; }
    }

    public class ManaComponent : IComponent
    {
        public double Mana;
        public double MaxMana;
        public double Regen;

        public ManaComponent(double max, double regen) { MaxMana = max; Regen = regen; }
        public void Update(double dt, GameEngine engine) { if (MaxMana > 0) Mana = Math.Min(MaxMana, Mana + Regen * dt); }
    }

    public class MovementComponent : IComponent
    {
        public double Speed;
        public PointD Velocity;
        public double Acceleration = 15.0;
        public double Facing = 1;

        public MovementComponent(double speed) { Speed = speed; }
        public void Update(double dt, GameEngine engine) { }
    }

    public class AttackComponent : IComponent
    {
        public double Damage;
        public double Range;
        public double MaxCD;
        public double CurrentCD;
        public AttackType Type;

        public AttackComponent(double dmg, double range, double cd, AttackType type)
        { Damage = dmg; Range = range; MaxCD = cd; Type = type; }

        public void Update(double dt, GameEngine engine) { if (CurrentCD > 0) CurrentCD -= dt * 60.0; }
    }

    public class LightSourceComponent : IComponent
    {
        public Color LightColor;
        public double Radius;
        public double FlickerIntensity;
        public double FlickerFrequency;
        public double CurrentRadius;

        public LightSourceComponent(Color color, double radius, double flickerIntensity = 0, double flickerFrequency = 0)
        {
            LightColor = color;
            Radius = radius;
            CurrentRadius = radius;
            FlickerIntensity = flickerIntensity;
            FlickerFrequency = flickerFrequency;
        }

        public void Update(double dt, GameEngine engine)
        {
            if (FlickerFrequency > 0)
            {
                double flicker = Math.Sin(engine.TotalTime * FlickerFrequency) * FlickerIntensity;
                CurrentRadius = Radius + flicker;
            }
        }
    }

    public class VisionComponent : IComponent
    {
        public double SightRadius { get; set; }

        public VisionComponent(double radius)
        {
            SightRadius = radius;
        }

        public void Update(double dt, GameEngine engine) { }
    }
}