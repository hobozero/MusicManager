using System.Web;

namespace SonosManagerAPI.Models
{
    public class Track
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Path { get; set; }

        public TimeSpan TotalPlayTime { get; set; }
        public TimeSpan CurrentPlayTime { get; set; }
        public string FileName { get; set; }

        public static Track CreateEmpty()
        {
            return new Track()
            {
                Title = "not available",
                Album = "not available",
                Artist = "not available",
                FileName = "not available",
                Path = "not available",
                TotalPlayTime = TimeSpan.FromSeconds(0),
                CurrentPlayTime = TimeSpan.FromSeconds(0)
            };
        }
    }
}