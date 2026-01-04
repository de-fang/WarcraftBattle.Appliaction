using System.Collections.Generic;
using System.Linq;

namespace WarcraftBattle3D.Core
{
    public class EntityManager
    {
        public List<Entity> Entities { get; } = new List<Entity>();

        public void Add(Entity entity)
        {
            if (entity != null)
            {
                Entities.Add(entity);
            }
        }

        public void Remove(Entity entity)
        {
            if (entity != null)
            {
                Entities.Remove(entity);
            }
        }

        public void Clear()
        {
            Entities.Clear();
        }

        public void Update(GameEngine engine, double dt)
        {
            for (int i = Entities.Count - 1; i >= 0; i--)
            {
                var e = Entities[i];
                e.Update(engine, dt);

                if (!e.IsAlive && e.DeathTimer > 5.0)
                {
                    Entities.RemoveAt(i);
                }
            }
        }

        public Entity FindClosestEnemy(Entity source, double maxRange)
        {
            if (source == null)
            {
                return null;
            }

            double maxRangeSq = maxRange * maxRange;
            Entity best = null;
            double bestDistSq = maxRangeSq;

            foreach (var e in Entities)
            {
                if (e == source || !e.IsAlive || e.Team == source.Team)
                {
                    continue;
                }

                double dx = e.X - source.X;
                double dy = e.Y - source.Y;
                double distSq = dx * dx + dy * dy;
                if (distSq <= bestDistSq)
                {
                    bestDistSq = distSq;
                    best = e;
                }
            }

            return best;
        }

        public List<Entity> FindEntitiesInRange(double x, double y, double radius, TeamType? teamFilter = null)
        {
            var list = new List<Entity>();
            double radiusSq = radius * radius;

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
                if (distSq <= radiusSq)
                {
                    list.Add(e);
                }
            }

            return list;
        }

        public int CountByTeam(TeamType team)
        {
            return Entities.Count(e => e.Team == team && e.IsAlive);
        }
    }
}
