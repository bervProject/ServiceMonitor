using Microsoft.AspNetCore.Mvc;

namespace ServiceMonitor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceMonitorController : ControllerBase
    {
        public ServiceMonitorController()
        {

        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Contacted");
        }
    }
}