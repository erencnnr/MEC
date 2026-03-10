using MEC.Application.Abstractions.Service.SchoolService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.School;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MEC.Application.Service.SchoolService
{
    public class AnnouncementService : IAnnouncementService
    {
        private readonly IGenericRepository<Announcement> _repository;

        // Dependency Injection ile Generic Repository'i içeri alıyoruz
        public AnnouncementService(IGenericRepository<Announcement> repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Announcement>> GetAllAnnouncementsAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<IEnumerable<Announcement>> GetActiveAnnouncementsAsync()
        {
            // IGenericRepository'deki predicate alanını kullanarak sadece IsActive = true olanları çekiyoruz
            return await _repository.GetAllAsync(x => x.IsActive);
        }

        public async Task<Announcement> GetAnnouncementByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task AddAnnouncementAsync(Announcement announcement)
        {
            announcement.CreatedDate = DateTime.Now; // Eklenme tarihini otomatik atıyoruz
            await _repository.AddAsync(announcement);
        }

        public async Task UpdateAnnouncementAsync(Announcement announcement)
        {
            announcement.UpdateDate = DateTime.Now; // Güncellenme tarihini otomatik atıyoruz
            _repository.Update(announcement);
        }

        public async Task DeleteAnnouncementAsync(int id)
        {
            var announcement = await _repository.GetByIdAsync(id);
            if (announcement != null)
            {
                _repository.Delete(announcement);
            }
        }
    }
}