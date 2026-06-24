using MEC.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace MEC.Domain.Entity.School
{
    [Table("survey_response_answer")]
    public class SurveyResponseAnswer : BaseEntity
    {
        [Column("survey_response_id")]
        public int SurveyResponseId { get; set; }

        [Column("survey_question_id")]
        public int SurveyQuestionId { get; set; }

        [Column("rating_value")]
        public int? RatingValue { get; set; }

        [Column("survey_question_option_id")]
        public int? SurveyQuestionOptionId { get; set; }

        [ForeignKey(nameof(SurveyResponseId))]
        public SurveyResponse? SurveyResponse { get; set; }

        [ForeignKey(nameof(SurveyQuestionId))]
        public SurveyQuestion? SurveyQuestion { get; set; }

        [ForeignKey(nameof(SurveyQuestionOptionId))]
        public SurveyQuestionOption? SurveyQuestionOption { get; set; }
    }
}
