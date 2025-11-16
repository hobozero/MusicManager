using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SonosManagerApi.Application;
using SonosManagerApi.Config;
using SonosManagerApi.Models;
using SonosManagerAPI.Models;
using System.Text;

namespace SonosManagerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrackController : ControllerBase
    {
        private SonosAdapter _sonosAdapter;
        private PlaylistAdpater _playlistAdapter;
        private FileAdapter _fileAdapter;

        public TrackController(SonosAdapter sonosAdapter, PlaylistAdpater playlistAdapter, FileAdapter fileAdapter)
        {
            _sonosAdapter = sonosAdapter;
            _playlistAdapter = playlistAdapter;
            _fileAdapter = fileAdapter;
        }

        [HttpGet("status/{ip}")]
        public async Task<ActionResult<TransportInfo>> GetTrackInfo([FromRoute] string ip)
        {
            var info = await _sonosAdapter.GetTransportInfo(ip);

            return Ok(info);
        }

        [HttpGet("{ip}")]
        public async Task<ActionResult<Track>> GetTrack([FromRoute] string ip)
        {
            var sonosTrack = await _sonosAdapter.ReadTrack(ip);

            var enrichedTrack = _playlistAdapter.EnrichWithCurrentlyPlaying(sonosTrack);

            return Ok(enrichedTrack);
        }

        [HttpPut("play/{ip}")]
        public async Task<ActionResult> PlayTrack([FromRoute] string ip)
        {
            await _sonosAdapter.PlayTrack(ip);

            return Ok();
        }

        [HttpPut("pause/{ip}")]
        public async Task<ActionResult> PauseTrack([FromRoute] string ip)
        {
            await _sonosAdapter.PauseTrack(ip);

            return Ok();
        }
            
        [HttpPut("skip/{ip}")]
        public async Task<ActionResult<Track>> SkipTrack([FromRoute] string ip)
        {
            var sonosTrack = await _sonosAdapter.ReadTrack(ip);

            if (sonosTrack.Path.Contains("WFMU"))
            {
                var wfmu = _playlistAdapter.LoadWFMUContent(sonosTrack);

                var currentSegment = wfmu?.GetSegmentAtTime(sonosTrack.CurrentPlayTime);
                var nextSegment = wfmu?.Next(currentSegment);

                if (nextSegment == null)
                {
                    await _sonosAdapter.Skip(ip);
                    sonosTrack = await _sonosAdapter.ReadTrack(ip);
                    return Ok(sonosTrack);
                }
                var nextTime = nextSegment.GetStartTime();
                if (nextTime == null)
                {
                    return BadRequest();
                }
                await _sonosAdapter.Seek(ip, nextTime.Value);
            }
            else
            {
                await _sonosAdapter.Skip(ip);
                sonosTrack = await _sonosAdapter.ReadTrack(ip);
            }

            var enrichedTrack = _playlistAdapter.EnrichWithCurrentlyPlaying(sonosTrack);
            return Ok(enrichedTrack);
        }

        [HttpPut("advance/{ip}")]
        public async Task<ActionResult<Track>> AdvanceTrack([FromRoute] string ip, [FromQuery] int secs)
        {
            var sonosTrack = await _sonosAdapter.ReadTrack(ip);
            await _sonosAdapter.Seek(ip, sonosTrack.CurrentPlayTime.Add(TimeSpan.FromSeconds(secs)));

            sonosTrack = await _sonosAdapter.ReadTrack(ip);
            var enrichedTrack = _playlistAdapter.EnrichWithCurrentlyPlaying(sonosTrack);

            return Ok(enrichedTrack);
        }

        [HttpPut("{ip}")]
        public async Task<IActionResult> SaveSubTrack([FromRoute] string ip)
        {
            var sonosTrack = await _sonosAdapter.ReadTrack(ip);

            if (sonosTrack.Path.Contains("WFMU"))
            {
                var wfmu = _playlistAdapter.LoadWFMUContent(sonosTrack);
                var segment = wfmu.GetSegmentAtTime(sonosTrack.CurrentPlayTime);

                if (!wfmu.Length(segment).HasValue)
                {
                    return BadRequest(new { message = "Could not determine track length. Probably a bad playlist." });
                }

                var trackRef = new TrackReference()
                {
                    FileLength = sonosTrack.TotalPlayTime,
                    SongLength = wfmu.Length(segment),
                    StartTime = segment.GetStartTime(),
                    TrackArtist = segment.Artist,
                    TrackTitle = segment.Title,
                    TrackPath = sonosTrack.Path
                };

                await _fileAdapter.SaveSong(trackRef);

                return Ok(new { message = $"{trackRef.ToFileName()} saved" });
            }

            return Ok(new { message = "Only WFMU supported" });
        }

        [HttpDelete("{ip}")]
        public async Task<ActionResult<string>> RemoveSubTrack([FromRoute] string ip)
        {
            var sonosTrack = await _sonosAdapter.ReadTrack(ip);

            if (!sonosTrack.Path.IsFile())
            {
                return BadRequest(new { message = "Not a file. Can't delete." });
            }

            if (sonosTrack.Path.Contains("WFMU"))
            {
                var wfmu = _playlistAdapter.LoadWFMUContent(sonosTrack);
                var segment = wfmu.GetSegmentAtTime(sonosTrack.CurrentPlayTime);

                var response = _fileAdapter.QueueDelete(
                    new TrackReference()
                    {
                        TrackPath = sonosTrack.Path.ToUNCPath(),
                        TrackTitle = segment.Title,
                        TrackArtist = segment.Artist,
                        StartTime = segment.GetStartTime().Value,
                        SongLength = wfmu.Length(segment),
                        FileLength = sonosTrack.TotalPlayTime
                    });

                await SkipTrack(ip);

                return Ok(response);
            }

            return Ok("Not WFMU. Can't delete track.");
        }
    }
}
