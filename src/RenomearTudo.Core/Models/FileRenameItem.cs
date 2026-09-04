using System;
using System.IO;

namespace RenomearTudo.Core.Models
{
    public enum RenameItemStatus
    {
        Ready,
        Unchanged,
        Conflict,
        Invalid,
        Error,
        Completed,
        Skipped
    }

    public class FileRenameItem
    {
        public FileRenameItem(string path)
        {
            OriginalPath = Path.GetFullPath(path);
            OriginalName = Path.GetFileName(OriginalPath);
            OriginalBaseName = Path.GetFileNameWithoutExtension(OriginalPath);
            OriginalExtension = Path.GetExtension(OriginalPath);
            DirectoryPath = Path.GetDirectoryName(OriginalPath) ?? string.Empty;
            PreviewName = OriginalName;
            NewPath = OriginalPath;
            Included = true;
            Status = RenameItemStatus.Unchanged;
            StatusMessage = "Sem alterações";

            var info = new FileInfo(OriginalPath);
            Size = info.Exists ? info.Length : 0;
            LastWriteTime = info.Exists ? info.LastWriteTime : DateTime.MinValue;
        }

        public bool Included { get; set; }
        public string OriginalPath { get; }
        public string DirectoryPath { get; }
        public string OriginalName { get; }
        public string OriginalBaseName { get; }
        public string OriginalExtension { get; }
        public string PreviewName { get; set; }
        public string ManualNameOverride { get; set; } = string.Empty;
        public string NewPath { get; set; }
        public long Size { get; }
        public DateTime LastWriteTime { get; }
        public RenameItemStatus Status { get; set; }
        public string StatusMessage { get; set; }
        public string PreviewError { get; set; } = string.Empty;
        public Mp3Metadata Metadata { get; set; }

        public bool IsChanged => !string.Equals(OriginalPath, NewPath, StringComparison.Ordinal);

        public string SizeDisplay
        {
            get
            {
                double value = Size;
                string[] units = { "B", "KB", "MB", "GB", "TB" };
                var unit = 0;
                while (value >= 1024 && unit < units.Length - 1)
                {
                    value /= 1024;
                    unit++;
                }
                return unit == 0 ? value.ToString("0") + " " + units[unit] : value.ToString("0.##") + " " + units[unit];
            }
        }
    }
}
