using System.IO;

namespace SonosManagerApi.Config
{
    public class NAS
    {
        public string BaseIp { get; set; }

        public string LogPath => $"\\\\{BaseIp}\\audio\\log.txt";

        public string CuratedPath => $"\\\\{BaseIp}\\audio\\Curated\\";

        public string DeletePath => $"\\\\{BaseIp}\\audio\\delete\\";
    }
}
