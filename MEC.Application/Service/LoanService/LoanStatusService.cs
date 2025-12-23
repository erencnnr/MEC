using MEC.Application.Abstractions.Service.LoanService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.Loan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEC.Application.Service.LoanService
{
    public class LoanStatusService : ILoanStatusService
    {
        private readonly IGenericRepository<LoanStatus> _repository;

        public LoanStatusService(IGenericRepository<LoanStatus> repository)
        {
            _repository = repository;
        }

        public async Task<List<LoanStatus>> GetLoanStatusListAsync()
        {
            var loanStatuses = await _repository.GetAllAsync();
            return loanStatuses.ToList();
        }
    }
}
