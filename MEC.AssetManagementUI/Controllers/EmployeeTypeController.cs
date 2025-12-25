using Microsoft.AspNetCore.Mvc;
using MEC.Application.Abstractions.Service.EmployeeService; // Namespace aynı kalabilir
using MEC.Domain.Entity.Employee;
using System.Threading.Tasks;

namespace MEC.AssetManagementUI.Controllers
{
    public class EmployeeTypeController : Controller
    {
        // Değişiklik Burda:
        private readonly IEmployeeTypeService _employeeTypeService;

        public EmployeeTypeController(IEmployeeTypeService employeeTypeService)
        {
            _employeeTypeService = employeeTypeService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Metot ismini yeni servise göre güncelledik (GetEmployeeTypeListAsync)
            var types = await _employeeTypeService.GetEmployeeTypeListAsync();
            return View("EmployeeTypeList",types);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdate(EmployeeType employeeType)
        {
            if (employeeType.Id == 0)
            {
                await _employeeTypeService.CreateEmployeeTypeAsync(employeeType);
            }
            else
            {
                await _employeeTypeService.UpdateEmployeeTypeAsync(employeeType);
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int id)
        {
            // Servisi çağırıp sonucu alıyoruz
            string errorMessage = await _employeeTypeService.DeleteEmployeeTypeAsync(id);

            if (!string.IsNullOrEmpty(errorMessage))
            {
                // Hata varsa TempData'ya atıyoruz
                TempData["ErrorMessage"] = errorMessage;
            }

            return RedirectToAction("Index");
            
        }
    }
}