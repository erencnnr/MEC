using MEC.Application.Abstractions.Service.LeaveService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.Leave;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq; // OrderByDescending ve ToList için gerekli
using System.Threading.Tasks;

namespace MEC.Application.Service.LeaveService
{
    public class LeaveService : ILeaveService
    {
        // İsim çakışmasını (namespace vs class) önlemek için tam yol kullanıyoruz
        private readonly IGenericRepository<MEC.Domain.Entity.Leave.Leave> _leaveRepository;

        public LeaveService(IGenericRepository<MEC.Domain.Entity.Leave.Leave> leaveRepository)
        {
            _leaveRepository = leaveRepository;
        }

        public async Task<List<MEC.Domain.Entity.Leave.Leave>> GetAllLeavesAsync()
        {
            // 1. HATA ÇÖZÜMÜ: Arayüzündeki metodun adı 'GetAll' değil 'GetAllAsync()'
            var leaves = await _leaveRepository.GetAllAsync();

            // IEnumerable sonucunu tarihe göre sıralayıp Listeye çeviriyoruz
            return leaves.OrderByDescending(x => x.CreatedDate).ToList();
        }

        public async Task<bool> UpdateLeaveStatusAsync(int leaveId, int status)
        {
            var leave = await _leaveRepository.GetByIdAsync(leaveId);
            if (leave == null) return false;

            leave.Status = status;

            // 2. HATA ÇÖZÜMÜ: IGenericRepository arayüzünde 'SaveAsync' metodu bulunmuyor
            // Ancak GenericRepository.Update metodun kendi içinde zaten '_context.SaveChanges()' çağırıyor
            // Bu yüzden harici bir kaydetme metoduna ihtiyacın yok.
            _leaveRepository.Update(leave);

            return true;
        }
    }
}