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
    public class LoanService : ILoanService
    {
        private readonly IGenericRepository<Loan> _repository;

        public LoanService(IGenericRepository<Loan> repository)
        {
            _repository = repository;
        }

        public async Task<List<Loan>> GetLoanListAsync()
        {
           var loans = await _repository.GetAllAsync();
           return loans.ToList();
        }
    }
}
