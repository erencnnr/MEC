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
        Task<List<Loan>> GetLoanListAsync();
    }
}
