using MEC.Application.Abstractions.Service.LoanService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.Asset;
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
        public async Task AddLoanAsync(Loan loan)
        {
            await _repository.AddAsync(loan);
        }
        public async Task UpdateLoanAsync(Loan loan)
        {
            _repository.Update(loan);
        }
        public async Task<List<Loan>> GetLoanListAsync()
        {
           var loans = await _repository.GetAllAsync();
           return loans.ToList();
        }
        public async Task<List<Loan>> GetLoansByAssetIdAsync(int assetId)
        {
            var loans = await _repository.GetAllAsync(
                x => x.AssetId == assetId,
                x => x.AssignedTo, // Personel adını görmek için Include
                x => x.LoanStatus  // Durumunu görmek için Include
            );

            return loans.OrderByDescending(x => x.LoanDate).ToList();
        }
        public async Task<bool> HasActiveLoanAsync(int assetId)
        {
            // ReturnDate'i boş olan (Hala zimmetli) kayıt var mı?
            var activeLoans = await _repository.GetAllAsync(x => x.AssetId == assetId && x.ReturnDate == null);
            return activeLoans.Any();
        }

        public async Task ReturnLoanAsync(int loanId, DateTime returnDate)
        {
            var loan = await _repository.GetByIdAsync(loanId);
            if (loan != null)
            {
                loan.ReturnDate = returnDate;
                // Not: Burada "İade Alındı" statüsünü bulup set etmek gerekir.
                // Şimdilik Servis içinde statik ID veya repo'dan isimle çekmek lazım.
                // Controller tarafında ID'yi bulup göndermek daha esnek olabilir.
                _repository.Update(loan);
            }
        }
    }
}
