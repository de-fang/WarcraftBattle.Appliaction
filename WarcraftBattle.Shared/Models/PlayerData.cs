using System.Collections.Generic;

namespace WarcraftBattle.Shared.Models
{
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
}