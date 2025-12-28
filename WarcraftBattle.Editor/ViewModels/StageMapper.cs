using System.Linq;
using WarcraftBattle.Shared.Models;
using WarcraftBattle.Shared.Models.Config;

namespace WarcraftBattle.Editor.ViewModels
{
    public static class StageMapper
    {
        public static StageInfo ToStageInfo(StageConfig config)
        {
            var stage = new StageInfo
            {
                Id = config.Id,
                Title = config.Title,
                EnemyBaseHp = config.Hp,
                MapWidth = config.Width,
                MapHeight = config.Height,
                TileMapData = config.TileMapData ?? string.Empty,
                RandomObstacleCount = config.ObstaclesCount
            };

            stage.TerrainRules = config.Terrain
                .Select(t => new StageTerrainDef { TileId = t.Id, Weight = t.Weight })
                .ToList();

            stage.TileOverrides = config.MapTiles
                .Select(t => new TileRectDef { X = t.X, Y = t.Y, W = t.W, H = t.H, TileId = t.TileId })
                .ToList();

            stage.Placements = config.Placements
                .Select(p => new EntityPlacement
                {
                    Team = p.Team,
                    Type = p.Type,
                    Key = p.Key,
                    X = p.X,
                    Y = p.Y,
                    Rotation = p.Rotation,
                    Width = p.Width,
                    Height = p.Height
                })
                .ToList();

            stage.FixedObstacles = config.Obstacles
                .Select(o => new LevelObstacleDef
                {
                    X = o.X,
                    Y = o.Y,
                    Type = o.Type,
                    Rotation = o.Rotation,
                    Width = o.Width,
                    Height = o.Height
                })
                .ToList();

            return stage;
        }

        public static void ApplyStageInfo(StageInfo stage, StageConfig config)
        {
            config.Id = stage.Id;
            config.Title = stage.Title;
            config.Hp = stage.EnemyBaseHp;
            config.Width = stage.MapWidth;
            config.Height = stage.MapHeight;
            config.TileMapData = stage.TileMapData;
            config.ObstaclesCount = stage.RandomObstacleCount;

            config.Terrain = stage.TerrainRules
                .Select(t => new StageTerrainConfig { Id = t.TileId, Weight = t.Weight })
                .ToList();

            config.MapTiles = stage.TileOverrides
                .Select(t => new MapTileRectConfig
                {
                    X = t.X,
                    Y = t.Y,
                    W = t.W,
                    H = t.H,
                    TileId = t.TileId
                })
                .ToList();

            config.Placements = stage.Placements
                .Select(p => new EntityPlacementConfig
                {
                    Team = p.Team,
                    Type = p.Type,
                    Key = p.Key,
                    X = p.X,
                    Y = p.Y,
                    Rotation = p.Rotation,
                    Width = p.Width,
                    Height = p.Height
                })
                .ToList();

            config.Obstacles = stage.FixedObstacles
                .Select(o => new StageObstacleConfig
                {
                    X = o.X,
                    Y = o.Y,
                    Type = o.Type,
                    Rotation = o.Rotation,
                    Width = o.Width,
                    Height = o.Height
                })
                .ToList();
        }

        public static ObstacleDef ToObstacleDef(ObstacleDefConfig config)
        {
            return new ObstacleDef
            {
                Key = config.Key,
                Name = config.Name,
                Image = config.Image,
                Folder = config.Folder,
                Width = config.Width,
                Height = config.Height,
                OffsetY = config.OffsetY,
                HasCollision = config.HasCollision
            };
        }

        public static BuildingInfo ToBuildingInfo(BuildingConfig config)
        {
            return new BuildingInfo
            {
                Id = config.Id,
                Name = config.Name,
                Image = config.Image,
                Scale = config.Scale,
                Cost = config.Cost,
                Hp = config.Hp,
                Max = config.Max,
                Width = config.Width,
                Height = config.Height,
                OffsetY = config.OffsetY,
                Damage = config.Damage,
                Range = config.Range,
                CD = config.CD,
                Produces = string.IsNullOrWhiteSpace(config.Produces)
                    ? new System.Collections.Generic.List<string>()
                    : config.Produces.Split(',').Select(p => p.Trim()).ToList()
            };
        }
    }
}
