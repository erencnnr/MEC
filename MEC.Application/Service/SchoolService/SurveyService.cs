using MEC.Application.Abstractions.Common.Models;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Application.Abstractions.Service.SchoolService.Model;
using MEC.DAL.Config.Contexts;
using MEC.Domain.Common.Enum;
using MEC.Domain.Entity.School;
using Microsoft.EntityFrameworkCore;

namespace MEC.Application.Service.SchoolService
{
    public class SurveyService : ISurveyService
    {
        private const int MaxMultipleChoiceOptions = 10;

        private readonly ApplicationDbContext _context;

        public SurveyService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AdminSurveyListItemModel>> GetAdminSurveysAsync(bool? isActive = null)
        {
            var surveys = await _context.Surveys
                .AsNoTracking()
                .Include(x => x.Questions)
                .Where(x => !isActive.HasValue || x.IsActive == isActive.Value)
                .OrderByDescending(x => x.IsActive)
                .ThenByDescending(x => x.CreatedDate ?? DateTime.MinValue)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            var responseCounts = await _context.SurveyResponses
                .AsNoTracking()
                .GroupBy(x => x.SurveyId)
                .Select(x => new { SurveyId = x.Key, Count = x.Count() })
                .ToDictionaryAsync(x => x.SurveyId, x => x.Count);

            var activePortalUserCount = await GetActivePortalUserCountAsync();

            return surveys.Select(x =>
            {
                var orderedQuestions = x.Questions
                    .OrderBy(question => question.DisplayOrder)
                    .ThenBy(question => question.Id)
                    .ToList();
                var answerCount = responseCounts.TryGetValue(x.Id, out var count) ? count : 0;

                return new AdminSurveyListItemModel
                {
                    Id = x.Id,
                    Title = x.Title,
                    Description = x.Description,
                    QuestionCount = orderedQuestions.Count,
                    QuestionPreview = orderedQuestions.FirstOrDefault()?.QuestionText ?? string.Empty,
                    IsActive = x.IsActive,
                    AnswerCount = answerCount,
                    ParticipationRate = CalculatePercentage(answerCount, activePortalUserCount),
                    CreatedDate = x.CreatedDate
                };
            }).ToList();
        }

        public async Task<AdminSurveyDetailModel?> GetAdminSurveyDetailAsync(int id)
        {
            var survey = await _context.Surveys
                .AsNoTracking()
                .Include(x => x.Questions)
                    .ThenInclude(x => x.Options)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (survey == null)
            {
                return null;
            }

            var responses = await _context.SurveyResponses
                .AsNoTracking()
                .Include(x => x.Answers)
                .Where(x => x.SurveyId == id)
                .ToListAsync();

            var orderedQuestions = survey.Questions
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ToList();
            var activePortalUserCount = await GetActivePortalUserCountAsync();
            var answerCount = responses.Count;

            return new AdminSurveyDetailModel
            {
                Id = survey.Id,
                Title = survey.Title,
                Description = survey.Description,
                IsActive = survey.IsActive,
                CreatedDate = survey.CreatedDate,
                Questions = orderedQuestions.Select(MapSurveyQuestion).ToList(),
                QuestionResults = orderedQuestions.Select(question => BuildAdminQuestionResult(question, responses, answerCount)).ToList(),
                AnswerCount = answerCount,
                ActivePortalUserCount = activePortalUserCount,
                ParticipationRate = CalculatePercentage(answerCount, activePortalUserCount)
            };
        }

        public async Task<OperationResultModel<SurveyModel>> CreateSurveyAsync(SurveyCreateModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Title))
            {
                return OperationResultModel<SurveyModel>.Fail("Anket basligi zorunludur.");
            }

            var normalizedQuestions = NormalizeQuestions(model.Questions);
            if (normalizedQuestions.Count == 0)
            {
                return OperationResultModel<SurveyModel>.Fail("En az 1 soru giriniz.");
            }

