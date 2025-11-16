namespace SonosManagerApi.Models
{
    public class TrackReference
    {
        public string TrackPath { get; set; }
        public string TrackTitle { get; set; }
        public string TrackArtist { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? SongLength { get; set; }
        public TimeSpan? FileLength { get; set; }

        public string ToFileName()
        {
            string targetFileName = $"{TrackTitle}-{TrackArtist}";

            char[] invalidChars = Path.GetInvalidFileNameChars();

            var fileName = new string(targetFileName
               .Select(c => invalidChars.Contains(c) ? '_' : c)
               .ToArray());

            return fileName;
        }
    }
}
