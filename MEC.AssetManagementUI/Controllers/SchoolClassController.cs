using Microsoft.AspNetCore.Mvc;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Domain.Entity.School;
using System.Threading.Tasks;

namespace MEC.AssetManagementUI.Controllers
{
    public class SchoolClassController : Controller
    {
        private readonly ISchoolClassService _schoolClassService;
        private readonly ISchoolService _schoolService;

        public SchoolClassController(ISchoolClassService schoolClassService, ISchoolService schoolService)
        {
            _schoolClassService = schoolClassService;
            _schoolService = schoolService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int schoolId)
        {
            // Okulun adını başlıkta göstermek için okulu çekiyoruz
            var school = await _schoolService.GetSchoolByIdAsync(schoolId);
            if (school == null) return RedirectToAction("Index", "School");

            ViewBag.SchoolName = school.Name;
            ViewBag.SchoolId = schoolId;

            // Sadece bu okula ait sınıfları getiriyoruz
            var classes = await _schoolClassService.GetSchoolClassesBySchoolIdAsync(schoolId);
            return View("SchoolClassList",classes);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdate(SchoolClass schoolClass)
        {
            if (schoolClass.Id == 0)
            {
                await _schoolClassService.CreateSchoolClassAsync(schoolClass);
            }
            else
            {
                await _schoolClassService.UpdateSchoolClassAsync(schoolClass);
            }

            // İşlem bitince o okulun sınıf listesine geri dön
            return RedirectToAction("Index", new { schoolId = schoolClass.SchoolId });
        }

        public async Task<IActionResult> Delete(int id)
        {
            // Silme işleminden sonra hangi okula döneceğimizi bilmek için önce sınıfı çekiyoruz
            var schoolClass = await _schoolClassService.GetSchoolClassByIdAsync(id);
            int schoolId = schoolClass.SchoolId;

            await _schoolClassService.DeleteSchoolClassAsync(id);
            return RedirectToAction("Index", new { schoolId = schoolId });
        }
    }
}
