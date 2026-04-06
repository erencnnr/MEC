using MEC.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace MEC.Domain.Entity.School
{
    [Table("announcement_image")]
    public class AnnouncementImage : BaseEntity
    {
        [Column("announcement_id")]
        public int AnnouncementId { get; set; }
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
        public Announcement? Announcement { get; set; }
    }
}
