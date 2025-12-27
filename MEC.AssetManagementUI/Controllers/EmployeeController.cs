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
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Eksik veya hatalı bilgiler var, lütfen kontrol edin.";
                    return RedirectToAction("Index");
                }
                await _employeeService.CreateEmployeeAsync(employee);
                TempData["Success"] = "Çalışan başarıyla kaydedildi/güncellendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "İşlem başarısız oldu! Hata: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int id)
        {
            await _employeeService.DeleteEmployeeAsync(id);
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> Activate(int id)
        {
            await _employeeService.ActivateEmployeeAsync(id);
            return RedirectToAction("Index");
        }
    }
}
