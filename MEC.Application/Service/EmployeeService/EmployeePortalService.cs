using MEC.Application.Abstractions.Service.EmployeeService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.Employee;

namespace MEC.Application.Service.EmployeeService
{
    public class EmployeePortalService : IEmployeePortalService
    {
        private readonly IGenericRepository<EmployeePortal> _repository;

        public EmployeePortalService(IGenericRepository<EmployeePortal> repository)
        {
            _repository = repository;
        }

        public async Task<EmployeePortal> GetProfileByEmailAsync(string email)
        {
            // GetAllAsync metodunuza filtre (predicate) göndererek sadece o maile ait kaydı istiyoruz
            var results = await _repository.GetAllAsync(x => x.Email == email && !x.IsDeleted);

            // Dönen liste içerisinden ilkini (veya null'u) alıyoruz
            return results.FirstOrDefault();
        }
    }
}
