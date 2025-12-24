using MEC.Application.Abstractions.Application;
using MEC.Domain.Entity.Loan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEC.Application.Abstractions.Service.LoanService
{
    public interface ILoanService : IApplicationService
    {
        Task AddLoanAsync(Loan loan);
        Task<List<Loan>> GetLoanListAsync();
        Task<List<Loan>> GetLoansByAssetIdAsync(int assetId);
        Task<bool> HasActiveLoanAsync(int assetId);
        Task ReturnLoanAsync(int loanId, DateTime returnDate);
        Task UpdateLoanAsync(Loan loan);
    }
}
