using MEC.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace MEC.Domain.Entity.School
{
    [Table("survey_question_option")]
    public class SurveyQuestionOption : BaseEntity
    {
        [Column("survey_question_id")]
        public int SurveyQuestionId { get; set; }

        [Column("text")]
        public string Text { get; set; } = string.Empty;

        [Column("display_order")]
        public int DisplayOrder { get; set; }

        [ForeignKey(nameof(SurveyQuestionId))]
        public SurveyQuestion? SurveyQuestion { get; set; }

        public ICollection<SurveyResponseAnswer> ResponseAnswers { get; set; } = new List<SurveyResponseAnswer>();
    }
}
