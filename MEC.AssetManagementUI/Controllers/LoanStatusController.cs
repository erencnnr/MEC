using Microsoft.AspNetCore.Mvc;
using MEC.Application.Abstractions.Service.LoanService;
using MEC.Domain.Entity.Loan;
using System.Threading.Tasks;

namespace MEC.AssetManagementUI.Controllers
{
    public class LoanStatusController : Controller
    {
        private readonly ILoanStatusService _service;

        public LoanStatusController(ILoanStatusService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var list = await _service.GetLoanStatusListAsync();
            return View("LoanStatusList",list);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdate(LoanStatus loanStatus)
        {
            if (loanStatus.Id == 0)
            {
                await _service.CreateLoanStatusAsync(loanStatus);
            }
            else
            {
                await _service.UpdateLoanStatusAsync(loanStatus);
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int id)
        {
            var errorMessage = await _service.DeleteLoanStatusAsync(id);
            if (!string.IsNullOrEmpty(errorMessage))
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            return RedirectToAction("Index");
        }
    }
}
