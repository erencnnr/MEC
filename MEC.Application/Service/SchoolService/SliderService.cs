using MEC.Application.Abstractions.Common.Models;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Application.Abstractions.Service.SchoolService.Model;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.School;

namespace MEC.Application.Service.SchoolService
{
    public class SliderService : ISliderService
    {
        private readonly IGenericRepository<SliderImage> _sliderImageRepository;

        public SliderService(IGenericRepository<SliderImage> sliderImageRepository)
        {
            _sliderImageRepository = sliderImageRepository;
        }

        public async Task<List<SliderImageModel>> GetSliderImagesAsync()
        {
            return (await _sliderImageRepository.GetAllAsync())
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.CreatedDate)
                .Select(MapSliderImage)
                .ToList();
        }

        public async Task<OperationResultModel> AddSliderImageAsync(SliderImageCreateModel model)
        {
            var existingItems = (await _sliderImageRepository.GetAllAsync())
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.CreatedDate)
                .ToList();

            var nextDisplayOrder = existingItems.Count == 0 ? 1 : existingItems.Max(x => x.DisplayOrder) + 1;

            await _sliderImageRepository.AddAsync(new SliderImage
            {
                FileName = model.FileName,
                OriginalFileName = model.OriginalFileName,
                RelativePath = model.RelativePath,
                ContentType = model.ContentType,
                SizeBytes = model.SizeBytes,
                DisplayOrder = nextDisplayOrder,
                CreatedDate = DateTime.Now,
                UpdateDate = DateTime.Now
            });

            return OperationResultModel.Success();
        }

        public async Task<SliderImageModel?> GetSliderImageAsync(int id)
        {
            var item = await _sliderImageRepository.GetByIdAsync(id);
            return item == null ? null : MapSliderImage(item);
        }

        public async Task<OperationResultModel> DeleteSliderImageMetadataAsync(int id)
        {
            var item = await _sliderImageRepository.GetByIdAsync(id);
            if (item == null)
            {
                return OperationResultModel.Fail("Silinecek slider görseli bulunamadı.");
            }

            _sliderImageRepository.Delete(item);
            await NormalizeSliderOrderAsync();
            return OperationResultModel.Success("Slider görseli silindi.");
        }

        public async Task<OperationResultModel> ReorderSliderAsync(SliderReorderModel model)
        {
            if (model.OrderedIds.Count == 0)
            {
                return OperationResultModel.Fail("Geçerli bir slider sırası gönderilmedi.");
            }

            var items = (await _sliderImageRepository.GetAllAsync())
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.CreatedDate)
                .ToList();

            var itemById = items.ToDictionary(x => x.Id);
            var normalizedIds = model.OrderedIds.Where(itemById.ContainsKey).Distinct().ToList();
            if (normalizedIds.Count != items.Count)
            {
                return OperationResultModel.Fail("Slider sırası eksik veya hatalı.");
            }

            for (var index = 0; index < normalizedIds.Count; index++)
            {
                var item = itemById[normalizedIds[index]];
                item.DisplayOrder = index + 1;
                item.UpdateDate = DateTime.Now;
                _sliderImageRepository.Update(item);
            }

            return OperationResultModel.Success("Slider sırası güncellendi.");
        }

        private async Task NormalizeSliderOrderAsync()
        {
            var items = (await _sliderImageRepository.GetAllAsync())
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.CreatedDate)
                .ToList();

            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                var normalizedDisplayOrder = index + 1;
                if (item.DisplayOrder == normalizedDisplayOrder)
                {
                    continue;
                }

                item.DisplayOrder = normalizedDisplayOrder;
                item.UpdateDate = DateTime.Now;
                _sliderImageRepository.Update(item);
            }
        }

        private static SliderImageModel MapSliderImage(SliderImage image)
        {
            return new SliderImageModel
            {
                Id = image.Id,
                FileName = image.FileName,
                OriginalFileName = image.OriginalFileName,
                RelativePath = image.RelativePath,
                ContentType = image.ContentType,
                SizeBytes = image.SizeBytes,
                DisplayOrder = image.DisplayOrder,
                CreatedDate = image.CreatedDate
            };
        }
    }
}
