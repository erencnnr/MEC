using Microsoft.AspNetCore.Mvc;

namespace MEC.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet(Name = "test")]
        public string Get()
        {
            return "Ok";
        }
    }
}
