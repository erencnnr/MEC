using MEC.Application.Abstractions.Common.Models;
using MEC.Domain.Common.Enum;
using MEC.Domain.Entity.School;

namespace MEC.Application.Abstractions.Service.SchoolService.Model
{
    public class AnnouncementListQueryModel
    {
        public string? Status { get; set; } = "active";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 6;
        public AnnouncementContentType ContentType { get; set; } = AnnouncementContentType.Announcement;
    }

    public class AnnouncementCardModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public DateTime? CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class AnnouncementListResultModel : PagedResultModel<AnnouncementCardModel>
    {
        public string Status { get; set; } = "active";
    }

    public class AnnouncementAssetModel
    {
        public int Id { get; set; }
        public int AnnouncementId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class AnnouncementFormDataModel
    {
        public Announcement Announcement { get; set; } = new();
        public List<AnnouncementAssetModel> Attachments { get; set; } = new();
        public List<AnnouncementAssetModel> GalleryImages { get; set; } = new();
    }

    public class AnnouncementDetailDataModel : AnnouncementFormDataModel
    {
        public List<Announcement> RelatedAnnouncements { get; set; } = new();
        public Dictionary<int, AnnouncementAssetModel> RelatedCoverImages { get; set; } = new();
    }

    public class AnnouncementAssetPersistModel : StoredFileModel
    {
        public int AnnouncementId { get; set; }
    }

    public class AnnouncementDeletePrepareModel
    {
        public List<AnnouncementAssetModel> Attachments { get; set; } = new();
        public List<AnnouncementAssetModel> GalleryImages { get; set; } = new();
    }
}
