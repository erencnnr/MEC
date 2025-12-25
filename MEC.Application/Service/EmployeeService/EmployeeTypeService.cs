using MEC.Application.Abstractions.Service.EmployeeService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.Employee;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MEC.Application.Service.EmployeeService
{
    public class EmployeeTypeService : IEmployeeTypeService
    {
        private readonly IGenericRepository<EmployeeType> _repository;

        public EmployeeTypeService(IGenericRepository<EmployeeType> repository)
        {
            _repository = repository;
        }

        public async Task<List<EmployeeType>> GetEmployeeTypeListAsync()
        {
            var types = await _repository.GetAllAsync();
            return types.ToList();
        }

        public async Task<EmployeeType> GetEmployeeTypeByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task CreateEmployeeTypeAsync(EmployeeType employeeType)
        {
            await _repository.AddAsync(employeeType);
        }

        public async Task UpdateEmployeeTypeAsync(EmployeeType employeeType)
        {
            _repository.Update(employeeType);
        }

        public async Task<string> DeleteEmployeeTypeAsync(int id)
        {
            try
            {
                var type = await _repository.GetByIdAsync(id);
                if (type != null)
                {
                    _repository.Delete(type);
                    // Hata yoksa null döner
                    return null;
                }
                return "Kayıt bulunamadı.";
            }
            catch (DbUpdateException)
            {
                // Yabancı anahtar hatası (Foreign Key) burada yakalanır
                return "Bu çalışan türü şu anda aktif personeller tarafından kullanıldığı için silinemez!";
            }
            catch (Exception ex)
            {
                return "Bir hata oluştu: " + ex.Message;
            }
        }
    }
}