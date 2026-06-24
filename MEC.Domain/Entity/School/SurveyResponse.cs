using MEC.Domain.Common;
using MEC.Domain.Entity.Employee;
using System.ComponentModel.DataAnnotations.Schema;

namespace MEC.Domain.Entity.School
{
    [Table("survey_response")]
    public class SurveyResponse : BaseEntity
    {
        [Column("survey_id")]
        public int SurveyId { get; set; }

        [Column("employee_portal_id")]
        public int EmployeePortalId { get; set; }

        [Column("answered_at")]
        public DateTime AnsweredAt { get; set; }

        [ForeignKey(nameof(SurveyId))]
        public Survey? Survey { get; set; }

        [ForeignKey(nameof(EmployeePortalId))]
        public EmployeePortal? EmployeePortal { get; set; }

        public ICollection<SurveyResponseAnswer> Answers { get; set; } = new List<SurveyResponseAnswer>();
    }
}
