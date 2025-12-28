using System.Collections.Generic;
using System.Xml.Serialization;

namespace WarcraftBattle.Shared.Models
{
    public class AIProfile
    {
        [XmlAttribute] public double Aggressiveness { get; set; } = 1.0; // 0.0 - 2.0
        [XmlAttribute] public double WaveInterval { get; set; } = 10.0;
        
        [XmlArray("BuildOrder")]
        [XmlArrayItem("Item")]
        public List<string> BuildOrder { get; set; } = new List<string>();
    }
}