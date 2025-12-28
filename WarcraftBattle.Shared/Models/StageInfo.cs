using System.Collections.Generic;

namespace WarcraftBattle.Shared.Models
{
    public struct EntityPlacement
    {
        public string Team; public string Type; public string Key; public double X, Y; public double Rotation; public double Width, Height;
    }

    public struct StageTerrainDef
    {
        public int TileId; public int Weight;
    }

    public struct LevelObstacleDef
    {
        public double X, Y; public string Type; public string ImagePath; public double Width, Height; public double Rotation;
    }

    public struct TileRectDef
    {
        public int X, Y; public int W, H; public int TileId;
    }

    public class StageInfo
    {
        public int Id;
        public string Title = "";
        public double EnemyBaseHp;
        public double MapWidth = 5000;
        public double MapHeight = 2000;
        public string TileMapData = "";
        public int RandomObstacleCount = 10;
        public List<StageTerrainDef> TerrainRules = new List<StageTerrainDef>();
        public List<TileRectDef> TileOverrides = new List<TileRectDef>();
        public List<EntityPlacement> Placements = new List<EntityPlacement>();
        public List<LevelObstacleDef> FixedObstacles = new List<LevelObstacleDef>();
        public List<PointD> VisualRoad = new List<PointD>();
    }
}
