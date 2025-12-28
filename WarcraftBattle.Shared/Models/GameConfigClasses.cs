using System.Collections.Generic;
using System.Xml.Serialization;

namespace WarcraftBattle.Shared.Models
{
    public class EffectConfig
    {
        public string Key; public string Image; public int FrameW; public int FrameH; public int Count; public double Speed = 15; public double Scale = 1.0;
        public LightSourceDef Light { get; set; }
    }

    public class LightSourceDef
    {
        [XmlAttribute] public string Color { get; set; } = "#FFFFFFFF";
        [XmlAttribute] public double Radius { get; set; } = 100;
        [XmlAttribute] public double FlickerIntensity { get; set; } = 0;
        [XmlAttribute] public double FlickerFrequency { get; set; } = 0;
    }

    public class UpgradeInfo
    {
        public string Id = ""; public string Name = ""; public string Desc = ""; public int Cost;
    }

    public class UpgradeLevelDef
    {
        public int Cost; public string Type = ""; public double Value; public string Desc = "";
    }

    public class ObstacleDef
    {
        [XmlAttribute] public string Key { get; set; }
        [XmlAttribute] public string Name { get; set; }
        [XmlAttribute] public string Image { get; set; }
        [XmlAttribute] public string Folder { get; set; }
        [XmlAttribute] public double Width { get; set; } = 50;
        [XmlAttribute] public double Height { get; set; } = 50;
        [XmlAttribute] public double OffsetY { get; set; } = 0;
        [XmlAttribute] public bool HasCollision { get; set; } = false;

        [XmlElement("SpriteSheet")]
        public SpriteSheetConfig SpriteConfig { get; set; }
    }

    public class EnvironmentConfig
    {
        [XmlAttribute] public string BackgroundHex { get; set; } = "#1a1a2e";
        [XmlElement("Texture")]
        public List<TerrainDef> TerrainTextures { get; set; } = new List<TerrainDef>();
    }

    public class TerrainDef
    {
        [XmlAttribute] public int Id { get; set; }
        [XmlAttribute] public string Path { get; set; }
        [XmlAttribute] public int Weight { get; set; }
    }
}