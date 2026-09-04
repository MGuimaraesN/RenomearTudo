namespace RenomearTudo.Core.Models
{
    public class Mp3Metadata
    {
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public string Track { get; set; } = string.Empty;

        public bool HasAnyValue => !string.IsNullOrWhiteSpace(Title) || !string.IsNullOrWhiteSpace(Artist) || !string.IsNullOrWhiteSpace(Album);
    }
}
