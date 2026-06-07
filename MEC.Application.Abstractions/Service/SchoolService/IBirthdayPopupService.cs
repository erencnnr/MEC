using MEC.Application.Abstractions.Common.Models;
using MEC.Application.Abstractions.Service.SchoolService.Model;

namespace MEC.Application.Abstractions.Service.SchoolService
{
    public interface IBirthdayPopupService
    {
        Task<BirthdayPopupImageModel?> GetActiveBirthdayPopupImageAsync();
        Task<BirthdayPopupImageModel?> GetBirthdayPopupImageAsync(int id);
        Task<OperationResultModel> ReplaceBirthdayPopupImageAsync(BirthdayPopupImageCreateModel model);
        Task<OperationResultModel> DeleteBirthdayPopupImageMetadataAsync(int id);
        Task<bool> HasSeenBirthdayPopupAsync(int employeePortalId, int year);
        Task<OperationResultModel> MarkBirthdayPopupAsSeenAsync(int employeePortalId, int year);
    }
}
