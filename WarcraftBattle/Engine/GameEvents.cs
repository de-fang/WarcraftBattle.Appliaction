using System;
using WarcraftBattle.Engine;

namespace WarcraftBattle.Engine
{
    public class UnitDiedEvent
    {
        public Unit Victim;
        public Entity Killer; // Can be Unit, Building, or null

        public UnitDiedEvent(Unit victim, Entity killer)
        {
            Victim = victim;
            Killer = killer;
        }
    }

    public class EntityDiedEvent
    {
        public Entity Entity;
        public Entity Killer; // Can be Unit, Building, or null

        public EntityDiedEvent(Entity entity, Entity killer)
        {
            Entity = entity;
            Killer = killer;
        }
    }

    public class ProjectileHitEvent
    {
        public Projectile Projectile;
        public Entity Target; // Entity hit, or null if ground/area

        public ProjectileHitEvent(Projectile projectile, Entity target)
        {
            Projectile = projectile;
            Target = target;
        }
    }

    public class UnitCommandEvent
    {
        public Unit Unit;
        public UnitCommand Command;

        public UnitCommandEvent(Unit unit, UnitCommand command)
        {
            Unit = unit;
            Command = command;
        }
    }
}
