using Microsoft.AspNetCore.Mvc.Formatters;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SonosManagerApi.Models
{
    public class StartTime
    {
        [JsonPropertyName("@attributes")]
        public Attributes Attributes { get; set; }
    }

    public class Attributes
    {
        [JsonPropertyName("msec")]
        public string Msec { get; set; }
    }

    public class Segment
    {
        [JsonPropertyName("startTime")]
        public JsonElement StartTimeElement 
        {
            get;
            set; 
        } 

        public TimeSpan? GetStartTime()
        {
            string? startTimeString = StartTimeElement.ValueKind == JsonValueKind.String ? StartTimeElement.GetString() : null;

            // If it's an object, check for "@attributes" -> "msec"
            if (StartTimeElement.ValueKind == JsonValueKind.Object &&
                StartTimeElement.TryGetProperty("@attributes", out var attributes) &&
                attributes.TryGetProperty("msec", out var msecElement))
            {
                startTimeString =  string.IsNullOrEmpty(msecElement.GetString()) ? null : msecElement.GetString();
            }

            return TimeSpan.TryParseExact(startTimeString, @"h\:mm\:ss", null, out var timeSpan) ? timeSpan : null;
        }

        public void SetStartTime(TimeSpan timeSpan)
        {
            JsonElement jsonElement = JsonSerializer.Deserialize<JsonElement>("\"" + timeSpan.ToString() + "\"");

            StartTimeElement = jsonElement;
        }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("artist")]
        public string Artist { get; set; }

    }

    public class PlaylistAttributes
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    public class Playlist
    {
        [JsonPropertyName("@attributes")]
        public PlaylistAttributes Attributes { get; set; }
    }

    public class AudioAttributes
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    public class Audio
    {
        [JsonPropertyName("@attributes")]
        public AudioAttributes Attributes { get; set; }
    }

    public class SegmentsAttributes
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    public class Segments
    {
        [JsonPropertyName("@attributes")]
        public SegmentsAttributes Attributes { get; set; }
    }

    public class InfoAttributes
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    public class Info
    {
        [JsonPropertyName("@attributes")]
        public InfoAttributes Attributes { get; set; }
    }

    public class RootAttributes
    {
        [JsonPropertyName("offset")]
        public string Offset { get; set; }
        [JsonPropertyName("shareSafe")]
        public string ShareSafe { get; set; }
        [JsonPropertyName("timestamps")]
        public string Timestamps { get; set; }
    }


    public class WFMUContentSingleSegment
    {
        [JsonPropertyName("segment")]
        public Segment Segment { get; set; }

        [JsonPropertyName("playlist")]
        public Playlist Playlist { get; set; }

        [JsonPropertyName("@attributes")]
        public RootAttributes Attributes { get; set; }

        [JsonPropertyName("audio")]
        public Audio Audio { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("segments")]
        public Segments Segments { get; set; }

        [JsonPropertyName("info")]
        public Info Info { get; set; }

        [JsonPropertyName("flash_server")]
        public string FlashServer { get; set; }

        [JsonPropertyName("mp4_server")]
        public string Mp4Server { get; set; }
    }

    public class WFMUContent
    {
        public static WFMUContent Create(WFMUContentSingleSegment wfmu)
        {
            return new WFMUContent()
            {
                Attributes = wfmu.Attributes,
                Audio = wfmu.Audio,
                FlashServer = wfmu.FlashServer,
                Info = wfmu.Info,
                Mp4Server = wfmu.Mp4Server,
                Playlist = wfmu.Playlist,
                Segment = new List<Segment>() { wfmu.Segment },
                Segments = wfmu.Segments,
                Title = wfmu.Title
            };
        }

        [JsonPropertyName("segment")]
        public List<Segment> Segment { get; set; }

        [JsonPropertyName("playlist")]
        public Playlist Playlist { get; set; }

        [JsonPropertyName("@attributes")]
        public RootAttributes Attributes { get; set; }

        [JsonPropertyName("audio")]
        public Audio Audio { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("segments")]
        public Segments Segments { get; set; }

        [JsonPropertyName("info")]
        public Info Info { get; set; }

        [JsonPropertyName("flash_server")]
        public string FlashServer { get; set; }

        [JsonPropertyName("mp4_server")]
        public string Mp4Server { get; set; }

        public Segment GetSegmentAtTime(TimeSpan targetTime)
        {
            if (Segment == null || Segment.Count == 0)
                return null;

            // Ensure segments are sorted by start time
            var orderedSegments = Segment.OrderBy(s => s.GetStartTime()).ToList();

            Segment previousSegment = null;

            foreach (var segment in orderedSegments)
            {
                if (segment.GetStartTime() > targetTime)
                    break; // Stop at the first segment that starts after targetTime

                previousSegment = segment; // Keep track of the last valid segment
            }

            return previousSegment;
        }

         public TimeSpan? Length(Segment segment)
            {
                var next = Next(segment);

                if (next == null){
                    return null;
                }

                return next.GetStartTime() - segment.GetStartTime();
            }

            public TimeSpan? EndTime(Segment segment)    {
            return segment.GetStartTime() + Length(segment);
        }

        public bool Exists(Segment segment) => Segment.IndexOf(segment) >= 0;

        public Segment Next(Segment segment)
        {
            var idx = Segment.IndexOf(segment);

                return (idx < Segment.Count - 1) ? Segment[idx + 1] : null;
        }

        public bool Remove(Segment segment)
        {
            var removedSegmentLength = Length(segment);
            if (removedSegmentLength is null || removedSegmentLength == TimeSpan.Zero)
                return false;

            bool legit = Segment.Remove(segment);
            if (!legit) return false;

            var next = Next(segment);

            while (next != null)
            {
                var startTime = next.GetStartTime() - removedSegmentLength;
                
                if (startTime is null) return false;

                next.SetStartTime(startTime.Value);
            }

            return true;
        }
    }



}