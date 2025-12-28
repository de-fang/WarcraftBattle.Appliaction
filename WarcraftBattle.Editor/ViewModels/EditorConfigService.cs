using System.IO;
using System.Xml.Serialization;
using WarcraftBattle.Shared.Models.Config;

namespace WarcraftBattle.Editor.ViewModels
{
    public class EditorConfigService
    {
        public GameConfigData Load(string path)
        {
            var serializer = new XmlSerializer(typeof(GameConfigData));
            using var stream = File.OpenRead(path);
            return (GameConfigData)serializer.Deserialize(stream);
        }

        public void Save(string path, GameConfigData config)
        {
            var serializer = new XmlSerializer(typeof(GameConfigData));
            using var stream = File.Create(path);
            serializer.Serialize(stream, config);
        }
    }
}
