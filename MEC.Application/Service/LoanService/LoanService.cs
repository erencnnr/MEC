using MEC.Application.Abstractions.Service.LoanService;
using MEC.Application.Abstractions.Service.LoanService.Model;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Common;
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
            var predicate = CreateFilterPredicate(request);

            // İlişkili tabloları dahil ederek çekiyoruz
            var loans = await _repository.GetAllAsync(predicate,
                x => x.Asset,
                x => x.AssignedTo,
                x => x.AssignedBy,
                x => x.LoanStatus);

            // Bellekte sıralama yapıp listeyi dönüyoruz
            return ApplySorting(loans, request.SortOrder).ToList();
        }

        // 2. SAYFALI LİSTE (LoanList Ekranı için)
        public async Task<PagedResult<Loan>> GetPagedLoanListAsync(LoanFilterRequestModel request)
        {
            var predicate = CreateFilterPredicate(request);

            // Tüm filtrelenmiş veriyi çek (Repo IQueryable dönmediği için)
            var loans = await _repository.GetAllAsync(predicate,
                x => x.Asset,
                x => x.AssignedTo,
                x => x.AssignedBy,
                x => x.LoanStatus);

            // Bellekte Sıralama
            var query = ApplySorting(loans, request.SortOrder);

            // Toplam kayıt sayısı
            int rowCount = query.Count();

            // Bellekte Sayfalama (Skip/Take)
            var pagedData = query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new PagedResult<Loan>
            {
                Results = pagedData,
                CurrentPage = request.Page,
                PageSize = request.PageSize,
                RowCount = rowCount,
                PageCount = (int)Math.Ceiling((double)rowCount / request.PageSize)
            };
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
        private Expression<Func<Loan, bool>> CreateFilterPredicate(LoanFilterRequestModel request)
        {
            return x =>
                // 1. Tab Filtresi (Aktif / Geçmiş)
                (string.IsNullOrEmpty(request.ActiveTab) ||
                 (request.ActiveTab == "active" && x.ReturnDate == null) ||
                 (request.ActiveTab == "history" && x.ReturnDate != null)) &&

                // 2. Arama Metni (Demirbaş Adı, Seri No, Personel Adı)
                (string.IsNullOrEmpty(request.SearchText) ||
                 (x.Asset != null && (x.Asset.Name.Contains(request.SearchText) || x.Asset.SerialNumber.Contains(request.SearchText))) ||
                 (x.AssignedTo != null && (x.AssignedTo.FirstName + " " + x.AssignedTo.LastName).Contains(request.SearchText))) &&

                // 3. Zimmet Alan Kişi Filtresi
                (request.AssignedToIds == null || !request.AssignedToIds.Any() || request.AssignedToIds.Contains(x.AssignedToId)) &&

                // 4. Zimmet Veren Kişi Filtresi (AssignedBy Nullable olduğu için kontrol ediyoruz)
                (request.AssignedByIds == null || !request.AssignedByIds.Any() || (x.AssignedById.HasValue && request.AssignedByIds.Contains(x.AssignedById.Value)));
        }

        private IEnumerable<Loan> ApplySorting(IEnumerable<Loan> query, string? sortOrder)
        {
            return sortOrder switch
            {
                "LoanDate_Asc" => query.OrderBy(x => x.LoanDate),
                "LoanDate_Desc" => query.OrderByDescending(x => x.LoanDate),
                "ReturnDate_Asc" => query.OrderBy(x => x.ReturnDate),
                "ReturnDate_Desc" => query.OrderByDescending(x => x.ReturnDate),
                _ => query.OrderByDescending(x => x.LoanDate), // Varsayılan: En yeni en üstte
            };
        }
    }
}
