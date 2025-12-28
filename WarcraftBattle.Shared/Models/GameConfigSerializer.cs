using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace WarcraftBattle.Shared.Models.Config
{
    public static class GameConfigSerializer
    {
        public static GameConfigData LoadFromFile(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(GameConfigData));
            using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return (GameConfigData)serializer.Deserialize(fs);
        }

        public static void SaveToFile(GameConfigData config, string path)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            XmlSerializer serializer = new XmlSerializer(typeof(GameConfigData));
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                NewLineChars = "\n"
            };

            using FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using XmlWriter writer = XmlWriter.Create(fs, settings);
            serializer.Serialize(writer, config);
        }

        public static string SaveAsNewFile(GameConfigData config, string basePath)
        {
            if (string.IsNullOrWhiteSpace(basePath))
            {
                throw new ArgumentException("Base path is required.", nameof(basePath));
            }

            string directory = Path.GetDirectoryName(basePath) ?? string.Empty;
            string fileName = Path.GetFileNameWithoutExtension(basePath);
            string extension = Path.GetExtension(basePath);
            string baseName = $"{fileName}.custom";
            string candidate = Path.Combine(directory, baseName + extension);
            int index = 1;

            while (File.Exists(candidate))
            {
                candidate = Path.Combine(directory, $"{baseName}.{index}{extension}");
                index++;
            }

            SaveToFile(config, candidate);
            return candidate;
        }
    }
}
