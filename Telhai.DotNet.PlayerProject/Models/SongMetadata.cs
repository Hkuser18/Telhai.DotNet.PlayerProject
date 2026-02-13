using System.Collections.Generic;

namespace Telhai.DotNet.HadarKeller.PlayerProject.Models
{
    public class SongMetadata
    {
        public string FilePath { get; set; } = string.Empty;
        public string? TrackName { get; set; }
        public string? ArtistName { get; set; }
        public string? AlbumName { get; set; }
        public string? ArtworkUrl { get; set; }
        public string? CustomTitle { get; set; }
        public List<string> ImagePaths { get; set; } = new();
    }
}
