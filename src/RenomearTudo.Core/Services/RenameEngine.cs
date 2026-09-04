using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RenomearTudo.Core.Models;

namespace RenomearTudo.Core.Services
{
    public sealed class RenameProgress
    {
        public int Current { get; set; }
        public int Total { get; set; }
        public string CurrentFile { get; set; } = string.Empty;
    }

    public sealed class RenameExecutionResult
    {
        public RenameExecutionResult()
        {
            Records = new List<RenameRecord>();
            Errors = new List<string>();
        }

        public List<RenameRecord> Records { get; }
        public List<string> Errors { get; }
        public bool Success => Errors.Count == 0;
    }

    public static class RenameEngine
    {
        public static string BuildPreviewName(FileRenameItem item, IReadOnlyList<RenameRule> rules, int index, int total)
        {
            var baseName = item.OriginalBaseName;
            var extension = item.OriginalExtension;
            var metadata = item.Metadata ?? new Mp3Metadata();

            foreach (var rule in rules.Where(r => r.Enabled))
            {
                switch (rule.Type)
                {
                    case RenameRuleType.Prefix:
                        baseName = (rule.Text1 ?? string.Empty) + baseName;
                        break;
                    case RenameRuleType.Suffix:
                        baseName += rule.Text1 ?? string.Empty;
                        break;
                    case RenameRuleType.Replace:
                        baseName = Replace(baseName, rule.Text1, rule.Text2, rule.UseRegex, rule.ReplaceFirstOnly, rule.IgnoreCase);
                        break;
                    case RenameRuleType.Numbering:
                        var number = rule.NumberStart + (index * rule.NumberStep);
                        var numberText = number.ToString(new string('0', Math.Max(1, Math.Min(12, rule.NumberPadding))), CultureInfo.InvariantCulture);
                        var pattern = string.IsNullOrWhiteSpace(rule.Text1) ? "{nome}_{numero}" : rule.Text1;
                        baseName = ApplyTemplate(pattern, item, baseName, extension, metadata, numberText, total);
                        break;
                    case RenameRuleType.Template:
                        baseName = ApplyTemplate(rule.Text1, item, baseName, extension, metadata, (index + 1).ToString(CultureInfo.InvariantCulture), total);
                        break;
                    case RenameRuleType.ChangeExtension:
                        extension = NormalizeExtension(rule.Text1);
                        break;
                    case RenameRuleType.ChangeCase:
                        baseName = ApplyCase(baseName, rule.CaseMode);
                        break;
                    case RenameRuleType.Insert:
                        var position = Math.Max(0, Math.Min(baseName.Length, rule.Position));
                        baseName = baseName.Insert(position, rule.Text1 ?? string.Empty);
                        break;
                    case RenameRuleType.RemoveText:
                        baseName = Replace(baseName, rule.Text1, string.Empty, rule.UseRegex, rule.ReplaceFirstOnly, rule.IgnoreCase);
                        break;
                    case RenameRuleType.RemoveAccents:
                        baseName = FileNameSanitizer.RemoveAccents(baseName);
                        break;
                    case RenameRuleType.RemoveSpecialCharacters:
                        baseName = FileNameSanitizer.RemoveSpecialCharacters(baseName);
                        break;
                }
            }

            return baseName + extension;
        }

        public static void ValidatePreview(IList<FileRenameItem> items)
        {
            var candidates = items.Where(i => i.Included).ToList();
            foreach (var item in items)
            {
                item.NewPath = Path.Combine(item.DirectoryPath, item.PreviewName ?? string.Empty);
                if (!item.Included)
                {
                    item.Status = RenameItemStatus.Skipped;
                    item.StatusMessage = "Ignorado";
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(item.PreviewError))
                {
                    item.Status = RenameItemStatus.Invalid;
                    item.StatusMessage = item.PreviewError;
                    continue;
                }

                if (!FileNameSanitizer.TryValidateFileName(item.PreviewName, out var reason))
                {
                    item.Status = RenameItemStatus.Invalid;
                    item.StatusMessage = reason;
                    continue;
                }

                if (item.NewPath.Length >= 260)
                {
                    item.Status = RenameItemStatus.Invalid;
                    item.StatusMessage = "Caminho excede o limite compatível com Windows 7";
                    continue;
                }

                if (!item.IsChanged)
                {
                    item.Status = RenameItemStatus.Unchanged;
                    item.StatusMessage = "Sem alterações";
                    continue;
                }

                item.Status = RenameItemStatus.Ready;
                item.StatusMessage = "Pronto";
            }

            var duplicates = candidates
                .Where(i => i.Status == RenameItemStatus.Ready)
                .GroupBy(i => i.NewPath, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g)
                .ToList();

            foreach (var item in duplicates)
            {
                item.Status = RenameItemStatus.Conflict;
                item.StatusMessage = "Nome duplicado na prévia";
            }

            // Recalcula as origens móveis até estabilizar. Isso evita liberar A→B quando B não
            // será movido por causa de outro conflito na cadeia.
            while (true)
            {
                var movableSources = new HashSet<string>(candidates
                    .Where(i => i.Status == RenameItemStatus.Ready)
                    .Select(i => i.OriginalPath), StringComparer.OrdinalIgnoreCase);
                var changed = false;

                foreach (var item in candidates.Where(i => i.Status == RenameItemStatus.Ready).ToList())
                {
                    if (File.Exists(item.NewPath) && !movableSources.Contains(item.NewPath))
                    {
                        item.Status = RenameItemStatus.Conflict;
                        item.StatusMessage = "Já existe um arquivo com esse nome";
                        changed = true;
                    }
                }

                if (!changed) break;
            }
        }

