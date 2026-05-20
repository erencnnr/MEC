using System.Text.RegularExpressions;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Application.Abstractions.Service.SchoolService.Model;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Common.Enum;
using MEC.Domain.Entity.School;

namespace MEC.Application.Service.SchoolService
{
    public class AnnouncementService : IAnnouncementService
    {
        private readonly IGenericRepository<Announcement> _repository;
        private readonly IGenericRepository<AnnouncementAttachment> _announcementAttachmentRepository;
        private readonly IGenericRepository<AnnouncementImage> _announcementImageRepository;

        public AnnouncementService(
            IGenericRepository<Announcement> repository,
            IGenericRepository<AnnouncementAttachment> announcementAttachmentRepository,
            IGenericRepository<AnnouncementImage> announcementImageRepository)
        {
            _repository = repository;
            _announcementAttachmentRepository = announcementAttachmentRepository;
            _announcementImageRepository = announcementImageRepository;
        }

        public async Task<IEnumerable<Announcement>> GetAllAnnouncementsAsync(AnnouncementContentType contentType = AnnouncementContentType.Announcement)
        {
            return await _repository.GetAllAsync(x => x.ContentType == contentType);
        }

        public async Task<IEnumerable<Announcement>> GetActiveAnnouncementsAsync(AnnouncementContentType contentType = AnnouncementContentType.Announcement)
        {
            return await _repository.GetAllAsync(x => x.IsActive && x.ContentType == contentType);
        }

        public async Task<Announcement?> GetAnnouncementByIdAsync(int id, AnnouncementContentType contentType = AnnouncementContentType.Announcement)
        {
            return (await _repository.GetAllAsync(x => x.Id == id && x.ContentType == contentType)).FirstOrDefault();
        }

        public async Task AddAnnouncementAsync(Announcement announcement)
        {
            announcement.CreatedDate = DateTime.Now;
            await _repository.AddAsync(announcement);
        }

        public async Task UpdateAnnouncementAsync(Announcement announcement)
        {
            announcement.UpdateDate = DateTime.Now;
            _repository.Update(announcement);
        }

        public async Task DeleteAnnouncementAsync(int id)
        {
            var announcement = await _repository.GetByIdAsync(id);
            if (announcement != null)
            {
                _repository.Delete(announcement);
            }
        }

