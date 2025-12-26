using Microsoft.AspNetCore.Mvc;
using MEC.WebAPI.Models;

namespace MEC.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ImageController : ControllerBase
    {
        [HttpPost(Name = "UploadImage")]
        public IActionResult UploadImage(ImageRequestModel request)
        {
            ImageResponseModel response = new ImageResponseModel(); 
            response.Success = true;
            response.Message = "yüklenen dosya " + request.fileName;

            return Ok(response);
        }
    }
}
