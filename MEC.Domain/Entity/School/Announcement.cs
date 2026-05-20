using MEC.Domain.Common;
using MEC.Domain.Common.Enum;
using System.ComponentModel.DataAnnotations.Schema;

namespace MEC.Domain.Entity.School
{
    [Table("announcement")]
    public class Announcement : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public AnnouncementContentType ContentType { get; set; } = AnnouncementContentType.Announcement;
        public ICollection<AnnouncementAttachment> Attachments { get; set; } = new List<AnnouncementAttachment>();
        public ICollection<AnnouncementImage> Images { get; set; } = new List<AnnouncementImage>();
    }
}
