using MEC.Domain.Entity.Loan;

namespace MEC.AssetManagementUI.Models.LoanModel
{
    public class ActiveLoanListViewModel
    {
        public List<Loan> ActiveLoans { get; set; } = new List<Loan>();
        public string? SearchTerm { get; set; }
    }
}
