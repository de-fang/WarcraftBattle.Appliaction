using System.Collections.Generic;
using System.Xml.Serialization;
using WarcraftBattle.Shared.Enums;

namespace WarcraftBattle.Shared.Models
{
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
        public SpriteSheetConfig? SpriteConfig { get; set; }
        public ProjectileSpriteConfig? ProjectileConfig { get; set; }

        public UnitStats Clone()
        {
            var clone = (UnitStats)this.MemberwiseClone();
            clone.SkillDefs = new List<SkillDef>(this.SkillDefs);
            return clone;
        }
    }

    public class SkillDef
    {
        [XmlAttribute] public string Key { get; set; } = "";
        [XmlAttribute] public string Name { get; set; } = "";
        [XmlAttribute] public int Cost { get; set; }
        [XmlAttribute] public double CD { get; set; }
        [XmlAttribute] public string Desc { get; set; } = "";
        [XmlAttribute] public string Target { get; set; } = "Instant";
        [XmlAttribute] public double Range { get; set; } = 0;
        [XmlAttribute] public double Radius { get; set; } = 0;
        [XmlAttribute] public double CastTime { get; set; } = 0.5;
        public List<SkillEffectDef> Effects { get; set; } = new List<SkillEffectDef>();
    }

    public class SpriteAnimDef
    {
        [XmlAttribute] public string Name { get; set; } = "";
        [XmlAttribute] public int Row { get; set; } = 0;
        [XmlAttribute] public int Count { get; set; } = 1;
        [XmlAttribute] public double Speed { get; set; } = 10;
        [XmlAttribute] public bool Loop { get; set; } = true;
    }

    public class SpriteSheetConfig
    {
        [XmlAttribute] public string Image { get; set; } = "";
        [XmlAttribute] public int FrameW { get; set; } = 32;
        [XmlAttribute] public int FrameH { get; set; } = 32;
        [XmlAttribute] public double Scale { get; set; } = 1.0;
        [XmlAttribute] public int Count { get; set; } = 1;
        [XmlElement("State")]
        public List<SpriteAnimDef> States { get; set; } = new List<SpriteAnimDef>();
    }

    public class ProjectileSpriteConfig
    {
        [XmlAttribute] public string Image { get; set; } = "";
        [XmlAttribute] public int FrameW { get; set; } = 32;
        [XmlAttribute] public int FrameH { get; set; } = 32;
        [XmlAttribute] public int Count { get; set; } = 1;
        [XmlAttribute] public double Speed { get; set; } = 15;
        [XmlAttribute] public double Scale { get; set; } = 1.0;

        [XmlElement("Light")]
        public LightSourceDef Light { get; set; }
    }
}