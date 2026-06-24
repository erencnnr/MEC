using MEC.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace MEC.Domain.Entity.School
{
    [Table("survey")]
    public class Survey : BaseEntity
    {
        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Column("is_active")]
        public bool IsActive { get; set; }

        public ICollection<SurveyQuestion> Questions { get; set; } = new List<SurveyQuestion>();
        public ICollection<SurveyResponse> Responses { get; set; } = new List<SurveyResponse>();
    }
}
