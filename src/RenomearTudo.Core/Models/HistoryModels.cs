using System;
using System.Collections.Generic;

namespace RenomearTudo.Core.Models
{
    [Serializable]
    public class RenameRecord
    {
        public string OldPath { get; set; } = string.Empty;
        public string NewPath { get; set; } = string.Empty;
    }

    [Serializable]
    public class OperationHistory
    {
        public OperationHistory()
        {
            Id = Guid.NewGuid().ToString("N");
            Timestamp = DateTime.Now;
            Records = new List<RenameRecord>();
            Result = "Concluído";
        }

        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Result { get; set; }
        public string Folder { get; set; } = string.Empty;
        public List<RenameRecord> Records { get; set; }
        public int Count => Records?.Count ?? 0;
    }

    [Serializable]
    public class PresetDefinition
    {
        public PresetDefinition()
        {
            Name = string.Empty;
            Rules = new List<RenameRule>();
        }

        public string Name { get; set; }
        public List<RenameRule> Rules { get; set; }
    }
}
