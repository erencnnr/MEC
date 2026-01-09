using Microsoft.AspNetCore.Mvc;
using MEC.Application.Abstractions.Service.EmployeeService;
using MEC.Domain.Entity.Employee;
using System.Threading.Tasks;
using System.Collections.Generic;
using System; // Exception için gerekli

namespace MEC.AssetManagementUI.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly IEmployeeService _employeeService;
        private readonly IEmployeeTypeService _employeeTypeService;

        public EmployeeController(IEmployeeService employeeService, IEmployeeTypeService employeeTypeService)
        {
            _employeeService = employeeService;
            _employeeTypeService = employeeTypeService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search = null, string? filterStatus = null)
        {
            // Dropdown için tipleri çek
            var types = await _employeeTypeService.GetEmployeeTypeListAsync();
            ViewBag.EmployeeTypes = types;

            // Filtreleri View'da tekrar göstermek için sakla
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentFilter = filterStatus;

            // Filtre mantığı: Admin/User/Hepsi
            bool? isAdmin = null;
            if (filterStatus == "Admin") isAdmin = true;
            else if (filterStatus == "User") isAdmin = false;

            // Servise parametreleri ilet
            var values = await _employeeService.GetEmployeeListAsync(search, isAdmin);
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

        [HttpPost]
        public async Task<IActionResult> ToggleAdmin(int id)
        {
            await _employeeService.ToggleAdminStatusAsync(id);
            TempData["Success"] = "Kullanıcı yetkisi güncellendi.";
            return RedirectToAction("Index");
        }
    }
}