        public async Task<AnnouncementListResultModel> GetAnnouncementBoardAsync(AnnouncementListQueryModel query)
        {
            var normalizedStatus = NormalizeStatus(query.Status);
            var pageSize = query.PageSize <= 0 ? 6 : query.PageSize;
            var announcements = await GetAllAnnouncementsAsync(query.ContentType);

            var filteredAnnouncements = announcements
                .Where(x => normalizedStatus == "passive" ? !x.IsActive : x.IsActive)
                .OrderByDescending(x => x.CreatedDate ?? DateTime.MinValue)
                .ToList();

            var totalCount = filteredAnnouncements.Count;
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            var currentPage = Math.Min(Math.Max(query.Page, 1), totalPages);

            return new AnnouncementListResultModel
            {
                Items = filteredAnnouncements
                    .Skip((currentPage - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new AnnouncementCardModel
                    {
                        Id = x.Id,
                        Title = x.Title,
                        Summary = BuildSummary(x.Content),
                        CreatedDate = x.CreatedDate,
                        IsActive = x.IsActive
                    })
                    .ToList(),
                Status = normalizedStatus,
                CurrentPage = currentPage,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = pageSize
            };
        }

        public async Task<AnnouncementFormDataModel?> GetAnnouncementFormDataAsync(int id, AnnouncementContentType contentType = AnnouncementContentType.Announcement)
        {
            var announcement = await GetAnnouncementByIdAsync(id, contentType);
            if (announcement == null)
            {
                return null;
            }

            return new AnnouncementFormDataModel
            {
                Announcement = announcement,
                Attachments = await GetAnnouncementAttachmentsAsync(id),
                GalleryImages = await GetAnnouncementImagesAsync(id)
            };
        }

        public async Task<AnnouncementDetailDataModel?> GetAnnouncementDetailDataAsync(int id, AnnouncementContentType contentType = AnnouncementContentType.Announcement)
        {
            var formData = await GetAnnouncementFormDataAsync(id, contentType);
            if (formData == null)
            {
                return null;
            }

            var relatedAnnouncements = (await GetAllAnnouncementsAsync(contentType))
                .Where(x => x.Id != id && x.IsActive)
                .OrderByDescending(x => x.CreatedDate ?? DateTime.MinValue)
                .Take(4)
                .ToList();
            var relatedAnnouncementIds = relatedAnnouncements.Select(x => x.Id).ToList();
            var relatedImages = relatedAnnouncementIds.Count == 0
                ? new List<AnnouncementImage>()
                : (await _announcementImageRepository.GetAllAsync(x => relatedAnnouncementIds.Contains(x.AnnouncementId)))
                    .OrderBy(x => x.CreatedDate ?? DateTime.MinValue)
                    .ToList();

            return new AnnouncementDetailDataModel
            {
                Announcement = formData.Announcement,
                Attachments = formData.Attachments,
                GalleryImages = formData.GalleryImages,
                RelatedAnnouncements = relatedAnnouncements,
                RelatedCoverImages = relatedImages
                    .GroupBy(x => x.AnnouncementId)
                    .ToDictionary(x => x.Key, x => MapImage(x.First()))
            };
        }

        public async Task<List<AnnouncementAssetModel>> GetAnnouncementAttachmentsAsync(int announcementId)
        {
            return (await _announcementAttachmentRepository.GetAllAsync(x => x.AnnouncementId == announcementId))
                .OrderBy(x => x.CreatedDate ?? DateTime.MinValue)
                .Select(MapAttachment)
                .ToList();
        }

        public async Task<List<AnnouncementAssetModel>> GetAnnouncementImagesAsync(int announcementId)
        {
            return (await _announcementImageRepository.GetAllAsync(x => x.AnnouncementId == announcementId))
                .OrderBy(x => x.CreatedDate ?? DateTime.MinValue)
                .Select(MapImage)
                .ToList();
        }

        public async Task AddAnnouncementAttachmentAsync(AnnouncementAssetPersistModel asset)
        {
            await _announcementAttachmentRepository.AddAsync(new AnnouncementAttachment
            {
                AnnouncementId = asset.AnnouncementId,
                FileName = asset.FileName,
                OriginalFileName = Path.GetFileName(asset.OriginalFileName),
                RelativePath = asset.RelativePath,
                ContentType = string.IsNullOrWhiteSpace(asset.ContentType) ? "application/octet-stream" : asset.ContentType,
                SizeBytes = asset.SizeBytes,
                CreatedDate = DateTime.Now
            });
        }

        public async Task AddAnnouncementImageAsync(AnnouncementAssetPersistModel asset)
        {
            await _announcementImageRepository.AddAsync(new AnnouncementImage
            {
                AnnouncementId = asset.AnnouncementId,
                FileName = asset.FileName,
                OriginalFileName = Path.GetFileName(asset.OriginalFileName),
                RelativePath = asset.RelativePath,
                ContentType = string.IsNullOrWhiteSpace(asset.ContentType) ? "application/octet-stream" : asset.ContentType,
                SizeBytes = asset.SizeBytes,
                CreatedDate = DateTime.Now
            });
        }

        public async Task<AnnouncementAssetModel?> GetAnnouncementAttachmentAsync(int attachmentId)
        {
            var attachment = await _announcementAttachmentRepository.GetByIdAsync(attachmentId);
            return attachment == null ? null : MapAttachment(attachment);
        }

        public async Task<AnnouncementAssetModel?> GetAnnouncementImageAsync(int imageId)
        {
            var image = await _announcementImageRepository.GetByIdAsync(imageId);
            return image == null ? null : MapImage(image);
        }

        public async Task DeleteAnnouncementAttachmentMetadataAsync(int attachmentId)
        {
            var attachment = await _announcementAttachmentRepository.GetByIdAsync(attachmentId);
            if (attachment != null)
            {
                _announcementAttachmentRepository.Delete(attachment);
            }
        }

        public async Task DeleteAnnouncementImageMetadataAsync(int imageId)
        {
            var image = await _announcementImageRepository.GetByIdAsync(imageId);
            if (image != null)
            {
                _announcementImageRepository.Delete(image);
            }
        }

        public async Task<AnnouncementDeletePrepareModel> PrepareAnnouncementDeleteAsync(int id, AnnouncementContentType contentType = AnnouncementContentType.Announcement)
        {
            var announcement = await GetAnnouncementByIdAsync(id, contentType);
            if (announcement == null)
            {
                return new AnnouncementDeletePrepareModel();
            }

            return new AnnouncementDeletePrepareModel
            {
                Attachments = await GetAnnouncementAttachmentsAsync(id),
                GalleryImages = await GetAnnouncementImagesAsync(id)
            };
        }

        private static AnnouncementAssetModel MapAttachment(AnnouncementAttachment attachment)
        {
            return new AnnouncementAssetModel
            {
                Id = attachment.Id,
                AnnouncementId = attachment.AnnouncementId,
                FileName = attachment.FileName,
                OriginalFileName = attachment.OriginalFileName,
                RelativePath = attachment.RelativePath,
                ContentType = attachment.ContentType,
                SizeBytes = attachment.SizeBytes,
                CreatedDate = attachment.CreatedDate
            };
        }

        private static AnnouncementAssetModel MapImage(AnnouncementImage image)
        {
            return new AnnouncementAssetModel
            {
                Id = image.Id,
                AnnouncementId = image.AnnouncementId,
                FileName = image.FileName,
                OriginalFileName = image.OriginalFileName,
                RelativePath = image.RelativePath,
                ContentType = image.ContentType,
                SizeBytes = image.SizeBytes,
                CreatedDate = image.CreatedDate
            };
        }

        private static string NormalizeStatus(string? status)
        {
            return string.Equals(status, "passive", StringComparison.OrdinalIgnoreCase)
                ? "passive"
                : "active";
        }

        private static string BuildSummary(string? htmlContent)
        {
            if (string.IsNullOrWhiteSpace(htmlContent))
            {
                return "İçerik bulunmuyor.";
            }

            var plainText = Regex.Replace(htmlContent, "<.*?>", " ");
            plainText = Regex.Replace(plainText, @"\s+", " ").Trim();

            return plainText.Length <= 180
                ? plainText
                : $"{plainText[..177]}...";
        }
    }
}