        public static async Task<RenameExecutionResult> ExecuteAsync(
            IReadOnlyList<FileRenameItem> items,
            CancellationToken cancellationToken,
            IProgress<RenameProgress> progress = null)
        {
            return await Task.Run(() => Execute(items, cancellationToken, progress), cancellationToken).ConfigureAwait(false);
        }

        public static RenameExecutionResult Execute(
            IReadOnlyList<FileRenameItem> items,
            CancellationToken cancellationToken,
            IProgress<RenameProgress> progress = null)
        {
            var result = new RenameExecutionResult();
            var candidates = items.Where(i => i.Included && i.Status == RenameItemStatus.Ready && i.IsChanged).ToList();
            var staged = new List<Tuple<FileRenameItem, string>>();

            try
            {
                for (var i = 0; i < candidates.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var item = candidates[i];
                    progress?.Report(new RenameProgress { Current = i, Total = candidates.Count, CurrentFile = item.OriginalName });

                    var tempPath = BuildUniqueTemporaryPath(item.DirectoryPath);
                    File.Move(item.OriginalPath, tempPath);
                    staged.Add(Tuple.Create(item, tempPath));
                }

                for (var i = 0; i < staged.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var entry = staged[i];
                    File.Move(entry.Item2, entry.Item1.NewPath);
                    result.Records.Add(new RenameRecord { OldPath = entry.Item1.OriginalPath, NewPath = entry.Item1.NewPath });
                    progress?.Report(new RenameProgress { Current = i + 1, Total = staged.Count, CurrentFile = Path.GetFileName(entry.Item1.NewPath) });
                }
            }
            catch (OperationCanceledException)
            {
                Rollback(staged, result.Records, result.Errors);
                result.Records.Clear();
                throw;
            }
            catch (Exception ex)
            {
                result.Errors.Add(ex.Message);
                Rollback(staged, result.Records, result.Errors);
                result.Records.Clear();
            }

            return result;
        }

