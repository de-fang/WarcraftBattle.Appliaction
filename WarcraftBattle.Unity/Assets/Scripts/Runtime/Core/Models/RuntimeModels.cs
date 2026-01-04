using System.Collections.Generic;
using System.Xml.Serialization;

namespace WarcraftBattle3D.Core
{
    public class EffectConfig
    {
        public string Key;
        public string Image;
        public int FrameW;
        public int FrameH;
        public int Count;
        public double Speed = 15;
        public double Scale = 1.0;
        public LightSourceDef Light { get; set; }
    }

    public class LightSourceDef
    {
        [XmlAttribute]
        public string Color { get; set; } = "#FFFFFFFF";

        [XmlAttribute]
        public double Radius { get; set; } = 100;

        [XmlAttribute]
        public double FlickerIntensity { get; set; } = 0;

        [XmlAttribute]
        public double FlickerFrequency { get; set; } = 0;
    }

    public class UpgradeInfo
    {
        public string Id = "";
        public string Name = "";
        public string Desc = "";
        public int Cost;
    }

    public class UpgradeLevelDef
    {
        public int Cost;
        public string Type = "";
        public double Value;
        public string Desc = "";
    }

    public class ObstacleDef
    {
        [XmlAttribute]
        public string Key { get; set; }

        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string Image { get; set; }

        [XmlAttribute]
        public string Folder { get; set; }

        [XmlAttribute]
        public double Width { get; set; } = 50;

        [XmlAttribute]
        public double Height { get; set; } = 50;

        [XmlAttribute]
        public double OffsetY { get; set; } = 0;

        [XmlAttribute]
        public bool HasCollision { get; set; } = false;

        [XmlElement("SpriteSheet")]
        public SpriteSheetConfig SpriteConfig { get; set; }
    }

    public class EnvironmentConfig
    {
        [XmlAttribute]
        public string BackgroundHex { get; set; } = "#1a1a2e";

        [XmlElement("Texture")]
        public List<TerrainDef> TerrainTextures { get; set; } = new List<TerrainDef>();
    }

    public class TerrainDef
    {
        [XmlAttribute]
        public int Id { get; set; }

        [XmlAttribute]
        public string Path { get; set; }

        [XmlAttribute]
        public int Weight { get; set; }
    }

    public class UnitStats
    {
        public string Key { get; set; } = "";
        public string Faction { get; set; } = "Neutral";
        public string Name { get; set; } = "";
        public int Cost { get; set; }
        public double HP { get; set; }
        public double Dmg { get; set; }
        public double Range { get; set; }
        public double Speed { get; set; }
        public int CD { get; set; }
        public UnitType Type { get; set; }
        public AttackType AtkType { get; set; } = AttackType.Normal;
        public ArmorType DefType { get; set; } = ArmorType.Medium;
        public double Width { get; set; } = 30;
        public double Height { get; set; } = 40;
        public bool IsMounted { get; set; }
        public bool IsHero { get; set; }
        public double MaxMana { get; set; } = 0;
        public double ManaRegen { get; set; } = 0;
        public string SkillName { get; set; } = "";
        public double SightRadius { get; set; } = 450;
        public bool DoubleShot { get; set; } = false;
        public double BlockChance { get; set; } = 0;
        public double CritChance { get; set; } = 0;
        public List<SkillDef> SkillDefs { get; set; } = new List<SkillDef>();
        public SpriteSheetConfig SpriteConfig { get; set; }
        public ProjectileSpriteConfig ProjectileConfig { get; set; }

        public UnitStats Clone()
        {
            var clone = (UnitStats)MemberwiseClone();
            clone.SkillDefs = new List<SkillDef>(SkillDefs);
            return clone;
        }
    }

    public class SkillDef
    {
        [XmlAttribute]
        public string Key { get; set; } = "";

        [XmlAttribute]
        public string Name { get; set; } = "";

        [XmlAttribute]
        public int Cost { get; set; }

        [XmlAttribute]
        public double CD { get; set; }

        [XmlAttribute]
        public string Desc { get; set; } = "";

        [XmlAttribute]
        public string Target { get; set; } = "Instant";

        [XmlAttribute]
        public double Range { get; set; } = 0;

        [XmlAttribute]
        public double Radius { get; set; } = 0;

        [XmlAttribute]
        public double CastTime { get; set; } = 0.5;

        public List<SkillEffectDef> Effects { get; set; } = new List<SkillEffectDef>();
    }

    public class SkillEffectDef
    {
        public string Type;
        public string TargetType;
        public double Value;
        public double Range;
        public double Radius;
        public double Duration;
        public string VisualKey;
        public string ProjectileId;
    }

    public class SpriteAnimDef
    {
        [XmlAttribute]
        public string Name { get; set; } = "";

