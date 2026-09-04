using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using RenomearTudo.App.Infrastructure;
using RenomearTudo.App.Models;
using RenomearTudo.Core.Models;
using RenomearTudo.Core.Services;
using WinForms = System.Windows.Forms;

namespace RenomearTudo.App.ViewModels
{
    public sealed class RuleTypeOption
    {
        public RuleTypeOption(string label, RenameRuleType type) { Label = label; Type = type; }
        public string Label { get; }
        public RenameRuleType Type { get; }
    }

    public sealed class CaseModeOption
    {
        public CaseModeOption(string label, RenameCaseMode mode) { Label = label; Mode = mode; }
        public string Label { get; }
        public RenameCaseMode Mode { get; }
    }

    public sealed class MainViewModel : ObservableObject
    {
        private readonly PersistenceService _persistence = new PersistenceService();
        private BindableRenameRule _selectedRule;
        private BindableFileItem _selectedFile;
        private string _searchText = string.Empty;
        private string _filterMode = "Todos";
        private string _sortMode = "Nome A-Z";
        private string _presetName = string.Empty;
        private PresetDefinition _selectedPreset;
        private bool _isBusy;
        private int _progressValue;
        private string _progressText = string.Empty;
        private CancellationTokenSource _cts;
        private readonly DispatcherTimer _previewTimer;

        public MainViewModel()
        {
            Files = new ObservableCollection<BindableFileItem>();
            Rules = new ObservableCollection<BindableRenameRule>();
            History = new ObservableCollection<OperationHistory>(_persistence.LoadHistory().OrderByDescending(h => h.Timestamp));
            Presets = new ObservableCollection<PresetDefinition>(_persistence.LoadPresets());
            FilesView = CollectionViewSource.GetDefaultView(Files);
            FilesView.Filter = FilterFile;
            _previewTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(140) };
            _previewTimer.Tick += (_, __) => { _previewTimer.Stop(); RefreshPreview(); };

            AddFilesCommand = new RelayCommand(AddFiles, () => !IsBusy);
            AddFolderCommand = new RelayCommand(AddFolder, () => !IsBusy);
            ClearCommand = new RelayCommand(Clear, () => !IsBusy && Files.Count > 0);
            RemoveSelectedCommand = new RelayCommand(RemoveSelected, () => !IsBusy && SelectedFile != null);
            AddRuleCommand = new RelayCommand(AddRule, () => !IsBusy);
            DeleteRuleCommand = new RelayCommand(DeleteRule, () => !IsBusy && SelectedRule != null);
            MoveRuleUpCommand = new RelayCommand(() => MoveRule(-1), () => !IsBusy && SelectedRule != null);
            MoveRuleDownCommand = new RelayCommand(() => MoveRule(1), () => !IsBusy && SelectedRule != null);
            RenameCommand = new RelayCommand(async () => await RenameAsync(), () => !IsBusy && ReadyCount > 0);
            CancelCommand = new RelayCommand(() => _cts?.Cancel(), () => IsBusy);
            UndoCommand = new RelayCommand(UndoLast, () => !IsBusy && History.Count > 0);
            ExportReportCommand = new RelayCommand(ExportReport, () => Files.Count > 0);
            ResetManualNameCommand = new RelayCommand(ResetManualName, () => !IsBusy && SelectedFile != null && !string.IsNullOrEmpty(SelectedFile.Model.ManualNameOverride));
            SavePresetCommand = new RelayCommand(SavePreset, () => !string.IsNullOrWhiteSpace(PresetName) && Rules.Count > 0);
            LoadPresetCommand = new RelayCommand(LoadPreset, () => SelectedPreset != null);
            DeletePresetCommand = new RelayCommand(DeletePreset, () => SelectedPreset != null);

            AddDefaultRule();
            RefreshPreview();
        }

        public ObservableCollection<BindableFileItem> Files { get; }
        public ObservableCollection<BindableRenameRule> Rules { get; }
        public ObservableCollection<OperationHistory> History { get; }
        public ObservableCollection<PresetDefinition> Presets { get; }
        public ICollectionView FilesView { get; }

