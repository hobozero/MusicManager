using System.Diagnostics.Eventing.Reader;

namespace SonosManagerApi.Models
{
    public class Zone
    {
        public List<Device> Devices { get; set; } = new List<Device>();
    }

    public class Device
    {
        public bool ZoneLeader { get; set; } = false;
        public string IPAddress { get; set; } = "";
        public string RoomName { get; set; } = "";
        public string Location { get; set; } = "";
    }
}
