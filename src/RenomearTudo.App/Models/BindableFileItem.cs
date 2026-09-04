using RenomearTudo.App.Infrastructure;
using RenomearTudo.Core.Models;

namespace RenomearTudo.App.Models
{
    public sealed class BindableFileItem : ObservableObject
    {
        public BindableFileItem(FileRenameItem model) { Model = model; }
        public FileRenameItem Model { get; }

        public bool Included { get => Model.Included; set { if (Model.Included != value) { Model.Included = value; OnPropertyChanged(); IncludedChanged?.Invoke(this, System.EventArgs.Empty); } } }
        public string OriginalName => Model.OriginalName;
        public string OriginalPath => Model.OriginalPath;
        public string PreviewName
        {
            get => Model.PreviewName;
            set
            {
                if (Model.PreviewName == value) return;
                Model.ManualNameOverride = value ?? string.Empty;
                Model.PreviewName = value ?? string.Empty;
                OnPropertyChanged();
                PreviewEdited?.Invoke(this, System.EventArgs.Empty);
            }
        }
        public string Status => Model.StatusMessage;
        public RenameItemStatus StatusKind => Model.Status;
        public string Size => Model.SizeDisplay;
        public string Modified => Model.LastWriteTime == System.DateTime.MinValue ? "—" : Model.LastWriteTime.ToString("g");
        public Mp3Metadata Metadata => Model.Metadata;

        public event System.EventHandler IncludedChanged;
        public event System.EventHandler PreviewEdited;

        public void Refresh()
        {
            OnPropertyChanged(nameof(PreviewName));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(StatusKind));
            OnPropertyChanged(nameof(Metadata));
        }
    }
}