        public IReadOnlyList<string> FilterModes { get; } = new[] { "Todos", "Alterados", "Conflitos", "Válidos", "Ignorados" };
        public IReadOnlyList<string> SortModes { get; } = new[] { "Nome A-Z", "Nome Z-A", "Data recente", "Tamanho maior", "Aleatório" };
        public IReadOnlyList<RuleTypeOption> RuleTypeOptions { get; } = new[]
        {
            new RuleTypeOption("Prefixo", RenameRuleType.Prefix),
            new RuleTypeOption("Sufixo", RenameRuleType.Suffix),
            new RuleTypeOption("Localizar / substituir", RenameRuleType.Replace),
            new RuleTypeOption("Numeração", RenameRuleType.Numbering),
            new RuleTypeOption("Template", RenameRuleType.Template),
            new RuleTypeOption("Alterar extensão", RenameRuleType.ChangeExtension),
            new RuleTypeOption("Maiúsculas / minúsculas", RenameRuleType.ChangeCase),
            new RuleTypeOption("Inserir na posição", RenameRuleType.Insert),
            new RuleTypeOption("Remover texto", RenameRuleType.RemoveText),
            new RuleTypeOption("Remover acentos", RenameRuleType.RemoveAccents),
            new RuleTypeOption("Remover caracteres especiais", RenameRuleType.RemoveSpecialCharacters)
        };

        public IReadOnlyList<CaseModeOption> CaseModeOptions { get; } = new[]
        {
            new CaseModeOption("MAIÚSCULAS", RenameCaseMode.Upper),
            new CaseModeOption("minúsculas", RenameCaseMode.Lower),
            new CaseModeOption("Título", RenameCaseMode.Title)
        };

        public ICommand AddFilesCommand { get; }
        public ICommand AddFolderCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand RemoveSelectedCommand { get; }
        public ICommand AddRuleCommand { get; }
        public ICommand DeleteRuleCommand { get; }
        public ICommand MoveRuleUpCommand { get; }
        public ICommand MoveRuleDownCommand { get; }
        public ICommand RenameCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand ExportReportCommand { get; }
        public ICommand ResetManualNameCommand { get; }
        public ICommand SavePresetCommand { get; }
        public ICommand LoadPresetCommand { get; }
        public ICommand DeletePresetCommand { get; }

        public BindableRenameRule SelectedRule
        {
            get => _selectedRule;
            set
            {
                if (SetProperty(ref _selectedRule, value)) RaiseCommands();
            }
        }

