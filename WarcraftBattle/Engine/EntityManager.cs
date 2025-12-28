using System;
using System.Collections.Generic;
using System.Windows;
using WarcraftBattle.Shared.Enums;

namespace WarcraftBattle.Engine
{
    public static class ListExtensions
    {
        public static void RemoveBySwap<T>(this List<T> list, int index)
        {
            int lastIndex = list.Count - 1;
            if (index < lastIndex)
            {
                list[index] = list[lastIndex];
            }
            list.RemoveAt(lastIndex);
        }
    }

    public class EntityManager
    {
        public List<Entity> Entities { get; private set; } = new List<Entity>();

        // [Optimization] Use flat array instead of dictionary for spatial grid
        private List<Entity>[] _staticGrid;
        private List<Entity>[] _dynamicGrid;
        private int _gridWidth;
        private int _gridHeight;
        private const int GRID_CELL_SIZE = 150;

        public void Initialize(double worldWidth, double worldHeight)
        {
            _gridWidth = (int)Math.Ceiling(worldWidth / GRID_CELL_SIZE);
            _gridHeight = (int)Math.Ceiling(worldHeight / GRID_CELL_SIZE);
            int gridSize = _gridWidth * _gridHeight;

            _staticGrid = new List<Entity>[gridSize];
            _dynamicGrid = new List<Entity>[gridSize];

            for (int i = 0; i < gridSize; i++)
            {
                _staticGrid[i] = new List<Entity>();
                _dynamicGrid[i] = new List<Entity>();
            }
        }

        private bool IsStatic(Entity e) => e is Building || e is Obstacle;

        public void Add(Entity entity)
        {
            Entities.Add(entity);
            if (IsStatic(entity)) AddToGrid(_staticGrid, entity);
        }

        public void Remove(Entity entity)
        {
            Entities.Remove(entity);
            if (IsStatic(entity)) RemoveFromGrid(_staticGrid, entity);
        }

        public void Clear()
        {
            Entities.Clear();
            if (_staticGrid != null)
            {
                for (int i = 0; i < _staticGrid.Length; i++)
                {
                    _staticGrid[i].Clear();
                    _dynamicGrid[i].Clear();
                }
            }
        }

        public void Update(double dt, GameEngine engine)
        {
            UpdateSpatialGrid();

            for (int i = Entities.Count - 1; i >= 0; i--)
            {
                var e = Entities[i];
                if (e.HP <= 0 && e.DeathTimer > 5.0)
                {
                    if (IsStatic(e)) RemoveFromGrid(_staticGrid, e);
                    Entities.RemoveBySwap(i);
                    continue;
                }
                else if (e is LootItem l && l.IsCollected)
                {
                    // LootItem is dynamic, no need to remove from grid as it is cleared every frame
                    Entities.RemoveBySwap(i);
                    continue;
                }
                e.Update(dt, engine);
            }
        }

        private void UpdateSpatialGrid()
        {
            // Only clear and rebuild dynamic grid
            if (_dynamicGrid == null) return;

            for (int i = 0; i < _dynamicGrid.Length; i++)
                _dynamicGrid[i].Clear();

            for (int i = 0; i < Entities.Count; i++)
            {
                var e = Entities[i];
                if (!IsStatic(e))
                {
                    AddToGrid(_dynamicGrid, e);
                }
            }
        }

        private void AddToGrid(List<Entity>[] grid, Entity e)
        {
            int index = GetGridIndexFromWorld(e.X, e.Y);
            if (index != -1)
                grid[index].Add(e);
        }

        private void RemoveFromGrid(List<Entity>[] grid, Entity e)
        {
            int index = GetGridIndexFromWorld(e.X, e.Y);
            if (index != -1)
                grid[index].Remove(e);
        }

        private int GetGridIndex(int gx, int gy)
        {
            if (gx < 0 || gx >= _gridWidth || gy < 0 || gy >= _gridHeight) return -1;
            return gy * _gridWidth + gx;
        }

