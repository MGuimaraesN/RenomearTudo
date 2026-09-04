using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RenomearTudo.Core.Services
{
    public static class FileNameSanitizer
    {
        private static readonly HashSet<string> ReservedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        public static string RemoveAccents(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (var c in normalized)
            {
                var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        public static string RemoveSpecialCharacters(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return new string(text.Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '_' || c == '-' || c == '.').ToArray());
        }

        public static bool TryValidateFileName(string fileName, out string reason)
        {
            reason = string.Empty;
            if (string.IsNullOrWhiteSpace(fileName))
            {
                reason = "Nome vazio";
                return false;
            }

            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                reason = "Contém caracteres inválidos";
                return false;
            }

            if (fileName.EndsWith(".", StringComparison.Ordinal) || fileName.EndsWith(" ", StringComparison.Ordinal))
            {
                reason = "Não pode terminar com ponto ou espaço";
                return false;
            }

            var baseName = Path.GetFileNameWithoutExtension(fileName).TrimEnd(' ', '.');
            if (ReservedNames.Contains(baseName))
            {
                reason = "Nome reservado pelo Windows";
                return false;
            }

            return true;
        }
    }
}
