using Microsoft.Extensions.Options;
using SonosManagerApi.Config;
using SonosManagerApi.Models;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace SonosManagerApi.Application
{
    public class FileAdapter
    {
        private NAS _nas;

        public FileAdapter(IOptions<NAS> nasConfig)
        {
            _nas = nasConfig.Value;
        }

        public async Task<string> ExecuteFFMpeg(string args)
        {
            // Execute command line to extract the song from the file
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            try
            {
                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                process.Start();

                // Read output and error asynchronously
                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();

                // Wait for the process to exit
                await process.WaitForExitAsync();

                string output = await outputTask;
                string error = await errorTask;

                if (process.ExitCode == 0)
                {
                    return $"";
                }
                else
                {
                    return $"FFmpeg Error: {error}";
                }
            }
            catch (Exception ex)
            {
                return $"Process execution failed: {ex.Message}";
            }
        }

        public async Task DeleteQueued()
        {
            TimeSpan? startTime = TimeSpan.Zero;
            TimeSpan? endTime = TimeSpan.Zero;

            var files = Directory.GetFiles(_nas.DeletePath);

            foreach (string trackRefPath in files)
            {
                string trackRefJson = File.ReadAllText(trackRefPath);

                var trackRef = JsonSerializer.Deserialize<TrackReference>(trackRefJson);

                var mp3Path = trackRef.TrackPath;

                var playlistPath = Path.ChangeExtension(mp3Path, ".json");
                WFMUContent wfmuPlaylist = null;

                // Load and deserialize JSON file
                if (File.Exists(playlistPath))
                {
                    string jsonContent = File.ReadAllText(playlistPath);
                    wfmuPlaylist = JsonSerializer.Deserialize<WFMUContent>(jsonContent);
                }

                // Just delete the file
                if (wfmuPlaylist.Segment.Count <= 1)
                {
                    System.IO.File.Delete(mp3Path);
                    System.IO.File.Delete(playlistPath);
                    System.IO.File.Delete(trackRefPath);
                    return;
                }

                startTime = trackRef.StartTime;
                string args = string.Empty;
                bool notFirstTrack = false;
                bool notLastTrack = false;
                var results = new StringBuilder();
               
                var startFile = $"\\\\{mp3Path.ToHost()}\\audio\\{Guid.NewGuid().ToString()}.mp4";
                var endFile = $"\\\\{mp3Path.ToHost()}\\audio\\{Guid.NewGuid().ToString()}.mp4";

                // Copy and work locally?
                // System.IO.File.Copy(sonosTrack.ToUNCPath(), sonosTrack.FileName);

                // We'll need the first part if the segment to remove isn't first
                var segment = wfmuPlaylist.Segment.FirstOrDefault(s => s.Artist == trackRef.TrackArtist && s.Title == trackRef.TrackTitle);
                if (segment == null)
                {
                    File.WriteAllText(_nas.LogPath, $"Couldn't find segment to delete {trackRef.TrackArtist} - {trackRef.TrackTitle} in {trackRef.TrackPath}");
                }
                int segmentPosition = wfmuPlaylist.Segment.IndexOf(segment);
                if (segmentPosition > 0)
                {
                    args = $"-i {mp3Path.ToUNCPath()} -ss 0 -to {startTime} -c copy {startFile}";
                    results.AppendLine(await ExecuteFFMpeg(args));
                    notFirstTrack = true;
                }

                // We'll need last part if the segment to remove isn't last
                if (segmentPosition < (wfmuPlaylist.Segment.Count() - 1))
                {
                    args = $"-i \"{ mp3Path }\" -ss {endTime} -to {trackRef.FileLength} -c copy {endFile}";
                    results.AppendLine(await ExecuteFFMpeg(args));
                    notLastTrack = true;
                }


                //System.IO.File.Move(sonosTrack.ToUNCPath(), $"\\\\{sonosTrack.ToHost()}\\audio\\deleted\\{sonosTrack.FileName}\"");

                // Write the edited File
                // Double negative - Is the first track, so just move the endFile
                if (notFirstTrack == false)
                {
                    System.IO.File.Move(endFile, mp3Path);
                    results.AppendLine("was the first file");
                }
                // Double negative - Is the last track, so just move the startFile 
                else if (notLastTrack == false)
                {
                    System.IO.File.Move(startFile, mp3Path);
                    results.AppendLine("was the last file");
                }
                // Somewhere in the middle concat the start and end files
                else
                {
                    args = $"-f concat - safe 0 -i < (echo \"file '{startFile}'\" && echo \"file '{endFile}'\") -c copy \"{mp3Path}\"";
                    results.AppendLine(await ExecuteFFMpeg(args));
                }

                var success = wfmuPlaylist.Remove(segment);
                if (!success)
                {
                    var wfmuPlaylistJson = JsonSerializer.Serialize(wfmuPlaylist);
                    File.WriteAllText(Path.ChangeExtension(mp3Path, ".json"), wfmuPlaylistJson);
                    File.WriteAllText(_nas.LogPath, "There was an issue. Probably timestamps in the playlist.");
                }

                System.IO.File.Delete(trackRefPath);
            }
        }

        public string QueueDelete(TrackReference trackToDelete)
        {
            var json = JsonSerializer.Serialize(trackToDelete);

            try
            {
                Directory.CreateDirectory(_nas.DeletePath);
                File.WriteAllText(Path.Combine(_nas.DeletePath, $"{trackToDelete.ToFileName()}-{Guid.NewGuid()}.json"), json);
            }
            catch (Exception ex)
            {

                return ex.Message;
            }
            return $"{trackToDelete.TrackTitle} - {trackToDelete.TrackArtist} queued for deletion for when track stops playing.";
        }

        public async Task SaveSong(TrackReference trackRef)
        {
            var targetPathed = $"{_nas.CuratedPath}{trackRef.ToFileName()}.mp3";

            string args = $"-i \"{trackRef.TrackPath}\" -ss {trackRef.StartTime} -t {trackRef.SongLength} \"{targetPathed}\"";

            var result = await ExecuteFFMpeg(args);
        }
    }
}
