using System.IO;
using System.Xml.Serialization;
using WarcraftBattle.Shared.Models;

namespace WarcraftBattle.Engine
{
    public static class DataManager
    {
        private static string FilePath = "SaveData.xml"; public static void Save(PlayerData data)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(PlayerData));
                using (StreamWriter writer = new StreamWriter(FilePath))
                {
                    serializer.Serialize(writer, data);
                }
            }
            catch { }
        }

        public static PlayerData Load()
        {
            if (!File.Exists(FilePath)) return new PlayerData(); try { XmlSerializer serializer = new XmlSerializer(typeof(PlayerData)); using (StreamReader reader = new StreamReader(FilePath)) { return (PlayerData)serializer.Deserialize(reader) ?? new PlayerData(); } } catch { return new PlayerData(); }
        }
    }
}