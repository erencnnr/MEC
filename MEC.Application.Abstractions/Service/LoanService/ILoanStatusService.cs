using MEC.Application.Abstractions.Application;
using MEC.Domain.Entity.Loan;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MEC.Application.Abstractions.Service.LoanService
{
    public interface ILoanStatusService : IApplicationService
    {
       
        Task<List<LoanStatus>> GetLoanStatusListAsync();

        
        Task<LoanStatus> GetLoanStatusByIdAsync(int id);
        Task CreateLoanStatusAsync(LoanStatus loanStatus);
        Task UpdateLoanStatusAsync(LoanStatus loanStatus);

        
        Task<string> DeleteLoanStatusAsync(int id);
    }
}