using MEC.Application.Abstractions.Service.LoanService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.Loan;
using Microsoft.EntityFrameworkCore; // DbUpdateException için gerekli
using System;
using System.Collections.Generic;
using System.Linq;
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
            var list = await _repository.GetAllAsync();
            return list.ToList();
        }

        // --- YENİ EKLENEN METOTLAR ---

        public async Task<LoanStatus> GetLoanStatusByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task CreateLoanStatusAsync(LoanStatus loanStatus)
        {
            await _repository.AddAsync(loanStatus);
        }

        public async Task UpdateLoanStatusAsync(LoanStatus loanStatus)
        {
            _repository.Update(loanStatus);
        }

        public async Task<string> DeleteLoanStatusAsync(int id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity != null)
                {
                    _repository.Delete(entity);
                    return null; // Başarılı
                }
                return "Kayıt bulunamadı.";
            }
            catch (DbUpdateException)
            {
                return "Bu zimmet durumu şu anda kullanımda olduğu için silinemez!";
            }
            catch (Exception ex)
            {
                return "Hata oluştu: " + ex.Message;
            }
        }
    }
}