using MEC.Application.Abstractions.Common.Models;
using MEC.Application.Abstractions.Service.SchoolService.Model;

namespace MEC.Application.Abstractions.Service.SchoolService
{
    public interface IFoodMenuService
    {
        Task<List<FoodMenuMonthModel>> GetFoodMenuMonthsAsync();
        Task<FoodMenuMonthModel?> GetFoodMenuMonthAsync(int id);
        Task<FoodMenuMonthModel?> GetFoodMenuMonthByYearMonthAsync(int year, int month);
        Task<OperationResultModel<FoodMenuMonthModel>> EnsureDraftFoodMenuMonthAsync(int year, int month);
        Task<OperationResultModel<FoodMenuMonthModel>> ReplaceImportedFoodMenuAsync(FoodMenuImportModel model);
        Task<OperationResultModel> SaveFoodMenuDaysAsync(FoodMenuMonthDaySaveModel model);
        Task<OperationResultModel> PublishFoodMenuMonthAsync(int id);
        Task<OperationResultModel> DeleteFoodMenuMonthAsync(int id);
        Task<List<FoodMenuMonthModel>> GetPublishedFoodMenuMonthsAsync();
        Task<FoodMenuMonthModel?> GetLatestPublishedFoodMenuMonthAsync();
        Task<FoodMenuMonthModel?> GetPublishedFoodMenuMonthAsync(int id);
    }
}
