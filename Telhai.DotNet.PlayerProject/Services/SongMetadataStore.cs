using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Telhai.DotNet.HadarKeller.PlayerProject.Models;

namespace Telhai.DotNet.HadarKeller.PlayerProject.Services
{
    public class SongMetadataStore
    {
        private const string MetadataFileName = "songMetadata.json";
        private readonly Dictionary<string, SongMetadata> _cache = new(StringComparer.OrdinalIgnoreCase);

        public SongMetadataStore()
        {
            Load();
        }

        public SongMetadata? GetByFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return null;

            _cache.TryGetValue(filePath, out var metadata);
            return metadata;
        }

        public void Upsert(SongMetadata metadata)
        {
            if (string.IsNullOrWhiteSpace(metadata.FilePath))
                return;

            _cache[metadata.FilePath] = metadata;
            Save();
        }

        private void Load()
        {
            if (!File.Exists(MetadataFileName))
                return;

            string json = File.ReadAllText(MetadataFileName);
            var list = JsonSerializer.Deserialize<List<SongMetadata>>(json) ?? new List<SongMetadata>();

            _cache.Clear();
            foreach (var item in list)
            {
                if (!string.IsNullOrWhiteSpace(item.FilePath))
                {
                    _cache[item.FilePath] = item;
                }
            }
        }

        private void Save()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_cache.Values.ToList(), options);
            File.WriteAllText(MetadataFileName, json);
        }
    }
}
