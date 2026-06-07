using MEC.Domain.Common;
using MEC.Domain.Common.Enum;
using System.ComponentModel.DataAnnotations.Schema;

namespace MEC.Domain.Entity.School
{
    [Table("food_menu_month")]
    public class FoodMenuMonth : BaseEntity
    {
        [Column("year")]
        public int Year { get; set; }

        [Column("month")]
        public int Month { get; set; }

        [Column("status")]
        public FoodMenuMonthStatus Status { get; set; }

        [Column("file_name")]
        public string FileName { get; set; } = string.Empty;

        [Column("original_file_name")]
        public string OriginalFileName { get; set; } = string.Empty;

        [Column("relative_path")]
        public string RelativePath { get; set; } = string.Empty;

        [Column("content_type")]
        public string ContentType { get; set; } = string.Empty;

        [Column("size_bytes")]
        public long SizeBytes { get; set; }

        [Column("page_count")]
        public int PageCount { get; set; }

        [Column("parse_warnings")]
        public string ParseWarnings { get; set; } = string.Empty;

        [Column("imported_at")]
        public DateTime? ImportedAt { get; set; }

        [Column("published_at")]
        public DateTime? PublishedAt { get; set; }

        public ICollection<FoodMenuDay> Days { get; set; } = new List<FoodMenuDay>();
    }
}
