using MEC.Application.Abstractions.Service.LoanService;
using MEC.AssetManagementUI.Models.LoanModel;
using Microsoft.AspNetCore.Mvc;

namespace MEC.AssetManagementUI.Controllers
{
    public class LoanController : Controller
    {
        private readonly ILoanService _loanService;

        public LoanController(ILoanService loanService)
        {
            _loanService = loanService;
        }

        [HttpGet]
        public async Task<IActionResult> ActiveLoans(string? searchTerm)
        {
            var loans = await _loanService.GetActiveLoansAsync(searchTerm);

            var model = new ActiveLoanListViewModel
            {
                ActiveLoans = loans,
                SearchTerm = searchTerm
            };

            return View(model);
        }

        // İade Al butonu için Action
        [HttpPost]
        public async Task<IActionResult> ReturnAsset(int id)
        {
            // İade işlemi bugünün tarihiyle yapılır
            await _loanService.ReturnLoanAsync(id, DateTime.Now);
            return RedirectToAction("ActiveLoans");
        }
    }
}