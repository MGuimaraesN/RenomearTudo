using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RenomearTudo.Core.Models;

namespace RenomearTudo.Core.Services
{
    public static class Id3v1Reader
    {
        private static readonly string[] Genres = LoadGenres();

        public static Mp3Metadata Read(string path)
        {
            var result = new Mp3Metadata();
            if (!string.Equals(Path.GetExtension(path), ".mp3", StringComparison.OrdinalIgnoreCase))
                return result;

            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (stream.Length < 128)
                        return result;

                    stream.Seek(-128, SeekOrigin.End);
                    var buffer = new byte[128];
                    if (stream.Read(buffer, 0, buffer.Length) != buffer.Length)
                        return result;

                    var encoding = Encoding.GetEncoding(1252);
                    if (encoding.GetString(buffer, 0, 3) != "TAG")
                        return result;

                    result.Title = ReadField(encoding, buffer, 3, 30);
                    result.Artist = ReadField(encoding, buffer, 33, 30);
                    result.Album = ReadField(encoding, buffer, 63, 30);
                    result.Year = ReadField(encoding, buffer, 93, 4);
                    result.Comment = ReadField(encoding, buffer, 97, 28);
                    if (buffer[125] == 0 && buffer[126] != 0)
                        result.Track = buffer[126].ToString();

                    var genreIndex = buffer[127];
                    result.Genre = genreIndex < Genres.Length ? Genres[genreIndex] : genreIndex.ToString();
                }
            }
            catch
            {
                // Metadados são auxiliares; falha de leitura nunca impede o renomeio.
            }

            return result;
        }

        private static string ReadField(Encoding encoding, byte[] buffer, int offset, int count)
        {
            return encoding.GetString(buffer, offset, count).TrimEnd('\0', ' ');
        }

        private static string[] LoadGenres()
        {
            try
            {
                var assembly = typeof(Id3v1Reader).Assembly;
                using (var stream = assembly.GetManifestResourceStream("RenomearTudo.Core.Resources.GENEROS.TXT"))
                using (var reader = stream == null ? null : new StreamReader(stream, Encoding.GetEncoding(1252)))
                {
                    if (reader == null) return new string[0];
                    var values = new List<string>();
                    string line;
                    while ((line = reader.ReadLine()) != null)
                        values.Add(line.Trim());
                    return values.ToArray();
                }
            }
            catch
            {
                return new string[0];
            }
        }
    }
}
