using RenomearTudo.App.Infrastructure;
using RenomearTudo.Core.Models;

namespace RenomearTudo.App.Models
{
    public sealed class BindableRenameRule : ObservableObject
    {
        private readonly RenameRule _model;

        public BindableRenameRule(RenameRule model) { _model = model; }
        public RenameRule Model => _model;

        public bool Enabled { get => _model.Enabled; set { if (_model.Enabled != value) { _model.Enabled = value; OnChanged(); } } }
        public RenameRuleType Type { get => _model.Type; set { if (_model.Type != value) { _model.Type = value; OnChanged(); OnPropertyChanged(nameof(DisplayName)); OnPropertyChanged(nameof(Summary)); NotifyVisibility(); } } }
        public string Text1 { get => _model.Text1; set { if (_model.Text1 != value) { _model.Text1 = value; OnChanged(); OnPropertyChanged(nameof(Summary)); } } }
        public string Text2 { get => _model.Text2; set { if (_model.Text2 != value) { _model.Text2 = value; OnChanged(); OnPropertyChanged(nameof(Summary)); } } }
        public int NumberStart { get => _model.NumberStart; set { if (_model.NumberStart != value) { _model.NumberStart = value; OnChanged(); OnPropertyChanged(nameof(Summary)); } } }
        public int NumberStep { get => _model.NumberStep; set { if (_model.NumberStep != value) { _model.NumberStep = value; OnChanged(); } } }
        public int NumberPadding { get => _model.NumberPadding; set { if (_model.NumberPadding != value) { _model.NumberPadding = value; OnChanged(); } } }
        public int Position { get => _model.Position; set { if (_model.Position != value) { _model.Position = value; OnChanged(); } } }
        public RenameCaseMode CaseMode { get => _model.CaseMode; set { if (_model.CaseMode != value) { _model.CaseMode = value; OnChanged(); OnPropertyChanged(nameof(Summary)); } } }
        public bool UseRegex { get => _model.UseRegex; set { if (_model.UseRegex != value) { _model.UseRegex = value; OnChanged(); } } }
        public bool ReplaceFirstOnly { get => _model.ReplaceFirstOnly; set { if (_model.ReplaceFirstOnly != value) { _model.ReplaceFirstOnly = value; OnChanged(); } } }
        public bool IgnoreCase { get => _model.IgnoreCase; set { if (_model.IgnoreCase != value) { _model.IgnoreCase = value; OnChanged(); } } }

        public string DisplayName => Type switch
        {
            RenameRuleType.Prefix => "Prefixo",
            RenameRuleType.Suffix => "Sufixo",
            RenameRuleType.Replace => "Localizar / substituir",
            RenameRuleType.Numbering => "Numeração",
            RenameRuleType.Template => "Template",
            RenameRuleType.ChangeExtension => "Alterar extensão",
            RenameRuleType.ChangeCase => "Maiúsculas / minúsculas",
            RenameRuleType.Insert => "Inserir na posição",
            RenameRuleType.RemoveText => "Remover texto",
            RenameRuleType.RemoveAccents => "Remover acentos",
            RenameRuleType.RemoveSpecialCharacters => "Remover caracteres especiais",
            _ => Type.ToString()
        };

        public string Summary
        {
            get
            {
                switch (Type)
                {
                    case RenameRuleType.Replace: return (Text1 ?? "") + " → " + (Text2 ?? "");
                    case RenameRuleType.Numbering: return "Início " + NumberStart + " · " + (string.IsNullOrWhiteSpace(Text1) ? "{nome}_{numero}" : Text1);
                    case RenameRuleType.ChangeCase: return CaseMode.ToString();
                    case RenameRuleType.RemoveAccents: return "Normalizar caracteres acentuados";
                    case RenameRuleType.RemoveSpecialCharacters: return "Remover caracteres especiais";
                    default: return Text1 ?? string.Empty;
                }
            }
        }

        public bool ShowsText1 => Type != RenameRuleType.RemoveAccents && Type != RenameRuleType.RemoveSpecialCharacters && Type != RenameRuleType.ChangeCase;
        public bool ShowsText2 => Type == RenameRuleType.Replace;
        public bool ShowsNumbering => Type == RenameRuleType.Numbering;
        public bool ShowsPosition => Type == RenameRuleType.Insert;
        public bool ShowsCaseMode => Type == RenameRuleType.ChangeCase;
        public bool ShowsRegexOptions => Type == RenameRuleType.Replace || Type == RenameRuleType.RemoveText;
        public bool ShowsTemplateHelp => Type == RenameRuleType.Template || Type == RenameRuleType.Numbering;

        public event System.EventHandler Changed;
        private void OnChanged() => Changed?.Invoke(this, System.EventArgs.Empty);
        private void NotifyVisibility()
        {
            OnPropertyChanged(nameof(ShowsText1)); OnPropertyChanged(nameof(ShowsText2)); OnPropertyChanged(nameof(ShowsNumbering));
            OnPropertyChanged(nameof(ShowsPosition)); OnPropertyChanged(nameof(ShowsCaseMode)); OnPropertyChanged(nameof(ShowsRegexOptions)); OnPropertyChanged(nameof(ShowsTemplateHelp));
        }
    }
}