        private int GetGridIndexFromWorld(double x, double y)
        {
            int gx = (int)(x / GRID_CELL_SIZE);
            int gy = (int)(y / GRID_CELL_SIZE);
            return GetGridIndex(gx, gy);
        }

        public void GetNearbyEntitiesNonAlloc(Entity source, List<Entity> output, int rangeCells = 1)
        {
            output.Clear();
            int cx = (int)(source.X / GRID_CELL_SIZE);
            int cy = (int)(source.Y / GRID_CELL_SIZE);
            for (int x = -rangeCells; x <= rangeCells; x++)
            {
                for (int y = -rangeCells; y <= rangeCells; y++)
                {
                    int index = GetGridIndex(cx + x, cy + y);
                    if (index != -1)
                    {
                        output.AddRange(_staticGrid[index]);
                        output.AddRange(_dynamicGrid[index]);
                    }
                }
            }
        }

        public void GetEntitiesInRectNonAlloc(Rect rect, List<Entity> output)
        {
            output.Clear();
            int startGx = (int)(rect.Left / GRID_CELL_SIZE);
            int startGy = (int)(rect.Top / GRID_CELL_SIZE);
            int endGx = (int)(rect.Right / GRID_CELL_SIZE);
            int endGy = (int)(rect.Bottom / GRID_CELL_SIZE);
            for (int x = startGx; x <= endGx; x++)
            {
                for (int y = startGy; y <= endGy; y++)
                {
                    int index = GetGridIndex(x, y);
                    if (index != -1)
                    {
                        QueryGrid(_staticGrid, index, rect, output);
                        QueryGrid(_dynamicGrid, index, rect, output);
                    }
                }
            }
        }

        private void QueryGrid(List<Entity>[] grid, int index, Rect rect, List<Entity> output)
        {
            var cellEntities = grid[index];
            for (int i = 0; i < cellEntities.Count; i++)
            {
                var e = cellEntities[i];
                if (rect.Contains(new Point(e.X, e.Y))) output.Add(e);
            }
        }

        public List<Entity> GetEntitiesInRect(Rect rect)
        {
            var result = new List<Entity>();
            int startGx = (int)(rect.Left / GRID_CELL_SIZE);
            int startGy = (int)(rect.Top / GRID_CELL_SIZE);
            int endGx = (int)(rect.Right / GRID_CELL_SIZE);
            int endGy = (int)(rect.Bottom / GRID_CELL_SIZE);
            for (int x = startGx; x <= endGx; x++)
            {
                for (int y = startGy; y <= endGy; y++)
                {
                    int index = GetGridIndex(x, y);
                    if (index != -1)
                    {
                        QueryGrid(_staticGrid, index, rect, result);
                        QueryGrid(_dynamicGrid, index, rect, result);
                    }
                }
            }
            return result;
        }

        public Entity? FindClosestEntity(double x, double y, double maxRange)
        {
            Entity? bestEntity = null;
            double minDistanceSq = maxRange * maxRange;
            foreach (var e in Entities)
            {
                if (e.HP <= 0 || e is Obstacle || e is LootItem) continue;
                double dx = e.X - x;
                double dy = e.Y - y;
                double distSq = dx * dx + dy * dy;
                if (distSq < minDistanceSq) { minDistanceSq = distSq; bestEntity = e; }
            }
            return bestEntity;
        }

        public List<Entity> FindEntitiesInRange(double x, double y, double radius, TeamType? teamFilter = null)
        {
            var list = new List<Entity>();
            double radiusSq = radius * radius;
            foreach (var e in Entities)
            {
                // Optimization: Perform cheaper checks first
                if (e.HP <= 0 || e is Obstacle || e is LootItem) continue;
                if (teamFilter.HasValue && e.Team != teamFilter.Value) continue;

                if (Math.Abs(e.X - x) > radius) continue;
                if (Math.Abs(e.Y - y) > radius) continue;
                double distSq = (e.X - x) * (e.X - x) + (e.Y - y) * (e.Y - y);
                if (distSq <= radiusSq) list.Add(e);
            }
            return list;
        }
    }
}