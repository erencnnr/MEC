using Microsoft.AspNetCore.Mvc;
using MEC.Application.Abstractions.Service.EmployeeService;
using MEC.Domain.Entity.Employee; // <--- BU SATIR ÇOK ÖNEMLİ
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MEC.AssetManagementUI.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly IEmployeeService _employeeService;

        public EmployeeController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Eğer Adım 1'i doğru yaptıysanız, buradaki kırmızılık gitmeli.
            var types = await _employeeService.GetEmployeeTypesAsync();
            ViewBag.EmployeeTypes = types;

            var values = await _employeeService.GetEmployeeListAsync();
            return View("EmployeeList", values);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Employee employee)
        {
            await _employeeService.CreateEmployeeAsync(employee);
            return RedirectToAction("Index");
        }

        // YENİ EKLENEN ACTION:
        public async Task<IActionResult> Delete(int id)
        {
            await _employeeService.DeleteEmployeeAsync(id);
            return RedirectToAction("Index");
        }
    }
}