        [XmlAttribute]
        public int Row { get; set; } = 0;

        [XmlAttribute]
        public int Count { get; set; } = 1;

        [XmlAttribute]
        public double Speed { get; set; } = 10;

        [XmlAttribute]
        public bool Loop { get; set; } = true;
    }

    public class SpriteSheetConfig
    {
        [XmlAttribute]
        public string Image { get; set; } = "";

        [XmlAttribute]
        public int FrameW { get; set; } = 32;

        [XmlAttribute]
        public int FrameH { get; set; } = 32;

        [XmlAttribute]
        public double Scale { get; set; } = 1.0;

        [XmlAttribute]
        public int Count { get; set; } = 1;

        [XmlElement("State")]
        public List<SpriteAnimDef> States { get; set; } = new List<SpriteAnimDef>();
    }

    public class ProjectileSpriteConfig
    {
        [XmlAttribute]
        public string Image { get; set; } = "";

        [XmlAttribute]
        public int FrameW { get; set; } = 32;

        [XmlAttribute]
        public int FrameH { get; set; } = 32;

        [XmlAttribute]
        public int Count { get; set; } = 1;

        [XmlAttribute]
        public double Speed { get; set; } = 15;

        [XmlAttribute]
        public double Scale { get; set; } = 1.0;

        [XmlElement("Light")]
        public LightSourceDef Light { get; set; }
    }

    public class BuildingInfo
    {
        [XmlAttribute]
        public string Id { get; set; }

        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public int Cost { get; set; }

        [XmlAttribute]
        public int Max { get; set; }

        [XmlAttribute]
        public double Width { get; set; }

        [XmlAttribute]
        public double Height { get; set; }

        [XmlAttribute]
        public double Scale { get; set; } = 1.0;

        [XmlAttribute]
        public string Image { get; set; }

        [XmlAttribute]
        public double Hp { get; set; }

        [XmlAttribute]
        public double Damage { get; set; }

        [XmlAttribute]
        public double Range { get; set; }

        [XmlAttribute]
        public double CD { get; set; }

        [XmlAttribute]
        public double OffsetY { get; set; } = 0;

        public List<string> Produces { get; set; } = new List<string>();

        [XmlElement("SpriteSheet")]
        public SpriteSheetConfig SpriteConfig { get; set; }

        [XmlElement("Light")]
        public LightSourceDef Light { get; set; }

        [XmlElement("ProjectileSprite")]
        public ProjectileSpriteConfig ProjectileConfig { get; set; }

        public BuildingInfo Clone()
        {
            return new BuildingInfo
            {
                Id = Id,
                Name = Name,
                Cost = Cost,
                Max = Max,
                Width = Width,
                Height = Height,
                Scale = Scale,
                Image = Image,
                Hp = Hp,
                Damage = Damage,
                Range = Range,
                CD = CD,
                OffsetY = OffsetY,
                Produces = new List<string>(Produces),
                Light = Light,
                SpriteConfig = SpriteConfig,
                ProjectileConfig = ProjectileConfig
            };
        }
    }

    public struct EntityPlacement
    {
        public string Team;
        public string Type;
        public string Key;
        public double X;
        public double Y;
        public double Rotation;
        public double Width;
        public double Height;
    }

    public struct StageTerrainDef
    {
        public int TileId;
        public int Weight;
    }

    public struct LevelObstacleDef
    {
        public double X;
        public double Y;
        public string Type;
        public string ImagePath;
        public double Width;
        public double Height;
        public double Rotation;
    }

    public struct TileRectDef
    {
        public int X;
        public int Y;
        public int W;
        public int H;
        public int TileId;
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

    public class AIProfile
    {
        [XmlAttribute]
        public double Aggressiveness { get; set; } = 1.0;

        [XmlAttribute]
        public double WaveInterval { get; set; } = 10.0;

        [XmlArray("BuildOrder")]
        [XmlArrayItem("Item")]
        public List<string> BuildOrder { get; set; } = new List<string>();
    }

    public class PlayerData
    {
        public int Honor { get; set; } = 0;
        public int MaxUnlockedStage { get; set; } = 1;
        public List<UnitLevelEntry> UnitLevels { get; set; } = new List<UnitLevelEntry>();
        public List<string> PermanentUpgradeIds { get; set; } = new List<string>();
    }

    public class UnitLevelEntry
    {
        public string Key { get; set; } = "";
        public int Level { get; set; }
    }

    public class FloaterInfo
    {
        public string Text { get; set; } = "";
        public double X { get; set; } = 0;
        public double Y { get; set; } = 0;
        public string Color { get; set; } = "White";
        public double VelX { get; set; } = 0;
        public double VelY { get; set; } = 0;
        public double Life { get; set; } = 1.0;
        public double Size { get; set; } = 20;
    }
}
