using MEC.Application.Abstractions.Common.Models;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Application.Abstractions.Service.SchoolService.Model;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.School;

namespace MEC.Application.Service.SchoolService
{
    public class BirthdayPopupService : IBirthdayPopupService
    {
        private readonly IGenericRepository<BirthdayPopupImage> _birthdayPopupImageRepository;
        private readonly IGenericRepository<BirthdayPopupView> _birthdayPopupViewRepository;

        public BirthdayPopupService(
            IGenericRepository<BirthdayPopupImage> birthdayPopupImageRepository,
            IGenericRepository<BirthdayPopupView> birthdayPopupViewRepository)
        {
            _birthdayPopupImageRepository = birthdayPopupImageRepository;
            _birthdayPopupViewRepository = birthdayPopupViewRepository;
        }

        public async Task<BirthdayPopupImageModel?> GetActiveBirthdayPopupImageAsync()
        {
            var item = (await _birthdayPopupImageRepository.GetAllAsync())
                .OrderByDescending(x => x.CreatedDate ?? DateTime.MinValue)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault();

            return item == null ? null : MapBirthdayPopupImage(item);
        }

        public async Task<BirthdayPopupImageModel?> GetBirthdayPopupImageAsync(int id)
        {
            var item = await _birthdayPopupImageRepository.GetByIdAsync(id);
            return item == null ? null : MapBirthdayPopupImage(item);
        }

        public async Task<OperationResultModel> ReplaceBirthdayPopupImageAsync(BirthdayPopupImageCreateModel model)
        {
            var existingItems = (await _birthdayPopupImageRepository.GetAllAsync()).ToList();
            foreach (var existingItem in existingItems)
            {
                _birthdayPopupImageRepository.Delete(existingItem);
            }

            await _birthdayPopupImageRepository.AddAsync(new BirthdayPopupImage
            {
                FileName = model.FileName,
                OriginalFileName = model.OriginalFileName,
                RelativePath = model.RelativePath,
                ContentType = model.ContentType,
                SizeBytes = model.SizeBytes,
                CreatedDate = DateTime.Now,
                UpdateDate = DateTime.Now
            });

            return OperationResultModel.Success("Doğum günü popup görseli güncellendi.");
        }

        public async Task<OperationResultModel> DeleteBirthdayPopupImageMetadataAsync(int id)
        {
            var item = await _birthdayPopupImageRepository.GetByIdAsync(id);
            if (item == null)
            {
                return OperationResultModel.Fail("Silinecek doğum günü popup görseli bulunamadı.");
            }

            _birthdayPopupImageRepository.Delete(item);
            return OperationResultModel.Success("Doğum günü popup görseli silindi.");
        }

        public async Task<bool> HasSeenBirthdayPopupAsync(int employeePortalId, int year)
        {
            return (await _birthdayPopupViewRepository.GetAllAsync(x => x.EmployeePortalId == employeePortalId && x.ShownYear == year))
                .Any();
        }

        public async Task<OperationResultModel> MarkBirthdayPopupAsSeenAsync(int employeePortalId, int year)
        {
            if (await HasSeenBirthdayPopupAsync(employeePortalId, year))
            {
                return OperationResultModel.Success();
            }

            await _birthdayPopupViewRepository.AddAsync(new BirthdayPopupView
            {
                EmployeePortalId = employeePortalId,
                ShownYear = year,
                ShownAt = DateTime.Now,
                CreatedDate = DateTime.Now,
                UpdateDate = DateTime.Now
            });

            return OperationResultModel.Success();
        }

        private static BirthdayPopupImageModel MapBirthdayPopupImage(BirthdayPopupImage image)
        {
            return new BirthdayPopupImageModel
            {
                Id = image.Id,
                FileName = image.FileName,
                OriginalFileName = image.OriginalFileName,
                RelativePath = image.RelativePath,
                ContentType = image.ContentType,
                SizeBytes = image.SizeBytes,
                CreatedDate = image.CreatedDate
            };
        }
    }
}
