using Microsoft.AspNetCore.Mvc;
using MEC.Application.Abstractions.Service.EmployeeService;
using MEC.Domain.Entity.Employee;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MEC.AssetManagementUI.Controllers
{
    public class EmployeeController : Controller
    {
        // 1. TANIMLAMA: Servisleri burada tanımlıyoruz
        private readonly IEmployeeService _employeeService;
        private readonly IEmployeeTypeService _employeeTypeService; // <-- EKSİK OLAN KISIM BURASIYDI

        // 2. CONSTRUCTOR: Servisleri burada içeri alıyoruz (Dependency Injection)
        public EmployeeController(IEmployeeService employeeService, IEmployeeTypeService employeeTypeService)
        {
            _employeeService = employeeService;
            _employeeTypeService = employeeTypeService; // <-- VE BURADA EŞLEŞTİRİYORUZ
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Türleri artık yeni servisten çekiyoruz
            var types = await _employeeTypeService.GetEmployeeTypeListAsync();
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

        public async Task<IActionResult> Delete(int id)
        {
            await _employeeService.DeleteEmployeeAsync(id);
            return RedirectToAction("Index");
        }
    }
}
