using MEC.Application.Abstractions.Service.LoanService;
using MEC.AssetManagementUI.Models.LoanModel;
using Microsoft.AspNetCore.Mvc;

namespace MEC.AssetManagementUI.Controllers
{
    public class ActiveLoanController : Controller
    {
        private readonly ILoanService _loanService;

        public ActiveLoanController(ILoanService loanService)
        {
            _loanService = loanService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? searchTerm)
        {
            // Servisteki metot aynı kalabilir, sadece çağıran yer değişti
            var loans = await _loanService.GetActiveLoansAsync(searchTerm);

            var model = new ActiveLoanListViewModel
            {
                ActiveLoans = loans,
                SearchTerm = searchTerm
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ReturnAsset(int id)
        {
            await _loanService.ReturnLoanAsync(id, DateTime.Now);
            return RedirectToAction("Index");
        }
    }
}