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
        public async Task<IActionResult> Index(string? search = null, string? filterStatus = null, int page = 1)
        {
            var types = await _employeeTypeService.GetEmployeeTypeListAsync();
            ViewBag.EmployeeTypes = types;

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentFilter = filterStatus;

            bool? isAdmin = null;
            if (filterStatus == "Admin") isAdmin = true;
            else if (filterStatus == "User") isAdmin = false;

            // BURADA YENİ METODU ÇAĞIRIYORUZ
            var pagedResult = await _employeeService.GetPagedEmployeeListAsync(search, isAdmin, page, 10);
            return View("EmployeeList", pagedResult);
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