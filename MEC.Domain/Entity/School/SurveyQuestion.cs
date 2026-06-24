using MEC.Domain.Common;
using MEC.Domain.Common.Enum;
using System.ComponentModel.DataAnnotations.Schema;

namespace MEC.Domain.Entity.School
{
    [Table("survey_question")]
    public class SurveyQuestion : BaseEntity
    {
        [Column("survey_id")]
        public int SurveyId { get; set; }

        [Column("question_text")]
        public string QuestionText { get; set; } = string.Empty;

        [Column("type")]
        public SurveyType Type { get; set; }

        [Column("display_order")]
        public int DisplayOrder { get; set; }

        [ForeignKey(nameof(SurveyId))]
        public Survey? Survey { get; set; }

        public ICollection<SurveyQuestionOption> Options { get; set; } = new List<SurveyQuestionOption>();
        public ICollection<SurveyResponseAnswer> ResponseAnswers { get; set; } = new List<SurveyResponseAnswer>();
    }
}
