using System.Collections.Generic;

namespace WarcraftBattle.Engine
{
    public interface IGameContext
    {
        List<Entity> Entities { get; }
        IPathfinder Pathfinder { get; }
        double WorldWidth { get; }
        double MapDepth { get; }

        ObjectPool<Projectile> ProjectilePool { get; }
        List<VisualEffect> VisualEffects { get; }
        List<Projectile> Projectiles { get; }
        ParticleData[] Particles { get; }
        int GetNewParticleIndex();
        double ShakeIntensity { get; set; }

        void AddFloater(string t, double x, double y, string c, double size = 20, double velX = 0, double velY = 0);
    }
}