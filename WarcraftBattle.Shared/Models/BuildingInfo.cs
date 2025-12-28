using System.Collections.Generic;
using System.Windows.Media;
using System.Xml.Serialization;

namespace WarcraftBattle.Shared.Models
{
    public class BuildingInfo
    {
        [XmlAttribute] public string Id { get; set; }
        [XmlAttribute] public string Name { get; set; }
        [XmlAttribute] public int Cost { get; set; }
        [XmlAttribute] public int Max { get; set; }
        [XmlAttribute] public double Width { get; set; }
        [XmlAttribute] public double Height { get; set; }
        [XmlAttribute] public double Scale { get; set; } = 1.0;
        [XmlAttribute] public string Image { get; set; }
        [XmlAttribute] public double Hp { get; set; }
        [XmlAttribute] public double Damage { get; set; }
        [XmlAttribute] public double Range { get; set; }
        [XmlAttribute] public double CD { get; set; }
        [XmlAttribute] public double OffsetY { get; set; } = 0;

        public List<string> Produces { get; set; } = new List<string>();

        [XmlElement("SpriteSheet")]
        public SpriteSheetConfig? SpriteConfig { get; set; }

        [XmlElement("Light")]
        public LightSourceDef Light { get; set; }

        [XmlElement("ProjectileSprite")]
        public ProjectileSpriteConfig? ProjectileConfig { get; set; }

        [XmlIgnore]
        public ImageSource IconSource { get; set; }

        public BuildingInfo Clone()
        {
            return new BuildingInfo
            {
                Id = Id, Name = Name, Cost = Cost, Max = Max, Width = Width, Height = Height,
                Scale = Scale, Image = Image, Hp = Hp, Damage = Damage, Range = Range, CD = CD, OffsetY = OffsetY,
                Produces = new List<string>(Produces), Light = Light,
                SpriteConfig = SpriteConfig, ProjectileConfig = ProjectileConfig
            };
        }
    }
}