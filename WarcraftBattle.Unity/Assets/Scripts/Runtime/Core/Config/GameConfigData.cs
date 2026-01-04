using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Serialization;
using WarcraftBattle3D.Core;

namespace WarcraftBattle3D.Core.Config
{
    [XmlRoot("GameConfig")]
    public class GameConfigData
    {
        public AIProfileConfig AIProfile { get; set; }

        [XmlArray("Units")]
        [XmlArrayItem("Unit")]
        public List<UnitConfig> Units { get; set; } = new List<UnitConfig>();

        [XmlArray("Buildings")]
        [XmlArrayItem("Building")]
        public List<BuildingConfig> Buildings { get; set; } = new List<BuildingConfig>();

        [XmlArray("Effects")]
        [XmlArrayItem("Effect")]
        public List<EffectConfigData> Effects { get; set; } = new List<EffectConfigData>();

        [XmlArray("UnitUpgrades")]
        [XmlArrayItem("Tree")]
        public List<UnitUpgradeTree> UnitUpgrades { get; set; } = new List<UnitUpgradeTree>();

        [XmlArray("Upgrades")]
        [XmlArrayItem("Upgrade")]
        public List<UpgradeInfoConfig> Upgrades { get; set; } = new List<UpgradeInfoConfig>();

        [XmlArray("ObstacleDefinitions")]
        [XmlArrayItem("Obstacle")]
        public List<ObstacleDefConfig> ObstacleDefinitions { get; set; } =
            new List<ObstacleDefConfig>();

        public EnvironmentConfigData Environment { get; set; }

        [XmlArray("Stages")]
        [XmlArrayItem("Stage")]
        public List<StageConfig> Stages { get; set; } = new List<StageConfig>();

        [XmlArray("Triggers")]
        [XmlArrayItem("Trigger")]
        public List<TriggerDef> Triggers { get; set; } = new List<TriggerDef>();

        public GlobalSettingsConfig GlobalSettings { get; set; }
    }

    public class TriggerDef
    {
        [XmlAttribute]
        public string Name { get; set; } = "New Trigger";

        [XmlAttribute]
        public bool Enabled { get; set; } = true;

        [XmlArray("Events")]
        [XmlArrayItem("Event")]
        public List<TriggerEventDef> Events { get; set; } = new List<TriggerEventDef>();

        [XmlArray("Conditions")]
        [XmlArrayItem("Condition")]
        public List<TriggerConditionDef> Conditions { get; set; } = new List<TriggerConditionDef>();

        [XmlArray("Actions")]
        [XmlArrayItem("Action")]
        public List<TriggerActionDef> Actions { get; set; } = new List<TriggerActionDef>();
    }

    public class TriggerEventDef
    {
        [XmlAttribute]
        public string Type { get; set; }

        [XmlAttribute]
        public string Value { get; set; }
    }

    public class TriggerConditionDef
    {
        [XmlAttribute]
        public string Type { get; set; }

        [XmlAttribute]
        public string Value { get; set; }
    }

    public class TriggerActionDef
    {
        [XmlAttribute]
        public string Type { get; set; }

        [XmlAttribute]
        public string Value { get; set; }
    }

    public class AIProfileConfig
    {
        [XmlAttribute]
        public double Aggressiveness { get; set; } = 1.0;

        [XmlAttribute]
        public double WaveInterval { get; set; } = 10.0;

        [XmlArray("BuildOrder")]
        [XmlArrayItem("Item")]
        public List<string> BuildOrder { get; set; } = new List<string>();
    }

    public class UnitConfig
    {
        [XmlAttribute]
        public string Inherits { get; set; }

        [XmlAttribute]
        public string Key { get; set; }

        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string Faction { get; set; }

        [XmlIgnore]
        public int? Cost { get; set; }

        [XmlAttribute("Cost")]
        public string CostString
        {
            get => Cost?.ToString(CultureInfo.InvariantCulture);
            set =>
                Cost = int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out int v)
                    ? v
                    : (int?)null;
        }

        [XmlIgnore]
        public double? HP { get; set; }

