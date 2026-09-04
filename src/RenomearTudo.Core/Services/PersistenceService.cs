using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using RenomearTudo.Core.Models;

namespace RenomearTudo.Core.Services
{
    public sealed class PersistenceService
    {
        private readonly string _baseDirectory;
        private readonly string _historyFile;
        private readonly string _presetsFile;

        public PersistenceService(string applicationName = "RenomearTudo")
        {
            _baseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), applicationName);
            _historyFile = Path.Combine(_baseDirectory, "history.xml");
            _presetsFile = Path.Combine(_baseDirectory, "presets.xml");
        }

        public List<OperationHistory> LoadHistory()
        {
            return Load<List<OperationHistory>>(_historyFile) ?? new List<OperationHistory>();
        }

        public void SaveHistory(IEnumerable<OperationHistory> history)
        {
            Save(_historyFile, history.Take(50).ToList());
        }

        public List<PresetDefinition> LoadPresets()
        {
            return Load<List<PresetDefinition>>(_presetsFile) ?? new List<PresetDefinition>();
        }

        public void SavePresets(IEnumerable<PresetDefinition> presets)
        {
            Save(_presetsFile, presets.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ToList());
        }

        private static T Load<T>(string path) where T : class
        {
            try
            {
                if (!File.Exists(path)) return null;
                using (var stream = File.OpenRead(path))
                    return new XmlSerializer(typeof(T)).Deserialize(stream) as T;
            }
            catch
            {
                return null;
            }
        }

        private void Save<T>(string path, T value)
        {
            Directory.CreateDirectory(_baseDirectory);
            var temp = path + ".tmp";
            using (var stream = File.Create(temp))
                new XmlSerializer(typeof(T)).Serialize(stream, value);

            if (File.Exists(path)) File.Delete(path);
            File.Move(temp, path);
        }
    }
}
