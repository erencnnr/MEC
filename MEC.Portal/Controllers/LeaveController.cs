using System;
using System.Globalization;
using System.Linq;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.Employee;
using MEC.Domain.Entity.Leave;
using MEC.Portal.Models;
using Microsoft.AspNetCore.Mvc;

namespace MEC.Portal.Controllers
{
    public class LeaveController : Controller
    {
        private static readonly string[] SupportedDateFormats = { "d.M.yyyy", "dd.MM.yyyy" };

        private readonly IGenericRepository<Leave> _leaveRepository;
        private readonly IGenericRepository<Employee> _employeeRepository;

        public LeaveController(
            IGenericRepository<Leave> leaveRepository,
            IGenericRepository<Employee> employeeRepository)
        {
            _leaveRepository = leaveRepository;
            _employeeRepository = employeeRepository;
        }

        [HttpGet]
        public IActionResult RequestLeave()
        {
            return View(new LeaveRequestViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestLeave(LeaveRequestViewModel model)
        {
            if (!TryParseDate(model.StartDate, out var startDate))
            {
                ModelState.AddModelError(nameof(model.StartDate), "Baslangic tarihi gecersiz.");
            }

            if (!TryParseDate(model.EndDate, out var endDate))
            {
                ModelState.AddModelError(nameof(model.EndDate), "Bitis tarihi gecersiz.");
            }

            if (ModelState.IsValid && endDate < startDate)
            {
                ModelState.AddModelError(nameof(model.EndDate), "Bitis tarihi baslangic tarihinden once olamaz.");
            }

            if (string.IsNullOrWhiteSpace(model.Reason))
            {
                ModelState.AddModelError(nameof(model.Reason), "Izin nedeni zorunludur.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userEmail = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = (await _employeeRepository.GetAllAsync(x => x.Email == userEmail && !x.IsDeleted)).FirstOrDefault();
            if (employee == null)
            {
                ModelState.AddModelError(string.Empty, "Kullanici kaydi bulunamadi.");
                return View(model);
            }

            var leaveRequest = new Leave
            {
                EmployeeId = employee.Id,
                StartDate = startDate,
                EndDate = endDate,
                Reason = model.Reason.Trim(),
                Status = 0,
                CreatedDate = DateTime.Now
            };

            await _leaveRepository.AddAsync(leaveRequest);

            TempData["LeaveSuccess"] = "Izin talebiniz basariyla gonderildi.";
            return RedirectToAction(nameof(RequestLeave));
        }

        private static bool TryParseDate(string? value, out DateTime date)
        {
            return DateTime.TryParseExact(
                value,
                SupportedDateFormats,
                CultureInfo.GetCultureInfo("tr-TR"),
                DateTimeStyles.None,
                out date);
        }
    }
}
