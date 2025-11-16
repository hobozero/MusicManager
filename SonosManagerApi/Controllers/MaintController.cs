using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SonosManagerApi.Application;

namespace SonosManagerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaintController : ControllerBase
    {
        FileAdapter _fileAdapter;   

        public MaintController(FileAdapter fileAdapter)
        {
            _fileAdapter = fileAdapter;
        }

        [HttpDelete]
        public async Task<ActionResult<string>> CleanTracks()
        {
            var response = _fileAdapter.DeleteQueued();

            return Ok(new { message = "Only WFMU supported" });
        }
    }
}
