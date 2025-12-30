using MEC.Application.Abstractions.Service.LoanService;
using MEC.Application.Abstractions.Service.LoanService.Model;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.Asset;
using MEC.Domain.Entity.Loan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
        public async Task<List<Loan>> GetLoanListAsync(LoanFilterRequestModel request)
        {
            // 1. FİLTRELEME (Predicate)
            Expression<Func<Loan, bool>> predicate = x =>
                // Arama Metni: Demirbaş, Seri No, Zimmet Alan Kişi
                (string.IsNullOrEmpty(request.SearchText) ||
                 x.Asset.Name.Contains(request.SearchText) ||
                 x.Asset.SerialNumber.Contains(request.SearchText) ||
                 (x.AssignedTo.FirstName + " " + x.AssignedTo.LastName).Contains(request.SearchText)) &&

                // Zimmet Alan Kişi Filtresi (AssignedTo)
                (request.AssignedToIds == null || !request.AssignedToIds.Any() || request.AssignedToIds.Contains(x.AssignedToId)) &&

                // Zimmet Veren Kişi Filtresi (CreatedBy - Opsiyonel)
                // Not: CreatedBy int olduğu varsayıldı.
                (request.AssignedByIds == null || !request.AssignedByIds.Any() || request.AssignedByIds.Contains((int)x.AssignedById));

            // Veriyi Çek
            // Not: Zimmeti veren kişinin adını göstermek için o tabloyu da joinlemek gerekebilir. 
            // Şimdilik sadece Asset ve AssignedTo include ediyoruz.
            var loans = await _repository.GetAllAsync(predicate, x => x.Asset, x => x.AssignedTo);

            // 2. SIRALAMA (Sorting)
            // IQueryable'a çevirip bellek içi sıralama yapıyoruz (Repo List dönüyorsa)
            var query = loans.AsQueryable();

            switch (request.SortOrder)
            {
                case "LoanDate_Asc":
                    query = query.OrderBy(x => x.LoanDate);
                    break;
                case "LoanDate_Desc":
                    query = query.OrderByDescending(x => x.LoanDate);
                    break;
                case "ReturnDate_Asc":
                    query = query.OrderBy(x => x.ReturnDate);
                    break;
                case "ReturnDate_Desc":
                    query = query.OrderByDescending(x => x.ReturnDate);
                    break;
                default:
                    query = query.OrderByDescending(x => x.LoanDate); // Varsayılan
                    break;
            }

            return query.ToList();
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
        public async Task BulkReturnLoansAsync(List<int> loanIds, DateTime returnDate)
        {
            // Sadece seçili ID'leri getir
            Expression<Func<Loan, bool>> predicate = x => loanIds.Contains(x.Id);
            var loans = await _repository.GetAllAsync(predicate);

            foreach (var loan in loans)
            {
                // Sadece iade edilmemişleri güncelle
                if (loan.ReturnDate == null)
                {
                    loan.ReturnDate = returnDate;
                    _repository.Update(loan);
                }
            }
            // Not: Generic Repository yapınızda SaveChanges yoksa burada context.SaveChanges() çağırmanız gerekebilir.
        }
    }
}
