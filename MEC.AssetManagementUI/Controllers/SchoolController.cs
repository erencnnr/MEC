using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Application.Service.SchoolService;
using Microsoft.AspNetCore.Mvc;

namespace MEC.AssetManagementUI.Controllers
{
    public class SchoolController : Controller
    {
        private readonly ISchoolService _schoolService;
        public SchoolController(ISchoolService schoolService)
        {
            _schoolService = schoolService;
        }

        [HttpGet]
        public async Task<IActionResult> GetSchools()
        {
            var schools = await _schoolService.GetSchoolListAsync();
            
            // Veriyi JSON formatına çevirip döner
            return Json(new { data = schools });
        }
    }
}
