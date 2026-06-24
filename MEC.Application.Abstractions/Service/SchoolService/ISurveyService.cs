using MEC.Application.Abstractions.Common.Models;
using MEC.Application.Abstractions.Service.SchoolService.Model;

namespace MEC.Application.Abstractions.Service.SchoolService
{
    public interface ISurveyService
    {
        Task<List<AdminSurveyListItemModel>> GetAdminSurveysAsync(bool? isActive = null);
        Task<AdminSurveyDetailModel?> GetAdminSurveyDetailAsync(int id);
        Task<OperationResultModel<SurveyModel>> CreateSurveyAsync(SurveyCreateModel model);
        Task<OperationResultModel> SetSurveyActiveStateAsync(int id, bool isActive);
        Task<List<PublicSurveyListItemModel>> GetActiveSurveysForUserAsync(int employeePortalId);
        Task<PublicSurveyDetailModel?> GetActiveSurveyDetailForUserAsync(int id, int employeePortalId);
        Task<OperationResultModel> SubmitSurveyAnswerAsync(SurveyAnswerCreateModel model);
    }
}
