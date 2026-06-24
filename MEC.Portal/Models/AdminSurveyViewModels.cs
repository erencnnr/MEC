using MEC.Domain.Common.Enum;
using System.ComponentModel.DataAnnotations;

namespace MEC.Portal.Models
{
    public class AdminSurveyListViewModel
    {
        public string Status { get; set; } = "all";
        public List<AdminSurveyListItemViewModel> Items { get; set; } = new();
    }

    public class AdminSurveyCreateViewModel
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public List<AdminSurveyQuestionInputViewModel> Questions { get; set; } = new();
    }

    public class AdminSurveyQuestionInputViewModel
    {
        public SurveyType Type { get; set; } = SurveyType.Rating;
        public string? QuestionText { get; set; }
        public List<AdminSurveyOptionInputViewModel> Options { get; set; } = new();
    }

    public class AdminSurveyOptionInputViewModel
    {
        public string? Text { get; set; }
    }

    public class AdminSurveyListItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
        public string QuestionPreview { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public string StatusTone { get; set; } = string.Empty;
        public int AnswerCount { get; set; }
        public decimal ParticipationRate { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class AdminSurveyDetailViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public string StatusTone { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
        public int AnswerCount { get; set; }
        public int ActivePortalUserCount { get; set; }
        public decimal ParticipationRate { get; set; }
        public DateTime? CreatedDate { get; set; }
        public List<AdminSurveyQuestionDetailViewModel> Questions { get; set; } = new();
        public bool HasResponses => AnswerCount > 0;
    }

    public class AdminSurveyQuestionDetailViewModel
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public SurveyType Type { get; set; }
        public string TypeLabel { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public decimal? AverageRating { get; set; }
        public List<AdminSurveyOptionViewModel> Options { get; set; } = new();
        public List<AdminSurveyRatingDistributionViewModel> RatingDistribution { get; set; } = new();
        public List<AdminSurveyOptionResultViewModel> OptionResults { get; set; } = new();
    }

    public class AdminSurveyOptionViewModel
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    public class AdminSurveyRatingDistributionViewModel
    {
        public int RatingValue { get; set; }
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    public class AdminSurveyOptionResultViewModel
    {
        public int OptionId { get; set; }
        public string Text { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }
}
