using MEC.Application.Abstractions.Service.SchoolService.Model;
using MEC.Domain.Common.Enum;
using MEC.Domain.Entity.School;

namespace MEC.Application.Abstractions.Service.SchoolService
{
    public interface IAnnouncementService
    {
        Task<IEnumerable<Announcement>> GetAllAnnouncementsAsync(AnnouncementContentType contentType = AnnouncementContentType.Announcement);
        Task<IEnumerable<Announcement>> GetActiveAnnouncementsAsync(AnnouncementContentType contentType = AnnouncementContentType.Announcement);
        Task<Announcement?> GetAnnouncementByIdAsync(int id, AnnouncementContentType contentType = AnnouncementContentType.Announcement);
        Task AddAnnouncementAsync(Announcement announcement);
        Task UpdateAnnouncementAsync(Announcement announcement);
        Task DeleteAnnouncementAsync(int id);
        Task<AnnouncementListResultModel> GetAnnouncementBoardAsync(AnnouncementListQueryModel query);
        Task<AnnouncementFormDataModel?> GetAnnouncementFormDataAsync(int id, AnnouncementContentType contentType = AnnouncementContentType.Announcement);
        Task<AnnouncementDetailDataModel?> GetAnnouncementDetailDataAsync(int id, AnnouncementContentType contentType = AnnouncementContentType.Announcement);
        Task<List<AnnouncementAssetModel>> GetAnnouncementAttachmentsAsync(int announcementId);
        Task<List<AnnouncementAssetModel>> GetAnnouncementImagesAsync(int announcementId);
        Task AddAnnouncementAttachmentAsync(AnnouncementAssetPersistModel asset);
        Task AddAnnouncementImageAsync(AnnouncementAssetPersistModel asset);
        Task<AnnouncementAssetModel?> GetAnnouncementAttachmentAsync(int attachmentId);
        Task<AnnouncementAssetModel?> GetAnnouncementImageAsync(int imageId);
        Task DeleteAnnouncementAttachmentMetadataAsync(int attachmentId);
        Task DeleteAnnouncementImageMetadataAsync(int imageId);
        Task<AnnouncementDeletePrepareModel> PrepareAnnouncementDeleteAsync(int id, AnnouncementContentType contentType = AnnouncementContentType.Announcement);
    }
}
