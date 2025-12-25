using Microsoft.AspNetCore.Mvc;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Domain.Entity.School;
using System.Threading.Tasks;

namespace MEC.AssetManagementUI.Controllers
{
    public class SchoolController : Controller
    {
        private readonly ISchoolService _schoolService;

        public SchoolController(ISchoolService schoolService)
        {
            _schoolService = schoolService;
        }

        // BU METOT SAYFAYI AÇAR (Hatanın sebebi bu metodun View aramasıdır)
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var schools = await _schoolService.GetSchoolListAsync();
            return View("SchoolList",schools); // Bu satır Views/School/Index.cshtml dosyasını arar
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdate(School school)
        {
            if (school.Id == 0)
            {
                await _schoolService.CreateSchoolAsync(school);
            }
            else
            {
                await _schoolService.UpdateSchoolAsync(school);
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int id)
        {
            await _schoolService.DeleteSchoolAsync(id);
            return RedirectToAction("Index");
        }
    }
}
