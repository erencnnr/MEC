using MEC.Application.Abstractions.Service.SchoolService.Model;
using MEC.Domain.Entity.School;

namespace MEC.Application.Abstractions.Service.SchoolService
{
    public interface IAnnouncementService
    {
        Task<IEnumerable<Announcement>> GetAllAnnouncementsAsync();
        Task<IEnumerable<Announcement>> GetActiveAnnouncementsAsync();
        Task<Announcement> GetAnnouncementByIdAsync(int id);
        Task AddAnnouncementAsync(Announcement announcement);
        Task UpdateAnnouncementAsync(Announcement announcement);
        Task DeleteAnnouncementAsync(int id);
        Task<AnnouncementListResultModel> GetAnnouncementBoardAsync(AnnouncementListQueryModel query);
        Task<AnnouncementFormDataModel?> GetAnnouncementFormDataAsync(int id);
        Task<AnnouncementDetailDataModel?> GetAnnouncementDetailDataAsync(int id);
        Task<List<AnnouncementAssetModel>> GetAnnouncementAttachmentsAsync(int announcementId);
        Task<List<AnnouncementAssetModel>> GetAnnouncementImagesAsync(int announcementId);
        Task AddAnnouncementAttachmentAsync(AnnouncementAssetPersistModel asset);
        Task AddAnnouncementImageAsync(AnnouncementAssetPersistModel asset);
        Task<AnnouncementAssetModel?> GetAnnouncementAttachmentAsync(int attachmentId);
        Task<AnnouncementAssetModel?> GetAnnouncementImageAsync(int imageId);
        Task DeleteAnnouncementAttachmentMetadataAsync(int attachmentId);
        Task DeleteAnnouncementImageMetadataAsync(int imageId);
        Task<AnnouncementDeletePrepareModel> PrepareAnnouncementDeleteAsync(int id);
    }
}