        public BindableFileItem SelectedFile
        {
            get => _selectedFile;
            set
            {
                if (!SetProperty(ref _selectedFile, value)) return;
                if (_selectedFile != null && _selectedFile.Model.Metadata == null && string.Equals(Path.GetExtension(_selectedFile.Model.OriginalPath), ".mp3", StringComparison.OrdinalIgnoreCase))
                {
                    _selectedFile.Model.Metadata = Id3v1Reader.Read(_selectedFile.Model.OriginalPath);
                    _selectedFile.Refresh();
                }
                RaiseCommands();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set { if (SetProperty(ref _searchText, value)) FilesView.Refresh(); }
        }

        public string FilterMode
        {
            get => _filterMode;
            set { if (SetProperty(ref _filterMode, value)) FilesView.Refresh(); }
        }

        public string SortMode
        {
            get => _sortMode;
            set { if (SetProperty(ref _sortMode, value)) ApplySort(); }
        }

        public string PresetName { get => _presetName; set { if (SetProperty(ref _presetName, value)) RaiseCommands(); } }
        public PresetDefinition SelectedPreset { get => _selectedPreset; set { if (SetProperty(ref _selectedPreset, value)) RaiseCommands(); } }
        public bool IsBusy { get => _isBusy; private set { if (SetProperty(ref _isBusy, value)) RaiseCommands(); } }
        public int ProgressValue { get => _progressValue; private set => SetProperty(ref _progressValue, value); }
        public string ProgressText { get => _progressText; private set => SetProperty(ref _progressText, value); }

        public int TotalCount => Files.Count;
        public int ConflictCount => Files.Count(f => f.Model.Included && (f.Model.Status == RenameItemStatus.Conflict || f.Model.Status == RenameItemStatus.Invalid));
        public int ReadyCount => Files.Count(f => f.Model.Included && f.Model.Status == RenameItemStatus.Ready && f.Model.IsChanged);
        public string Summary => TotalCount + " arquivos · " + ReadyCount + " prontos · " + ConflictCount + " conflitos";

        public void AddPaths(IEnumerable<string> paths)
        {
            var existing = new HashSet<string>(Files.Select(f => f.Model.OriginalPath), StringComparer.OrdinalIgnoreCase);
            var added = false;
            foreach (var input in paths ?? Enumerable.Empty<string>())
            {
                if (File.Exists(input))
                {
                    added |= AddOne(input, existing);
                }
                else if (Directory.Exists(input))
                {
                    try
                    {
                        foreach (var file in Directory.EnumerateFiles(input))
                            added |= AddOne(file, existing);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Não foi possível ler a pasta:\n" + ex.Message, "Renomear Tudo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            if (added) { ApplySort(); RefreshPreview(); }
        }

        public void MoveSelectedRuleTo(int newIndex)
        {
            if (SelectedRule == null) return;
            var old = Rules.IndexOf(SelectedRule);
            if (old < 0) return;
            newIndex = Math.Max(0, Math.Min(Rules.Count - 1, newIndex));
            if (old == newIndex) return;
            Rules.Move(old, newIndex);
            RefreshPreview();
        }

        private bool AddOne(string path, HashSet<string> existing)
        {
            var full = Path.GetFullPath(path);
            if (!existing.Add(full)) return false;
            var model = new FileRenameItem(full);
            var bindable = new BindableFileItem(model);
            bindable.IncludedChanged += (_, __) => SchedulePreview();
            bindable.PreviewEdited += (_, __) => ValidateOnly();
            Files.Add(bindable);
            return true;
        }

        private void AddFiles()
        {
            var dialog = new OpenFileDialog { Multiselect = true, Title = "Adicionar arquivos" };
            if (dialog.ShowDialog() == true) AddPaths(dialog.FileNames);
        }

        private void AddFolder()
        {
            using (var dialog = new WinForms.FolderBrowserDialog { Description = "Selecione uma pasta" })
            {
                if (dialog.ShowDialog() == WinForms.DialogResult.OK) AddPaths(new[] { dialog.SelectedPath });
            }
        }

        private void Clear()
        {
            Files.Clear();
            SelectedFile = null;
            RefreshPreview();
        }

        private void RemoveSelected()
        {
            if (SelectedFile == null) return;
            Files.Remove(SelectedFile);
            SelectedFile = null;
            RefreshPreview();
        }

        private void AddDefaultRule() => AddRule(RenameRuleType.Prefix);

        private void AddRule()
        {
            AddRule(RenameRuleType.Replace);
        }

        public void AddRule(RenameRuleType type)
        {
            var model = new RenameRule { Type = type };
            if (type == RenameRuleType.Numbering) model.Text1 = "{nome}_{numero}";
            if (type == RenameRuleType.Template) model.Text1 = "{nome}";
            var rule = new BindableRenameRule(model);
            rule.Changed += (_, __) => SchedulePreview();
            Rules.Add(rule);
            SelectedRule = rule;
            RefreshPreview();
        }

        private void DeleteRule()
        {
            if (SelectedRule == null) return;
            var index = Rules.IndexOf(SelectedRule);
            Rules.Remove(SelectedRule);
            SelectedRule = Rules.Count == 0 ? null : Rules[Math.Min(index, Rules.Count - 1)];
            RefreshPreview();
        }

        private void MoveRule(int delta)
        {
            if (SelectedRule == null) return;
            var index = Rules.IndexOf(SelectedRule);
            var target = index + delta;
            if (target < 0 || target >= Rules.Count) return;
            Rules.Move(index, target);
            RefreshPreview();
        }

        private void SchedulePreview()
        {
            _previewTimer.Stop();
            _previewTimer.Start();
        }

        private void RefreshPreview()
        {
            var models = Files.Select(f => f.Model).ToList();
            var rules = Rules.Select(r => r.Model).ToList();
            var needsMp3Metadata = RulesNeedMp3Metadata(rules);
            if (needsMp3Metadata)
            {
                foreach (var model in models.Where(m => m.Metadata == null && string.Equals(Path.GetExtension(m.OriginalPath), ".mp3", StringComparison.OrdinalIgnoreCase)))
                    model.Metadata = Id3v1Reader.Read(model.OriginalPath);
            }

            var includedTotal = models.Count(m => m.Included);
            var includedIndex = 0;
            for (var i = 0; i < models.Count; i++)
            {
                models[i].PreviewError = string.Empty;
                try
                {
                    var sequenceIndex = models[i].Included ? includedIndex++ : i;
                    var calculated = RenameEngine.BuildPreviewName(models[i], rules, sequenceIndex, includedTotal);
                    models[i].PreviewName = string.IsNullOrEmpty(models[i].ManualNameOverride) ? calculated : models[i].ManualNameOverride;
                }
                catch (Exception ex)
                {
                    models[i].PreviewName = models[i].OriginalName;
                    models[i].PreviewError = "Regra inválida: " + ex.Message;
                }
            }

            RenameEngine.ValidatePreview(models);
            foreach (var file in Files) file.Refresh();
            OnPropertyChanged(nameof(TotalCount)); OnPropertyChanged(nameof(ReadyCount)); OnPropertyChanged(nameof(ConflictCount)); OnPropertyChanged(nameof(Summary));
            FilesView.Refresh();
            RaiseCommands();
        }

        private void ValidateOnly()
        {
            RenameEngine.ValidatePreview(Files.Select(f => f.Model).ToList());
            foreach (var file in Files) file.Refresh();
            OnPropertyChanged(nameof(TotalCount)); OnPropertyChanged(nameof(ReadyCount)); OnPropertyChanged(nameof(ConflictCount)); OnPropertyChanged(nameof(Summary));
            FilesView.Refresh();
            RaiseCommands();
        }

        private void ResetManualName()
        {
            if (SelectedFile == null) return;
            SelectedFile.Model.ManualNameOverride = string.Empty;
            RefreshPreview();
        }

        private static bool RulesNeedMp3Metadata(IEnumerable<RenameRule> rules)
        {
            var tokens = new[] { "{artista}", "{titulo}", "{album}", "{ano}", "{genero}", "{faixa}" };
            return rules.Where(r => r.Enabled).Any(r => tokens.Any(t => (r.Text1 ?? string.Empty).IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private async Task RenameAsync()
        {
            RefreshPreview();
            if (ReadyCount == 0) return;

            var answer = MessageBox.Show("Serão renomeados " + ReadyCount + " arquivos.\n\nConflitos e nomes inválidos serão ignorados. Deseja continuar?",
                "Confirmar renomeação", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes) return;

            IsBusy = true;
            _cts = new CancellationTokenSource();
            ProgressValue = 0;
            ProgressText = "Preparando...";
            try
            {
                var models = Files.Select(f => f.Model).ToList();
                var progress = new Progress<RenameProgress>(p =>
                {
                    ProgressValue = p.Total == 0 ? 0 : (int)Math.Round(p.Current * 100d / p.Total);
                    ProgressText = p.Current + " / " + p.Total + " · " + p.CurrentFile;
                });
                var result = await RenameEngine.ExecuteAsync(models, _cts.Token, progress);

                if (result.Records.Count > 0)
                {
                    var folders = result.Records.Select(r => Path.GetDirectoryName(r.OldPath) ?? string.Empty).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    var history = new OperationHistory
                    {
                        Records = result.Records,
                        Result = result.Success ? "Concluído" : "Concluído com avisos",
                        Folder = folders.Count == 1 ? folders[0] : "Várias pastas"
                    };
                    History.Insert(0, history);
                    _persistence.SaveHistory(History);
                }

                if (result.Success)
                    MessageBox.Show(result.Records.Count + " arquivos renomeados com sucesso.", "Renomear Tudo", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show("A operação encontrou erros e executou rollback quando necessário:\n\n" + string.Join("\n", result.Errors.Take(8)), "Renomear Tudo", MessageBoxButton.OK, MessageBoxImage.Warning);

                ReloadExistingFiles();
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Operação cancelada. O mecanismo tenta manter os arquivos em estado consistente.", "Renomear Tudo", MessageBoxButton.OK, MessageBoxImage.Information);
                ReloadExistingFiles();
            }
            finally
            {
                _cts.Dispose(); _cts = null; IsBusy = false; ProgressText = string.Empty; ProgressValue = 0;
            }
        }

        private void ReloadExistingFiles()
        {
            var paths = Files.Select(f => File.Exists(f.Model.NewPath) ? f.Model.NewPath : f.Model.OriginalPath).Where(File.Exists).ToArray();
            Files.Clear();
            AddPaths(paths);
        }

        private void UndoLast()
        {
            if (History.Count == 0) return;
            var history = History[0];
            var answer = MessageBox.Show("Desfazer a última operação de " + history.Count + " arquivos?", "Desfazer", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes) return;

            var result = RenameEngine.Undo(history.Records);
            if (result.Success)
            {
                History.RemoveAt(0);
                _persistence.SaveHistory(History);
                RaiseCommands();
                MessageBox.Show("Última operação desfeita.", "Renomear Tudo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Não foi possível desfazer completamente:\n" + string.Join("\n", result.Errors.Take(8)), "Renomear Tudo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ExportReport()
        {
            var dialog = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = "RenomearTudo-relatorio.csv" };
            if (dialog.ShowDialog() != true) return;
            using (var writer = new StreamWriter(dialog.FileName, false, System.Text.Encoding.UTF8))
            {
                writer.WriteLine("Original;Novo;Status;Caminho");
                foreach (var file in Files)
                    writer.WriteLine(Csv(file.OriginalName) + ";" + Csv(file.PreviewName) + ";" + Csv(file.Status) + ";" + Csv(file.OriginalPath));
            }
        }

        private static string Csv(string value) => "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";

        private void SavePreset()
        {
            var name = PresetName.Trim();
            var current = Presets.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.CurrentCultureIgnoreCase));
            if (current == null)
            {
                current = new PresetDefinition { Name = name };
                Presets.Add(current);
            }
            current.Rules = Rules.Select(r => r.Model.Clone()).ToList();
            _persistence.SavePresets(Presets);
            SelectedPreset = current;
            OnPropertyChanged(nameof(Presets));
        }

        private void LoadPreset()
        {
            if (SelectedPreset == null) return;
            Rules.Clear();
            foreach (var model in SelectedPreset.Rules.Select(r => r.Clone()))
            {
                var rule = new BindableRenameRule(model);
                rule.Changed += (_, __) => SchedulePreview();
                Rules.Add(rule);
            }
            SelectedRule = Rules.FirstOrDefault();
            PresetName = SelectedPreset.Name;
            RefreshPreview();
        }

        private void DeletePreset()
        {
            if (SelectedPreset == null) return;
            Presets.Remove(SelectedPreset);
            SelectedPreset = null;
            _persistence.SavePresets(Presets);
        }

        private bool FilterFile(object obj)
        {
            var file = obj as BindableFileItem;
            if (file == null) return false;
            if (!string.IsNullOrWhiteSpace(SearchText) && file.OriginalName.IndexOf(SearchText, StringComparison.CurrentCultureIgnoreCase) < 0 && file.PreviewName.IndexOf(SearchText, StringComparison.CurrentCultureIgnoreCase) < 0)
                return false;
            switch (FilterMode)
            {
                case "Alterados": return file.Model.IsChanged;
                case "Conflitos": return file.Model.Status == RenameItemStatus.Conflict || file.Model.Status == RenameItemStatus.Invalid;
                case "Válidos": return file.Model.Status == RenameItemStatus.Ready;
                case "Ignorados": return !file.Model.Included;
                default: return true;
            }
        }

        private void ApplySort()
        {
            if (Files.Count < 2) return;
            IEnumerable<BindableFileItem> ordered = Files;
            switch (SortMode)
            {
                case "Nome Z-A": ordered = Files.OrderByDescending(f => f.OriginalName, StringComparer.CurrentCultureIgnoreCase); break;
                case "Data recente": ordered = Files.OrderByDescending(f => f.Model.LastWriteTime); break;
                case "Tamanho maior": ordered = Files.OrderByDescending(f => f.Model.Size); break;
                case "Aleatório": ordered = Files.OrderBy(_ => Guid.NewGuid()); break;
                default: ordered = Files.OrderBy(f => f.OriginalName, StringComparer.CurrentCultureIgnoreCase); break;
            }
            var list = ordered.ToList();
            Files.Clear();
            foreach (var item in list) Files.Add(item);
            RefreshPreview();
        }

        private void RaiseCommands()
        {
            foreach (var command in new[] { AddFilesCommand, AddFolderCommand, ClearCommand, RemoveSelectedCommand, AddRuleCommand, DeleteRuleCommand, MoveRuleUpCommand, MoveRuleDownCommand, RenameCommand, CancelCommand, UndoCommand, SavePresetCommand, LoadPresetCommand, DeletePresetCommand, ResetManualNameCommand, ExportReportCommand })
                (command as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }
}