            var validationMessage = ValidateQuestions(normalizedQuestions);
            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                return OperationResultModel<SurveyModel>.Fail(validationMessage);
            }

            var survey = new Survey
            {
                Title = model.Title.Trim(),
                Description = (model.Description ?? string.Empty).Trim(),
                IsActive = model.IsActive,
                CreatedDate = DateTime.Now,
                UpdateDate = DateTime.Now,
                Questions = normalizedQuestions.Select((question, questionIndex) => new SurveyQuestion
                {
                    QuestionText = question.QuestionText.Trim(),
                    Type = question.Type,
                    DisplayOrder = questionIndex + 1,
                    CreatedDate = DateTime.Now,
                    UpdateDate = DateTime.Now,
                    Options = question.Type == SurveyType.MultipleChoice
                        ? question.Options
                            .Where(option => !string.IsNullOrWhiteSpace(option))
                            .Select((option, optionIndex) => new SurveyQuestionOption
                            {
                                Text = option.Trim(),
                                DisplayOrder = optionIndex + 1,
                                CreatedDate = DateTime.Now,
                                UpdateDate = DateTime.Now
                            })
                            .ToList()
                        : new List<SurveyQuestionOption>()
                }).ToList()
            };

            await _context.Surveys.AddAsync(survey);
            await _context.SaveChangesAsync();

            return OperationResultModel<SurveyModel>.Success(
                new SurveyModel
                {
                    Id = survey.Id,
                    Title = survey.Title,
                    Description = survey.Description,
                    IsActive = survey.IsActive,
                    CreatedDate = survey.CreatedDate,
                    Questions = survey.Questions
                        .OrderBy(x => x.DisplayOrder)
                        .ThenBy(x => x.Id)
                        .Select(MapSurveyQuestion)
                        .ToList()
                },
                "Anket olusturuldu.");
        }

        public async Task<OperationResultModel> SetSurveyActiveStateAsync(int id, bool isActive)
        {
            var survey = await _context.Surveys.FirstOrDefaultAsync(x => x.Id == id);
            if (survey == null)
            {
                return OperationResultModel.Fail("Anket bulunamadi.");
            }

            survey.IsActive = isActive;
            survey.UpdateDate = DateTime.Now;
            await _context.SaveChangesAsync();

            return OperationResultModel.Success(isActive ? "Anket aktif hale getirildi." : "Anket pasif hale getirildi.");
        }

        public async Task<List<PublicSurveyListItemModel>> GetActiveSurveysForUserAsync(int employeePortalId)
        {
            var surveys = await _context.Surveys
                .AsNoTracking()
                .Include(x => x.Questions)
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.CreatedDate ?? DateTime.MinValue)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            var responses = await _context.SurveyResponses
                .AsNoTracking()
                .Where(x => x.EmployeePortalId == employeePortalId)
                .ToListAsync();

            return surveys.Select(x =>
            {
                var orderedQuestions = x.Questions
                    .OrderBy(question => question.DisplayOrder)
                    .ThenBy(question => question.Id)
                    .ToList();
                var response = responses.FirstOrDefault(item => item.SurveyId == x.Id);

                return new PublicSurveyListItemModel
                {
                    Id = x.Id,
                    Title = x.Title,
                    Description = x.Description,
                    QuestionCount = orderedQuestions.Count,
                    QuestionPreview = orderedQuestions.FirstOrDefault()?.QuestionText ?? string.Empty,
                    HasAnswered = response != null,
                    AnsweredAt = response?.AnsweredAt
                };
            }).ToList();
        }

        public async Task<PublicSurveyDetailModel?> GetActiveSurveyDetailForUserAsync(int id, int employeePortalId)
        {
            var survey = await _context.Surveys
                .AsNoTracking()
                .Include(x => x.Questions)
                    .ThenInclude(x => x.Options)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (survey == null || !survey.IsActive)
            {
                return null;
            }

            var response = await _context.SurveyResponses
                .AsNoTracking()
                .Include(x => x.Answers)
                    .ThenInclude(x => x.SurveyQuestionOption)
                .Where(x => x.SurveyId == id && x.EmployeePortalId == employeePortalId)
                .FirstOrDefaultAsync();

            var orderedQuestions = survey.Questions
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ToList();

            return new PublicSurveyDetailModel
            {
                Id = survey.Id,
                Title = survey.Title,
                Description = survey.Description,
                HasAnswered = response != null,
                AnsweredAt = response?.AnsweredAt,
                Questions = orderedQuestions.Select(question => new PublicSurveyQuestionModel
                {
                    Id = question.Id,
                    QuestionText = question.QuestionText,
                    Type = question.Type,
                    DisplayOrder = question.DisplayOrder,
                    Options = question.Options
                        .OrderBy(option => option.DisplayOrder)
                        .ThenBy(option => option.Id)
                        .Select(MapSurveyQuestionOption)
                        .ToList()
                }).ToList(),
                AnsweredQuestions = response?.Answers
                    .OrderBy(answer => orderedQuestions.FindIndex(question => question.Id == answer.SurveyQuestionId))
                    .Select(answer =>
                    {
                        var question = orderedQuestions.First(x => x.Id == answer.SurveyQuestionId);
                        return new PublicSurveyAnsweredQuestionModel
                        {
                            SurveyQuestionId = question.Id,
                            QuestionText = question.QuestionText,
                            Type = question.Type,
                            SelectedRatingValue = answer.RatingValue,
                            SelectedOptionId = answer.SurveyQuestionOptionId,
                            SelectedOptionText = answer.SurveyQuestionOption?.Text ?? string.Empty
                        };
                    })
                    .ToList() ?? new List<PublicSurveyAnsweredQuestionModel>()
            };
        }

        public async Task<OperationResultModel> SubmitSurveyAnswerAsync(SurveyAnswerCreateModel model)
        {
            var survey = await _context.Surveys
                .Include(x => x.Questions)
                    .ThenInclude(x => x.Options)
                .FirstOrDefaultAsync(x => x.Id == model.SurveyId);
            if (survey == null || !survey.IsActive)
            {
                return OperationResultModel.Fail("Cevaplanacak aktif anket bulunamadi.");
            }

            var alreadyAnswered = await _context.SurveyResponses
                .AnyAsync(x => x.SurveyId == model.SurveyId && x.EmployeePortalId == model.EmployeePortalId);
            if (alreadyAnswered)
            {
                return OperationResultModel.Fail("Bu anketi zaten cevapladiniz.");
            }

            var orderedQuestions = survey.Questions
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ToList();
            var answers = model.Answers ?? new List<SurveyQuestionAnswerCreateModel>();
            var submittedQuestionIds = answers.Select(x => x.SurveyQuestionId).ToList();

            if (orderedQuestions.Count == 0)
            {
                return OperationResultModel.Fail("Cevaplanacak soru bulunamadi.");
            }

            if (submittedQuestionIds.Count != orderedQuestions.Count || submittedQuestionIds.Distinct().Count() != orderedQuestions.Count)
            {
                return OperationResultModel.Fail("Tum sorular cevaplanmalidir.");
            }

            if (orderedQuestions.Any(question => !submittedQuestionIds.Contains(question.Id)))
            {
                return OperationResultModel.Fail("Tum sorular cevaplanmalidir.");
            }

            var response = new SurveyResponse
            {
                SurveyId = survey.Id,
                EmployeePortalId = model.EmployeePortalId,
                AnsweredAt = DateTime.Now,
                CreatedDate = DateTime.Now,
                UpdateDate = DateTime.Now,
                Answers = new List<SurveyResponseAnswer>()
            };

            foreach (var question in orderedQuestions)
            {
                var answer = answers.First(x => x.SurveyQuestionId == question.Id);

                if (question.Type == SurveyType.Rating)
                {
                    if (!answer.RatingValue.HasValue || answer.RatingValue.Value < 1 || answer.RatingValue.Value > 5)
                    {
                        return OperationResultModel.Fail("Tum puanlama sorulari icin 1 ile 5 arasinda secim yapiniz.");
                    }

                    response.Answers.Add(new SurveyResponseAnswer
                    {
                        SurveyQuestionId = question.Id,
                        RatingValue = answer.RatingValue.Value,
                        SurveyQuestionOptionId = null,
                        CreatedDate = DateTime.Now,
                        UpdateDate = DateTime.Now
                    });
                    continue;
                }

                if (!answer.SurveyQuestionOptionId.HasValue)
                {
                    return OperationResultModel.Fail("Tum coktan secmeli sorular icin bir secenek seciniz.");
                }

                var option = question.Options.FirstOrDefault(x => x.Id == answer.SurveyQuestionOptionId.Value);
                if (option == null)
                {
                    return OperationResultModel.Fail("Secilen anket secenegi gecersiz.");
                }

                response.Answers.Add(new SurveyResponseAnswer
                {
                    SurveyQuestionId = question.Id,
                    RatingValue = null,
                    SurveyQuestionOptionId = option.Id,
                    CreatedDate = DateTime.Now,
                    UpdateDate = DateTime.Now
                });
            }

            try
            {
                await _context.SurveyResponses.AddAsync(response);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return OperationResultModel.Fail("Bu anketi zaten cevapladınız.");
            }

            return OperationResultModel.Success("Anket cevabınız kaydedildi.");
        }

        private async Task<int> GetActivePortalUserCountAsync()
        {
            return await _context.EmployeePortals.CountAsync(x => !x.IsDeleted);
        }

        private static List<SurveyQuestionCreateModel> NormalizeQuestions(IEnumerable<SurveyQuestionCreateModel>? questions)
        {
            return (questions ?? Enumerable.Empty<SurveyQuestionCreateModel>())
                .Where(question => question != null)
                .Select(question => new SurveyQuestionCreateModel
                {
                    Type = question.Type,
                    QuestionText = question.QuestionText ?? string.Empty,
                    Options = question.Type == SurveyType.MultipleChoice
                        ? (question.Options ?? new List<string>())
                        .Select(option => option ?? string.Empty)
                        .ToList()
                        : new List<string>()
                })
                .ToList();
        }

        private static string? ValidateQuestions(List<SurveyQuestionCreateModel> questions)
        {
            foreach (var question in questions)
            {
                if (string.IsNullOrWhiteSpace(question.QuestionText))
                {
                    return "Her soru için soru metni zorunludur.";
                }

                if (question.Type != SurveyType.MultipleChoice)
                {
                    continue;
                }

                var normalizedOptions = question.Options
                    .Where(option => !string.IsNullOrWhiteSpace(option))
                    .Select(option => option.Trim())
                    .ToList();

                if (normalizedOptions.Count < 2)
                {
                    return "Çoktan seçmeli sorular için en az 2 seçenek giriniz.";
                }

                if (normalizedOptions.Count > MaxMultipleChoiceOptions)
                {
                    return "Çoktan seçmeli sorularda en fazla 10 seçenek olabilir.";
                }
            }

            return null;
        }

        private static AdminSurveyQuestionResultModel BuildAdminQuestionResult(SurveyQuestion question, List<SurveyResponse> responses, int answerCount)
        {
            var relatedAnswers = responses
                .SelectMany(response => response.Answers)
                .Where(answer => answer.SurveyQuestionId == question.Id)
                .ToList();

            var result = new AdminSurveyQuestionResultModel
            {
                Id = question.Id,
                QuestionText = question.QuestionText,
                Type = question.Type,
                DisplayOrder = question.DisplayOrder,
                Options = question.Options
                    .OrderBy(option => option.DisplayOrder)
                    .ThenBy(option => option.Id)
                    .Select(MapSurveyQuestionOption)
                    .ToList()
            };

            if (question.Type == SurveyType.Rating)
            {
                var ratingAnswers = relatedAnswers
                    .Where(answer => answer.RatingValue.HasValue)
                    .Select(answer => answer.RatingValue!.Value)
                    .ToList();

                result.AverageRating = ratingAnswers.Count == 0
                    ? null
                    : Math.Round((decimal)ratingAnswers.Average(), 2);

                result.RatingDistribution = Enumerable.Range(1, 5)
                    .Select(rating =>
                    {
                        var count = ratingAnswers.Count(value => value == rating);
                        return new SurveyRatingDistributionItemModel
                        {
                            RatingValue = rating,
                            Count = count,
                            Percentage = CalculatePercentage(count, answerCount)
                        };
                    })
                    .ToList();

                return result;
            }

            result.OptionResults = question.Options
                .OrderBy(option => option.DisplayOrder)
                .ThenBy(option => option.Id)
                .Select(option =>
                {
                    var count = relatedAnswers.Count(answer => answer.SurveyQuestionOptionId == option.Id);
                    return new SurveyOptionResultModel
                    {
                        OptionId = option.Id,
                        Text = option.Text,
                        DisplayOrder = option.DisplayOrder,
                        Count = count,
                        Percentage = CalculatePercentage(count, answerCount)
                    };
                })
                .ToList();

            return result;
        }

        private static SurveyQuestionModel MapSurveyQuestion(SurveyQuestion question)
        {
            return new SurveyQuestionModel
            {
                Id = question.Id,
                QuestionText = question.QuestionText,
                Type = question.Type,
                DisplayOrder = question.DisplayOrder,
                Options = question.Options
                    .OrderBy(option => option.DisplayOrder)
                    .ThenBy(option => option.Id)
                    .Select(MapSurveyQuestionOption)
                    .ToList()
            };
        }

        private static SurveyQuestionOptionModel MapSurveyQuestionOption(SurveyQuestionOption option)
        {
            return new SurveyQuestionOptionModel
            {
                Id = option.Id,
                Text = option.Text,
                DisplayOrder = option.DisplayOrder
            };
        }

        private static decimal CalculatePercentage(int count, int total)
        {
            if (count <= 0 || total <= 0)
            {
                return 0m;
            }

            return Math.Round((decimal)count * 100m / total, 1);
        }
    }
}
