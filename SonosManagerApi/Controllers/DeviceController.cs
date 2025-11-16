using Microsoft.AspNetCore.Mvc;
using SonosManagerApi.Application;
using SonosManagerApi.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SonosManagerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceController : ControllerBase
    {
        private SonosDiscovery _discoverer;

        public DeviceController(SonosDiscovery discoverer)
        {
            _discoverer = discoverer;
        }

        [HttpGet]
        public async Task<IEnumerable<Device>> Get([FromQuery] bool reset = false)
        {
            var sonosDevices = await _discoverer.DiscoverSonosDevicesAsync(reset);

            return sonosDevices;
        }
    }
}
