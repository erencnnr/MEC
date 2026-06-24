using MEC.Application.Abstractions.Service.EmployeeService;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Application.Abstractions.Service.SchoolService.Model;
using MEC.Domain.Common.Enum;
using MEC.Portal.Models;
using Microsoft.AspNetCore.Mvc;

namespace MEC.Portal.Controllers
{
    public class SurveysController : Controller
    {
        private readonly ISurveyService _surveyService;
        private readonly IEmployeePortalService _employeePortalService;

        public SurveysController(
            ISurveyService surveyService,
            IEmployeePortalService employeePortalService)
        {
            _surveyService = surveyService;
            _employeePortalService = employeePortalService;
        }

        [HttpGet("/Surveys")]
        public async Task<IActionResult> Index()
        {
            var portalUser = await GetCurrentPortalUserAsync();
            if (portalUser == null)
            {
                return Forbid();
            }

            var surveys = await _surveyService.GetActiveSurveysForUserAsync(portalUser.Id);
            return View(new SurveyListViewModel
            {
                Items = surveys.Select(MapSurveyListItem).ToList()
            });
        }

        [HttpGet("/Surveys/{id:int}")]
        public async Task<IActionResult> Detail(int id)
        {
            var portalUser = await GetCurrentPortalUserAsync();
            if (portalUser == null)
            {
                return Forbid();
            }

            var detail = await _surveyService.GetActiveSurveyDetailForUserAsync(id, portalUser.Id);
            if (detail == null)
            {
                return RedirectToAction(nameof(Index));
            }

            return View(MapSurveyDetail(detail));
        }

        [HttpPost("/Surveys/{id:int}/Answer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Answer(int id, List<SurveyQuestionAnswerInputViewModel> answers)
        {
            var portalUser = await GetCurrentPortalUserAsync();
            if (portalUser == null)
            {
                return Forbid();
            }

            var result = await _surveyService.SubmitSurveyAnswerAsync(new SurveyAnswerCreateModel
            {
                SurveyId = id,
                EmployeePortalId = portalUser.Id,
                Answers = (answers ?? new List<SurveyQuestionAnswerInputViewModel>())
                    .Select(answer => new SurveyQuestionAnswerCreateModel
                    {
                        SurveyQuestionId = answer.SurveyQuestionId,
                        RatingValue = answer.RatingValue,
                        SurveyQuestionOptionId = answer.SurveyQuestionOptionId
                    })
                    .ToList()
            });

            TempData[result.IsSuccess ? "SurveySuccess" : "SurveyError"] = result.Message;
            return RedirectToAction(nameof(Detail), new { id });
        }

        private async Task<MEC.Domain.Entity.Employee.EmployeePortal?> GetCurrentPortalUserAsync()
        {
            var email = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            return await _employeePortalService.GetActivePortalUserByEmailAsync(email);
        }

        private static SurveyListItemViewModel MapSurveyListItem(PublicSurveyListItemModel survey)
        {
            return new SurveyListItemViewModel
            {
                Id = survey.Id,
                Title = survey.Title,
                Description = survey.Description,
                QuestionCount = survey.QuestionCount,
                QuestionPreview = survey.QuestionPreview,
                HasAnswered = survey.HasAnswered,
                AnsweredAt = survey.AnsweredAt
            };
        }

        private static SurveyDetailViewModel MapSurveyDetail(PublicSurveyDetailModel survey)
        {
            return new SurveyDetailViewModel
            {
                Id = survey.Id,
                Title = survey.Title,
                Description = survey.Description,
                HasAnswered = survey.HasAnswered,
                AnsweredAt = survey.AnsweredAt,
                Questions = survey.Questions.Select(question => new SurveyQuestionViewModel
                {
                    Id = question.Id,
                    QuestionText = question.QuestionText,
                    Type = question.Type,
                    TypeLabel = GetSurveyTypeLabel(question.Type),
                    DisplayOrder = question.DisplayOrder,
                    Options = question.Options.Select(option => new SurveyOptionViewModel
                    {
                        Id = option.Id,
                        Text = option.Text,
                        DisplayOrder = option.DisplayOrder
                    }).ToList()
                }).ToList(),
                AnsweredQuestions = survey.AnsweredQuestions.Select(answer => new SurveyAnsweredQuestionViewModel
                {
                    SurveyQuestionId = answer.SurveyQuestionId,
                    QuestionText = answer.QuestionText,
                    TypeLabel = GetSurveyTypeLabel(answer.Type),
                    SelectedRatingValue = answer.SelectedRatingValue,
                    SelectedOptionText = answer.SelectedOptionText
                }).ToList()
            };
        }

        private static string GetSurveyTypeLabel(SurveyType type)
        {
            return type == SurveyType.MultipleChoice ? "Çoktan seçmeli" : "Puanlamalı";
        }
    }
}