        [XmlAttribute("HP")]
        public string HPString
        {
            get => HP?.ToString(CultureInfo.InvariantCulture);
            set =>
                HP = double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double v)
                    ? v
                    : (double?)null;
        }

        [XmlIgnore]
        public double? Dmg { get; set; }

        [XmlAttribute("Dmg")]
        public string DmgString
        {
            get => Dmg?.ToString(CultureInfo.InvariantCulture);
            set =>
                Dmg = double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double v)
                    ? v
                    : (double?)null;
        }

        [XmlIgnore]
        public double? Range { get; set; }

        [XmlAttribute("Range")]
        public string RangeString
        {
            get => Range?.ToString(CultureInfo.InvariantCulture);
            set =>
                Range = double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double v)
                    ? v
                    : (double?)null;
        }

        [XmlIgnore]
        public double? Speed { get; set; }

        [XmlAttribute("Speed")]
        public string SpeedString
        {
            get => Speed?.ToString(CultureInfo.InvariantCulture);
            set =>
                Speed = double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double v)
                    ? v
                    : (double?)null;
        }

        [XmlIgnore]
        public double? CD { get; set; }

        [XmlAttribute("CD")]
        public string CDString
        {
            get => CD?.ToString(CultureInfo.InvariantCulture);
            set =>
                CD = double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double v)
                    ? v
                    : (double?)null;
        }

        [XmlIgnore]
        public UnitType? Type { get; set; }

        [XmlAttribute("Type")]
        public string TypeString
        {
            get => Type?.ToString();
            set => Type = (value != null && Enum.TryParse(value, out UnitType r)) ? r : (UnitType?)null;
        }

        [XmlIgnore]
        public AttackType? AtkType { get; set; }

        [XmlAttribute("AtkType")]
        public string AtkTypeString
        {
            get => AtkType?.ToString();
            set =>
                AtkType = (value != null && Enum.TryParse(value, out AttackType r)) ? r : (AttackType?)null;
        }

        [XmlIgnore]
        public ArmorType? DefType { get; set; }

        [XmlAttribute("DefType")]
        public string DefTypeString
        {
            get => DefType?.ToString();
            set =>
                DefType = (value != null && Enum.TryParse(value, out ArmorType r)) ? r : (ArmorType?)null;
        }

        [XmlIgnore]
        public bool? IsHero { get; set; }

        [XmlAttribute("IsHero")]
        public string IsHeroString
        {
            get => IsHero?.ToString().ToLowerInvariant();
            set => IsHero = bool.TryParse(value, out bool v) ? v : (bool?)null;
        }

        [XmlIgnore]
        public bool? IsMounted { get; set; }

        [XmlAttribute("IsMounted")]
        public string IsMountedString
        {
            get => IsMounted?.ToString().ToLowerInvariant();
            set => IsMounted = bool.TryParse(value, out bool v) ? v : (bool?)null;
        }

        [XmlIgnore]
        public double? MaxMana { get; set; }

        [XmlAttribute("MaxMana")]
        public string MaxManaString
        {
            get => MaxMana?.ToString(CultureInfo.InvariantCulture);
            set =>
                MaxMana = double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double v)
                    ? v
                    : (double?)null;
        }

        [XmlIgnore]
        public double? ManaRegen { get; set; }

        [XmlAttribute("ManaRegen")]
        public string ManaRegenString
        {
            get => ManaRegen?.ToString(CultureInfo.InvariantCulture);
            set =>
                ManaRegen = double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double v)
                    ? v
                    : (double?)null;
        }

        [XmlIgnore]
        public double? BlockChance { get; set; }

        [XmlAttribute("BlockChance")]
        public string BlockChanceString
        {
            get => BlockChance?.ToString(CultureInfo.InvariantCulture);
            set =>
                BlockChance = double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double v)
                    ? v
                    : (double?)null;
        }

        [XmlIgnore]
        public double? CritChance { get; set; }

        [XmlAttribute("CritChance")]
        public string CritChanceString
        {
            get => CritChance?.ToString(CultureInfo.InvariantCulture);
            set =>
                CritChance = double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double v)
                    ? v
                    : (double?)null;
        }

        [XmlIgnore]
        public double? SightRadius { get; set; }

        [XmlAttribute("SightRadius")]
        public string SightRadiusString
        {
            get => SightRadius?.ToString(CultureInfo.InvariantCulture);
            set =>
                SightRadius = double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double v)
                    ? v
                    : (double?)null;
        }

        [XmlIgnore]
        public double? Width { get; set; }

        [XmlAttribute("Width")]
        public string WidthString
        {
            get => Width?.ToString(CultureInfo.InvariantCulture);
            set =>
                Width = double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double v)
                    ? v
                    : (double?)null;
        }

        [XmlIgnore]
        public double? Height { get; set; }

        [XmlAttribute("Height")]
        public string HeightString
        {
            get => Height?.ToString(CultureInfo.InvariantCulture);
            set =>
                Height = double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double v)
                    ? v
                    : (double?)null;
        }

        [XmlAttribute("Skill")]
        public string SkillName { get; set; }

        public SpriteSheetConfigData SpriteSheet { get; set; }
        public ProjectileSpriteConfigData ProjectileSprite { get; set; }

        [XmlArray("Skills")]
        [XmlArrayItem("Skill")]
        public List<SkillConfig> Skills { get; set; } = new List<SkillConfig>();
    }

    public class SpriteSheetConfigData
    {
        [XmlAttribute]
        public string Image { get; set; }

        [XmlAttribute]
        public int FrameW { get; set; } = 32;

        [XmlAttribute]
        public int FrameH { get; set; } = 32;

        [XmlAttribute]
        public double Scale { get; set; } = 1.0;

        [XmlAttribute]
        public int Count { get; set; } = 1;

        [XmlElement("State")]
        public List<SpriteStateConfig> States { get; set; } = new List<SpriteStateConfig>();
    }

    public class SpriteStateConfig
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public int Row { get; set; }

        [XmlAttribute]
        public int Count { get; set; } = 1;

        [XmlAttribute]
        public double Speed { get; set; } = 10;

        [XmlAttribute]
        public bool Loop { get; set; } = true;

        [XmlAttribute]
        public string Image { get; set; }
    }

    public class ProjectileSpriteConfigData : SpriteSheetConfigData
    {
        [XmlAttribute]
        public double Speed { get; set; } = 10;
        public LightConfig Light { get; set; }
    }

    public class LightConfig
    {
        [XmlAttribute]
        public string Color { get; set; } = "#FFFFFFFF";

        [XmlAttribute]
        public double Radius { get; set; } = 100;

        [XmlAttribute]
        public double FlickerIntensity { get; set; }

        [XmlAttribute]
        public double FlickerFrequency { get; set; }
    }

    public class SkillConfig
    {
        [XmlAttribute]
        public string Key { get; set; }

        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public int Cost { get; set; }

        [XmlAttribute]
        public double CD { get; set; }

        [XmlAttribute]
        public string Desc { get; set; }

        [XmlAttribute]
        public string Target { get; set; } = "Instant";

        [XmlAttribute]
        public double Range { get; set; }

        [XmlAttribute]
        public double Radius { get; set; }

        [XmlAttribute]
        public double CastTime { get; set; } = 0.5;

        [XmlArray("Effects")]
        [XmlArrayItem("Effect")]
        public List<SkillEffectConfig> Effects { get; set; } = new List<SkillEffectConfig>();
    }

    public class SkillEffectConfig
    {
        [XmlAttribute]
        public string Type { get; set; } = "Visual";

        [XmlAttribute]
        public string TargetType { get; set; } = "Target";

        [XmlAttribute]
        public double Value { get; set; }

        [XmlAttribute]
        public double Radius { get; set; }

        [XmlAttribute]
        public double Duration { get; set; }

        [XmlAttribute]
        public string VisualKey { get; set; }

        [XmlAttribute]
        public string ProjectileId { get; set; }
    }

    public class BuildingConfig
    {
        [XmlAttribute]
        public string Id { get; set; }

        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string Image { get; set; }

        [XmlAttribute]
        public double Scale { get; set; } = 1.0;

        [XmlAttribute]
        public int Cost { get; set; }

        [XmlAttribute]
        public double Hp { get; set; }

        [XmlAttribute]
        public int Max { get; set; } = 1;

        [XmlAttribute]
        public string Produces { get; set; }

        [XmlAttribute]
        public double Width { get; set; } = 100;

        [XmlAttribute]
        public double Height { get; set; } = 100;

        [XmlAttribute]
        public double OffsetY { get; set; }

        [XmlAttribute]
        public double Damage { get; set; }

        [XmlAttribute]
        public double Range { get; set; }

        [XmlAttribute]
        public double CD { get; set; }

        public SpriteSheetConfigData SpriteSheet { get; set; }
        public LightConfig Light { get; set; }
        public ProjectileSpriteConfigData ProjectileSprite { get; set; }
    }

    public class EffectConfigData
    {
        [XmlAttribute]
        public string Key { get; set; }

        [XmlAttribute]
        public string Image { get; set; }

        [XmlAttribute]
        public int FrameW { get; set; }

        [XmlAttribute]
        public int FrameH { get; set; }

        [XmlAttribute]
        public int Count { get; set; }

        [XmlAttribute]
        public double Speed { get; set; } = 15;

        [XmlAttribute]
        public double Scale { get; set; } = 1.0;

        public LightConfig Light { get; set; }
    }

    public class UnitUpgradeTree
    {
        [XmlAttribute]
        public string Key { get; set; }

        [XmlElement("Level")]
        public List<UpgradeLevelConfig> Levels { get; set; } = new List<UpgradeLevelConfig>();
    }

    public class UpgradeLevelConfig
    {
        [XmlAttribute]
        public int Cost { get; set; } = 100;

        [XmlAttribute]
        public string Type { get; set; }

        [XmlAttribute]
        public double Value { get; set; }

        [XmlAttribute]
        public string Desc { get; set; }
    }

    public class UpgradeInfoConfig
    {
        [XmlAttribute]
        public string Id { get; set; }

        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string Desc { get; set; }

        [XmlAttribute]
        public int Cost { get; set; }
    }

    public class ObstacleDefConfig
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
        public bool HasCollision { get; set; }

        [XmlAttribute]
        public double Width { get; set; } = 50;

        [XmlAttribute]
        public double Height { get; set; } = 50;

        [XmlAttribute]
        public double OffsetY { get; set; }

        public SpriteSheetConfigData SpriteSheet { get; set; }
    }

    public class EnvironmentConfigData
    {
        [XmlAttribute]
        public string BackgroundHex { get; set; } = "#1a1a2e";

        [XmlArray("TerrainTextures")]
        [XmlArrayItem("Texture")]
        public List<TerrainTextureConfig> TerrainTextures { get; set; } =
            new List<TerrainTextureConfig>();
    }

    public class TerrainTextureConfig
    {
        [XmlAttribute]
        public int Id { get; set; }

        [XmlAttribute]
        public string Path { get; set; }

        [XmlAttribute]
        public int Weight { get; set; } = 10;
    }

    public class StageConfig
    {
        [XmlAttribute]
        public int Id { get; set; } = 1;

        [XmlAttribute]
        public string Title { get; set; } = "Unknown";

        [XmlAttribute]
        public double Hp { get; set; } = 2000;

        [XmlAttribute]
        public double Width { get; set; } = 5000;

        [XmlAttribute]
        public double Height { get; set; } = 2000;

        [XmlAttribute]
        public int ObstaclesCount { get; set; }

        [XmlAttribute]
        public string TileMapData { get; set; }

        [XmlArray("Terrain")]
        [XmlArrayItem("Tile")]
        public List<StageTerrainConfig> Terrain { get; set; } = new List<StageTerrainConfig>();

        [XmlArray("Placements")]
        [XmlArrayItem("Entity")]
        public List<EntityPlacementConfig> Placements { get; set; } = new List<EntityPlacementConfig>();

        [XmlArray("Obstacles")]
        [XmlArrayItem("Obstacle")]
        public List<StageObstacleConfig> Obstacles { get; set; } = new List<StageObstacleConfig>();

        [XmlArray("MapTiles")]
        [XmlArrayItem("Rect")]
        public List<MapTileRectConfig> MapTiles { get; set; } = new List<MapTileRectConfig>();
    }

    public class StageTerrainConfig
    {
        [XmlAttribute]
        public int Id { get; set; }

        [XmlAttribute]
        public int Weight { get; set; }
    }

    public class EntityPlacementConfig
    {
        [XmlAttribute]
        public string Team { get; set; } = "neutral";

        [XmlAttribute]
        public string Type { get; set; } = "Unit";

        [XmlAttribute]
        public string Key { get; set; }

        [XmlAttribute]
        public double X { get; set; }

        [XmlAttribute]
        public double Y { get; set; }

        [XmlAttribute]
        public double Rotation { get; set; }

        [XmlAttribute]
        public double Width { get; set; }

        [XmlAttribute]
        public double Height { get; set; }
    }

    public class StageObstacleConfig
    {
        [XmlAttribute]
        public double X { get; set; }

        [XmlAttribute]
        public double Y { get; set; }

        [XmlAttribute]
        public string Type { get; set; } = "Rock";

        [XmlAttribute]
        public double Rotation { get; set; }

        [XmlAttribute]
        public double Width { get; set; }

        [XmlAttribute]
        public double Height { get; set; }
    }

    public class MapTileRectConfig
    {
        [XmlAttribute]
        public int X { get; set; }

        [XmlAttribute]
        public int Y { get; set; }

        [XmlAttribute]
        public int W { get; set; } = 1;

        [XmlAttribute]
        public int H { get; set; } = 1;

        [XmlAttribute]
        public int Id { get; set; }
    }

    public class GlobalSettingsConfig
    {
        [XmlAttribute]
        public string Title { get; set; } = "Warcraft";

        [XmlAttribute]
        public double InitialGold { get; set; } = 200;

        [XmlAttribute]
        public double BaseGoldPerSec { get; set; } = 5;

        [XmlAttribute]
        public int MaxPop { get; set; } = 10;

        [XmlAttribute]
        public double HouseGoldPerSec { get; set; } = 2;

        [XmlAttribute]
        public double FloaterGravity { get; set; } = 0.2;

        [XmlAttribute]
        public double FloaterUpForce { get; set; } = 3.0;

        [XmlAttribute]
        public double FloaterSpread { get; set; } = 2.0;

        [XmlAttribute]
        public bool EnableFog { get; set; } = true;

        [XmlAttribute]
        public double FogStart { get; set; } = 400;

        [XmlAttribute]
        public double FogFade { get; set; } = 500;
    }
}