        public static RenameExecutionResult Undo(IReadOnlyList<RenameRecord> records)
        {
            var result = new RenameExecutionResult();
            var sourcePaths = new HashSet<string>(records.Select(r => r.NewPath), StringComparer.OrdinalIgnoreCase);

            foreach (var record in records)
            {
                if (!File.Exists(record.NewPath))
                    result.Errors.Add("Arquivo não encontrado para desfazer: " + record.NewPath);
                if (File.Exists(record.OldPath) && !sourcePaths.Contains(record.OldPath))
                    result.Errors.Add("Destino original já existe: " + record.OldPath);
            }

            if (!result.Success)
                return result;

            var staged = new List<Tuple<RenameRecord, string>>();
            try
            {
                foreach (var record in records.Reverse())
                {
                    var temp = BuildUniqueTemporaryPath(Path.GetDirectoryName(record.NewPath) ?? string.Empty);
                    File.Move(record.NewPath, temp);
                    staged.Add(Tuple.Create(record, temp));
                }

                foreach (var entry in staged)
                {
                    File.Move(entry.Item2, entry.Item1.OldPath);
                    result.Records.Add(new RenameRecord { OldPath = entry.Item1.NewPath, NewPath = entry.Item1.OldPath });
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(ex.Message);

                // Primeiro desfaz restaurações já finalizadas.
                foreach (var completed in result.Records.AsEnumerable().Reverse())
                {
                    try
                    {
                        if (File.Exists(completed.NewPath) && !File.Exists(completed.OldPath))
                            File.Move(completed.NewPath, completed.OldPath);
                    }
                    catch (Exception rollbackEx)
                    {
                        result.Errors.Add("Falha no rollback do desfazer: " + rollbackEx.Message);
                    }
                }

                // Depois devolve temporários que ainda não foram restaurados.
                foreach (var entry in staged.AsEnumerable().Reverse())
                {
                    try
                    {
                        if (File.Exists(entry.Item2) && !File.Exists(entry.Item1.NewPath))
                            File.Move(entry.Item2, entry.Item1.NewPath);
                    }
                    catch (Exception rollbackEx)
                    {
                        result.Errors.Add("Falha no rollback temporário do desfazer: " + rollbackEx.Message);
                    }
                }
                result.Records.Clear();
            }
            return result;
        }

        private static void Rollback(List<Tuple<FileRenameItem, string>> staged, List<RenameRecord> completed, List<string> errors)
        {
            foreach (var record in completed.AsEnumerable().Reverse())
            {
                try
                {
                    if (File.Exists(record.NewPath) && !File.Exists(record.OldPath))
                        File.Move(record.NewPath, record.OldPath);
                }
                catch (Exception ex)
                {
                    errors.Add("Falha no rollback: " + ex.Message);
                }
            }

            foreach (var entry in staged.AsEnumerable().Reverse())
            {
                try
                {
                    if (File.Exists(entry.Item2) && !File.Exists(entry.Item1.OriginalPath))
                        File.Move(entry.Item2, entry.Item1.OriginalPath);
                }
                catch (Exception ex)
                {
                    errors.Add("Falha no rollback temporário: " + ex.Message);
                }
            }
        }

        private static string BuildUniqueTemporaryPath(string directory)
        {
            string path;
            do
            {
                path = Path.Combine(directory, ".renomeartudo-" + Guid.NewGuid().ToString("N") + ".tmp");
            } while (File.Exists(path) || Directory.Exists(path));
            return path;
        }

        private static string Replace(string input, string find, string replacement, bool regex, bool firstOnly, bool ignoreCase)
        {
            input = input ?? string.Empty;
            find = find ?? string.Empty;
            replacement = replacement ?? string.Empty;
            if (string.IsNullOrEmpty(find)) return input;

            if (regex)
            {
                var options = RegexOptions.CultureInvariant | (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
                var expression = new Regex(find, options);
                return firstOnly ? expression.Replace(input, replacement, 1) : expression.Replace(input, replacement);
            }

            if (!ignoreCase)
            {
                if (!firstOnly) return input.Replace(find, replacement);
                var ordinalIndex = input.IndexOf(find, StringComparison.Ordinal);
                return ordinalIndex < 0 ? input : input.Substring(0, ordinalIndex) + replacement + input.Substring(ordinalIndex + find.Length);
            }

            var simpleRegex = new Regex(Regex.Escape(find), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            return firstOnly ? simpleRegex.Replace(input, _ => replacement, 1) : simpleRegex.Replace(input, _ => replacement);
        }

        private static string NormalizeExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension)) return string.Empty;
            return extension.StartsWith(".", StringComparison.Ordinal) ? extension : "." + extension;
        }

        private static string ApplyCase(string value, RenameCaseMode mode)
        {
            switch (mode)
            {
                case RenameCaseMode.Upper: return value.ToUpperInvariant();
                case RenameCaseMode.Lower: return value.ToLowerInvariant();
                case RenameCaseMode.Title: return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLower(CultureInfo.CurrentCulture));
                default: return value;
            }
        }

        private static string ApplyTemplate(string template, FileRenameItem item, string currentName, string extension, Mp3Metadata metadata, string number, int total)
        {
            var value = string.IsNullOrWhiteSpace(template) ? "{nome}" : template;
            var folder = new DirectoryInfo(item.DirectoryPath).Name;
            value = ReplaceToken(value, "{nome}", currentName);
            value = ReplaceToken(value, "{ext}", extension.TrimStart('.'));
            value = ReplaceToken(value, "{numero}", number);
            value = ReplaceToken(value, "{total}", total.ToString(CultureInfo.InvariantCulture));
            value = ReplaceToken(value, "{data}", item.LastWriteTime == DateTime.MinValue ? string.Empty : item.LastWriteTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            value = ReplaceToken(value, "{pasta}", folder);
            value = ReplaceToken(value, "{titulo}", metadata.Title);
            value = ReplaceToken(value, "{artista}", metadata.Artist);
            value = ReplaceToken(value, "{album}", metadata.Album);
            value = ReplaceToken(value, "{ano}", metadata.Year);
            value = ReplaceToken(value, "{genero}", metadata.Genre);
            value = ReplaceToken(value, "{faixa}", metadata.Track);
            return value;
        }

        private static string ReplaceToken(string input, string token, string value)
        {
            var tokenRegex = new Regex(Regex.Escape(token), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            return tokenRegex.Replace(input, _ => value ?? string.Empty);
        }
    }
}
