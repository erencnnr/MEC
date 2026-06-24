using MEC.Domain.Common.Enum;

namespace MEC.Application.Abstractions.Service.SchoolService.Model
{
    public class SurveyModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? CreatedDate { get; set; }
        public List<SurveyQuestionModel> Questions { get; set; } = new();
    }

    public class SurveyQuestionModel
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public SurveyType Type { get; set; }
        public int DisplayOrder { get; set; }
        public List<SurveyQuestionOptionModel> Options { get; set; } = new();
    }

    public class SurveyQuestionOptionModel
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    public class SurveyCreateModel
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<SurveyQuestionCreateModel> Questions { get; set; } = new();
    }

    public class SurveyQuestionCreateModel
    {
        public string QuestionText { get; set; } = string.Empty;
        public SurveyType Type { get; set; }
        public List<string> Options { get; set; } = new();
    }

    public class AdminSurveyListItemModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
        public string QuestionPreview { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int AnswerCount { get; set; }
        public decimal ParticipationRate { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class AdminSurveyDetailModel : SurveyModel
    {
        public int AnswerCount { get; set; }
        public int ActivePortalUserCount { get; set; }
        public decimal ParticipationRate { get; set; }
        public List<AdminSurveyQuestionResultModel> QuestionResults { get; set; } = new();
    }

    public class AdminSurveyQuestionResultModel
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public SurveyType Type { get; set; }
        public int DisplayOrder { get; set; }
        public List<SurveyQuestionOptionModel> Options { get; set; } = new();
        public decimal? AverageRating { get; set; }
        public List<SurveyRatingDistributionItemModel> RatingDistribution { get; set; } = new();
        public List<SurveyOptionResultModel> OptionResults { get; set; } = new();
    }

    public class SurveyRatingDistributionItemModel
    {
        public int RatingValue { get; set; }
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    public class SurveyOptionResultModel
    {
        public int OptionId { get; set; }
        public string Text { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    public class PublicSurveyListItemModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
        public string QuestionPreview { get; set; } = string.Empty;
        public bool HasAnswered { get; set; }
        public DateTime? AnsweredAt { get; set; }
    }

    public class PublicSurveyDetailModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool HasAnswered { get; set; }
        public DateTime? AnsweredAt { get; set; }
        public List<PublicSurveyQuestionModel> Questions { get; set; } = new();
        public List<PublicSurveyAnsweredQuestionModel> AnsweredQuestions { get; set; } = new();
    }

    public class PublicSurveyQuestionModel
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public SurveyType Type { get; set; }
        public int DisplayOrder { get; set; }
        public List<SurveyQuestionOptionModel> Options { get; set; } = new();
    }

    public class PublicSurveyAnsweredQuestionModel
    {
        public int SurveyQuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public SurveyType Type { get; set; }
        public int? SelectedRatingValue { get; set; }
        public int? SelectedOptionId { get; set; }
        public string SelectedOptionText { get; set; } = string.Empty;
    }

    public class SurveyAnswerCreateModel
    {
        public int SurveyId { get; set; }
        public int EmployeePortalId { get; set; }
        public List<SurveyQuestionAnswerCreateModel> Answers { get; set; } = new();
    }

    public class SurveyQuestionAnswerCreateModel
    {
        public int SurveyQuestionId { get; set; }
        public int? RatingValue { get; set; }
        public int? SurveyQuestionOptionId { get; set; }
    }
}
