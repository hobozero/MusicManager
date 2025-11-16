using Microsoft.Extensions.Options;
using SonosManagerApi.Config;
using SonosManagerApi.Models;
using SonosManagerAPI.Models;
using System.Text.Json;

namespace SonosManagerApi.Application
{
    public class PlaylistAdpater
    {   
        private NAS _nasConfig;

        public PlaylistAdpater(IOptions<NAS> nasConfig)
        {
            _nasConfig = nasConfig.Value;
        }

        public WFMUContent? LoadWFMUContent(Track sonosTrackLocation)
        {
            if (!sonosTrackLocation.Path.IsFile())
                return null;

            // Replace .mp4 with .json
            string jsonFilePath = Path.ChangeExtension(sonosTrackLocation.Path.ToUNCPath(), ".json");

            // Load and deserialize JSON file
            if (File.Exists(jsonFilePath))
            {
                string jsonContent = File.ReadAllText(jsonFilePath);
                return JsonSerializer.Deserialize<WFMUContent>(jsonContent);
            }

            return null;
        }

        public Track EnrichWithCurrentlyPlaying(Track sonosTrack)
        {

            sonosTrack.Title = "playlist not found";
            sonosTrack.Album = sonosTrack.FileName;
            sonosTrack.Artist = sonosTrack.Path.ParentFolder();

            if (!sonosTrack.Path.Contains("WFMU"))
            {
                return sonosTrack;
            }

            var wfmuPlaylist = LoadWFMUContent(sonosTrack);
            if (null == wfmuPlaylist)
            {
                return sonosTrack;
            }

            var playlistTrack = wfmuPlaylist.GetSegmentAtTime(sonosTrack.CurrentPlayTime);
            if (playlistTrack != null)
            {
                sonosTrack.Artist = playlistTrack.Artist;
                sonosTrack.Album = string.IsNullOrEmpty(sonosTrack.Album) ? sonosTrack.Title : sonosTrack.Album;
                sonosTrack.Title = playlistTrack.Title;
            }
            return sonosTrack;
        }
    }
}