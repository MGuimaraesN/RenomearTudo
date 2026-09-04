using System;

namespace RenomearTudo.Core.Models
{
    public enum RenameRuleType
    {
        Prefix,
        Suffix,
        Replace,
        Numbering,
        Template,
        ChangeExtension,
        ChangeCase,
        Insert,
        RemoveText,
        RemoveAccents,
        RemoveSpecialCharacters
    }

    public enum RenameCaseMode
    {
        Upper,
        Lower,
        Title
    }

    [Serializable]
    public class RenameRule
    {
        public RenameRule()
        {
            Id = Guid.NewGuid().ToString("N");
            Enabled = true;
            Type = RenameRuleType.Prefix;
            NumberStart = 1;
            NumberStep = 1;
            NumberPadding = 3;
            Position = 0;
            Text1 = string.Empty;
            Text2 = string.Empty;
        }

        public string Id { get; set; }
        public bool Enabled { get; set; }
        public RenameRuleType Type { get; set; }
        public string Text1 { get; set; }
        public string Text2 { get; set; }
        public int NumberStart { get; set; }
        public int NumberStep { get; set; }
        public int NumberPadding { get; set; }
        public int Position { get; set; }
        public RenameCaseMode CaseMode { get; set; }
        public bool UseRegex { get; set; }
        public bool ReplaceFirstOnly { get; set; }
        public bool IgnoreCase { get; set; }

        public RenameRule Clone()
        {
            return (RenameRule)MemberwiseClone();
        }
    }
}
