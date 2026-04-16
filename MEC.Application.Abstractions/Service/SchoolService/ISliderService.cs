using MEC.Application.Abstractions.Common.Models;
using MEC.Application.Abstractions.Service.SchoolService.Model;

namespace MEC.Application.Abstractions.Service.SchoolService
{
    public interface ISliderService
    {
        Task<List<SliderImageModel>> GetSliderImagesAsync();
        Task<OperationResultModel> AddSliderImageAsync(SliderImageCreateModel model);
        Task<SliderImageModel?> GetSliderImageAsync(int id);
        Task<OperationResultModel> DeleteSliderImageMetadataAsync(int id);
        Task<OperationResultModel> ReorderSliderAsync(SliderReorderModel model);
    }
}
