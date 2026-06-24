using MEC.Domain.Common.Enum;

namespace MEC.Portal.Models
{
    public class SurveyListViewModel
    {
        public List<SurveyListItemViewModel> Items { get; set; } = new();
    }

    public class SurveyListItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
        public string QuestionPreview { get; set; } = string.Empty;
        public bool HasAnswered { get; set; }
        public DateTime? AnsweredAt { get; set; }
    }

    public class SurveyDetailViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool HasAnswered { get; set; }
        public DateTime? AnsweredAt { get; set; }
        public List<SurveyQuestionViewModel> Questions { get; set; } = new();
        public List<SurveyAnsweredQuestionViewModel> AnsweredQuestions { get; set; } = new();
    }

    public class SurveyQuestionViewModel
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public SurveyType Type { get; set; }
        public string TypeLabel { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public List<SurveyOptionViewModel> Options { get; set; } = new();
    }

    public class SurveyOptionViewModel
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    public class SurveyAnsweredQuestionViewModel
    {
        public int SurveyQuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string TypeLabel { get; set; } = string.Empty;
        public int? SelectedRatingValue { get; set; }
        public string SelectedOptionText { get; set; } = string.Empty;
    }

    public class SurveyQuestionAnswerInputViewModel
    {
        public int SurveyQuestionId { get; set; }
        public int? RatingValue { get; set; }
        public int? SurveyQuestionOptionId { get; set; }
    }
}
