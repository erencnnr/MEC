using System.Text.RegularExpressions;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.School;
using MEC.Portal.Models;
using MEC.Portal.Services;
using Microsoft.AspNetCore.Mvc;

namespace MEC.Portal.Controllers
{
    [Route("Admin/Announcements")]
    public class AnnouncementController : Controller
    {
        private const int PageSize = 6;
        private static readonly HashSet<string> AllowedAttachmentExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".ppt", ".pptx", ".zip", ".rar", ".jpg", ".jpeg", ".png"
        };
        private static readonly HashSet<string> AllowedGalleryExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif"
        };

        private readonly IAnnouncementService _announcementService;
        private readonly IGenericRepository<AnnouncementAttachment> _announcementAttachmentRepository;
        private readonly IGenericRepository<AnnouncementImage> _announcementImageRepository;
        private readonly IAnnouncementAttachmentApiClient _announcementAttachmentApiClient;
        private readonly IAnnouncementImageApiClient _announcementImageApiClient;

        public AnnouncementController(
            IAnnouncementService announcementService,
            IGenericRepository<AnnouncementAttachment> announcementAttachmentRepository,
            IGenericRepository<AnnouncementImage> announcementImageRepository,
            IAnnouncementAttachmentApiClient announcementAttachmentApiClient,
            IAnnouncementImageApiClient announcementImageApiClient)
        {
            _announcementService = announcementService;
            _announcementAttachmentRepository = announcementAttachmentRepository;
            _announcementImageRepository = announcementImageRepository;
            _announcementAttachmentApiClient = announcementAttachmentApiClient;
            _announcementImageApiClient = announcementImageApiClient;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? status = "active", int page = 1)
        {
            var normalizedStatus = NormalizeStatus(status);
            var announcements = await _announcementService.GetAllAnnouncementsAsync();

            var filteredAnnouncements = announcements
                .Where(x => normalizedStatus == "passive" ? !x.IsActive : x.IsActive)
                .OrderByDescending(x => x.CreatedDate ?? DateTime.MinValue)
                .ToList();

            var totalCount = filteredAnnouncements.Count;
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
            var currentPage = Math.Min(Math.Max(page, 1), totalPages);

            var items = filteredAnnouncements
                .Skip((currentPage - 1) * PageSize)
                .Take(PageSize)
                .Select(x => new AnnouncementCardViewModel
                {
                    Id = x.Id,
                    Title = x.Title,
                    Summary = BuildSummary(x.Content),
                    CreatedDate = x.CreatedDate,
                    IsActive = x.IsActive
                })
                .ToList();

            var model = new AnnouncementListViewModel
            {
                Items = items,
                Status = normalizedStatus,
                CurrentPage = currentPage,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = PageSize
            };

            return View(model);
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View(new AnnouncementFormViewModel { IsActive = true });
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AnnouncementFormViewModel model, CancellationToken cancellationToken)
        {
            ValidateAnnouncementFiles(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var announcement = new Announcement
            {
                Title = model.Title,
                Content = model.Content,
                IsActive = model.IsActive
            };

            await _announcementService.AddAnnouncementAsync(announcement);

            try
            {
                var uploadResult = await UploadAnnouncementAssetsAsync(announcement.Id, model, cancellationToken);
                if (!uploadResult.IsSuccess)
                {
                    await CleanupUploadedAssetsAsync(announcement.Id, uploadResult.UploadedAttachments, uploadResult.UploadedImages, cancellationToken);
                    await _announcementService.DeleteAnnouncementAsync(announcement.Id);
                    ModelState.AddModelError(string.Empty, uploadResult.Message);
                    return View(model);
                }

                await PersistUploadedAssetsAsync(announcement.Id, uploadResult.UploadedAttachments, uploadResult.UploadedImages);
            }
            catch
            {
                await _announcementService.DeleteAnnouncementAsync(announcement.Id);
                throw;
            }

            TempData["AnnouncementSuccess"] = "Duyuru kaydedildi.";
            return RedirectToAction(nameof(Index), new { status = announcement.IsActive ? "active" : "passive" });
        }

        [HttpGet("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var announcement = await _announcementService.GetAnnouncementByIdAsync(id);
            if (announcement == null)
            {
                return NotFound();
            }

            return View(await BuildFormModelAsync(announcement));
        }

        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AnnouncementFormViewModel model, CancellationToken cancellationToken)
        {
            if (id != model.Id)
            {
                model.Id = id;
            }

            ValidateAnnouncementFiles(model);

            var currentAnnouncement = await _announcementService.GetAnnouncementByIdAsync(id);
            if (currentAnnouncement == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await PopulateExistingAssetsAsync(model, id);
                return View(model);
            }

            var uploadResult = await UploadAnnouncementAssetsAsync(id, model, cancellationToken);
            if (!uploadResult.IsSuccess)
            {
                await CleanupUploadedAssetsAsync(id, uploadResult.UploadedAttachments, uploadResult.UploadedImages, cancellationToken);
                ModelState.AddModelError(string.Empty, uploadResult.Message);
                await PopulateExistingAssetsAsync(model, id);
                return View(model);
            }

            currentAnnouncement.Title = model.Title;
            currentAnnouncement.Content = model.Content;
            currentAnnouncement.IsActive = model.IsActive;
            await _announcementService.UpdateAnnouncementAsync(currentAnnouncement);
            await PersistUploadedAssetsAsync(id, uploadResult.UploadedAttachments, uploadResult.UploadedImages);

            TempData["AnnouncementSuccess"] = "Duyuru güncellendi.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost("Edit/{id:int}/DeleteAttachment/{attachmentId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttachment(int id, int attachmentId, CancellationToken cancellationToken)
        {
            var attachment = await _announcementAttachmentRepository.GetByIdAsync(attachmentId);
            if (attachment == null || attachment.AnnouncementId != id)
            {
                return NotFound();
            }

            var deleteResult = await _announcementAttachmentApiClient.DeleteAsync(id, attachment.FileName, cancellationToken);
            if (!deleteResult.IsSuccess)
            {
                TempData["AnnouncementError"] = deleteResult.Message;
                return RedirectToAction(nameof(Edit), new { id });
            }

            _announcementAttachmentRepository.Delete(attachment);
            TempData["AnnouncementSuccess"] = "Duyuru eki silindi.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost("Edit/{id:int}/DeleteImage/{imageId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int id, int imageId, CancellationToken cancellationToken)
        {
            var image = await _announcementImageRepository.GetByIdAsync(imageId);
            if (image == null || image.AnnouncementId != id)
            {
                return NotFound();
            }

            var deleteResult = await _announcementImageApiClient.DeleteAsync(id, image.FileName, cancellationToken);
            if (!deleteResult.IsSuccess)
            {
                TempData["AnnouncementError"] = deleteResult.Message;
                return RedirectToAction(nameof(Edit), new { id });
            }

            _announcementImageRepository.Delete(image);
            TempData["AnnouncementSuccess"] = "Galeri görseli silindi.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpGet("/Announcements/{id:int}", Name = "AnnouncementDetail")]
        public async Task<IActionResult> Detail(int id)
        {
            var announcement = await _announcementService.GetAnnouncementByIdAsync(id);
            if (announcement == null)
            {
                return NotFound();
            }

            var attachments = (await _announcementAttachmentRepository.GetAllAsync(x => x.AnnouncementId == id))
                .OrderBy(x => x.CreatedDate ?? DateTime.MinValue)
                .Select(x => new AnnouncementDetailLinkViewModel
                {
                    Label = x.OriginalFileName,
                    Url = _announcementAttachmentApiClient.GetFileUrl(x.AnnouncementId, x.FileName)
                })
                .ToList();

            var galleryImages = (await _announcementImageRepository.GetAllAsync(x => x.AnnouncementId == id))
                .OrderBy(x => x.CreatedDate ?? DateTime.MinValue)
                .Select(x => _announcementImageApiClient.GetFileUrl(x.AnnouncementId, x.FileName))
                .ToList();

            var allAnnouncements = await _announcementService.GetAllAnnouncementsAsync();
            var relatedAnnouncements = allAnnouncements
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

            var relatedImageLookup = relatedImages
                .GroupBy(x => x.AnnouncementId)
                .ToDictionary(x => x.Key, x => x.First());

            var model = new AnnouncementDetailViewModel
            {
                Id = announcement.Id,
                Title = announcement.Title,
                Content = announcement.Content ?? string.Empty,
                CreatedDate = announcement.CreatedDate,
                UpdatedDate = announcement.UpdateDate,
                IsAdminView = false,
                GalleryImages = galleryImages,
                Attachments = attachments,
                RelatedAnnouncements = relatedAnnouncements
                    .Select(x => new AnnouncementDetailRelatedItemViewModel
                    {
                        Id = x.Id,
                        Title = x.Title,
                        CreatedDate = x.CreatedDate,
                        ImageUrl = relatedImageLookup.TryGetValue(x.Id, out var image)
                            ? _announcementImageApiClient.GetFileUrl(image.AnnouncementId, image.FileName)
                            : "/content/images/logo.png"
                    })
                    .ToList()
            };

            return View(model);
        }

        [HttpPost("Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var attachments = await _announcementAttachmentRepository.GetAllAsync(x => x.AnnouncementId == id);
            var images = await _announcementImageRepository.GetAllAsync(x => x.AnnouncementId == id);

            foreach (var attachment in attachments)
            {
                await _announcementAttachmentApiClient.DeleteAsync(id, attachment.FileName, cancellationToken);
            }

            foreach (var image in images)
            {
                await _announcementImageApiClient.DeleteAsync(id, image.FileName, cancellationToken);
            }

            await _announcementService.DeleteAnnouncementAsync(id);
            return RedirectToAction(nameof(Index));
        }

        private void ValidateAnnouncementFiles(AnnouncementFormViewModel model)
        {
            ValidateFiles(model.AttachmentFiles, AllowedAttachmentExtensions, nameof(model.AttachmentFiles), "Duyuru ekleri");
            ValidateFiles(model.GalleryFiles, AllowedGalleryExtensions, nameof(model.GalleryFiles), "Duyuru galerisi");
        }

        private void ValidateFiles(IEnumerable<IFormFile> files, HashSet<string> allowedExtensions, string fieldName, string label)
        {
            const long maxFileSize = 10 * 1024 * 1024;

            foreach (var file in files.Where(x => x != null && x.Length > 0))
            {
                if (file.Length > maxFileSize)
                {
                    ModelState.AddModelError(fieldName, $"{label} alanındaki her dosya en fazla 10 MB olabilir.");
                }

                var extension = Path.GetExtension(file.FileName);
                if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(fieldName, $"{label} için seçilen dosya türü desteklenmiyor.");
                }
            }
        }

        private async Task<AnnouncementFormViewModel> BuildFormModelAsync(Announcement announcement)
        {
            var model = new AnnouncementFormViewModel
            {
                Id = announcement.Id,
                Title = announcement.Title,
                Content = announcement.Content,
                IsActive = announcement.IsActive
            };

            await PopulateExistingAssetsAsync(model, announcement.Id);
            return model;
        }

        private async Task PopulateExistingAssetsAsync(AnnouncementFormViewModel model, int announcementId)
        {
            model.ExistingAttachments = (await _announcementAttachmentRepository.GetAllAsync(x => x.AnnouncementId == announcementId))
                .OrderBy(x => x.CreatedDate ?? DateTime.MinValue)
                .Select(x => new AnnouncementAssetViewModel
                {
                    Id = x.Id,
                    Label = x.OriginalFileName,
                    Url = _announcementAttachmentApiClient.GetFileUrl(x.AnnouncementId, x.FileName)
                })
                .ToList();

            model.ExistingGalleryImages = (await _announcementImageRepository.GetAllAsync(x => x.AnnouncementId == announcementId))
                .OrderBy(x => x.CreatedDate ?? DateTime.MinValue)
                .Select(x => new AnnouncementAssetViewModel
                {
                    Id = x.Id,
                    Label = x.OriginalFileName,
                    Url = _announcementImageApiClient.GetFileUrl(x.AnnouncementId, x.FileName)
                })
                .ToList();
        }

        private async Task<AnnouncementAssetUploadBundleResult> UploadAnnouncementAssetsAsync(int announcementId, AnnouncementFormViewModel model, CancellationToken cancellationToken)
        {
            var uploadedAttachments = new List<AnnouncementAssetUploadRecord>();
            var uploadedImages = new List<AnnouncementAssetUploadRecord>();

            foreach (var file in model.AttachmentFiles.Where(x => x != null && x.Length > 0))
            {
                var result = await _announcementAttachmentApiClient.UploadAsync(announcementId, file, cancellationToken);
                if (!result.IsSuccess)
                {
                    return AnnouncementAssetUploadBundleResult.Fail(result.Message, uploadedAttachments, uploadedImages);
                }

                uploadedAttachments.Add(new AnnouncementAssetUploadRecord(file.FileName, file.ContentType, file.Length, result.FileName, result.RelativePath));
            }

            foreach (var file in model.GalleryFiles.Where(x => x != null && x.Length > 0))
            {
                var result = await _announcementImageApiClient.UploadAsync(announcementId, file, cancellationToken);
                if (!result.IsSuccess)
                {
                    return AnnouncementAssetUploadBundleResult.Fail(result.Message, uploadedAttachments, uploadedImages);
                }

                uploadedImages.Add(new AnnouncementAssetUploadRecord(file.FileName, file.ContentType, file.Length, result.FileName, result.RelativePath));
            }

            return AnnouncementAssetUploadBundleResult.Success(uploadedAttachments, uploadedImages);
        }

        private async Task PersistUploadedAssetsAsync(
            int announcementId,
            IReadOnlyCollection<AnnouncementAssetUploadRecord> uploadedAttachments,
            IReadOnlyCollection<AnnouncementAssetUploadRecord> uploadedImages)
        {
            foreach (var attachment in uploadedAttachments)
            {
                await _announcementAttachmentRepository.AddAsync(new AnnouncementAttachment
                {
                    AnnouncementId = announcementId,
                    FileName = attachment.StoredFileName,
                    OriginalFileName = Path.GetFileName(attachment.OriginalFileName),
                    RelativePath = attachment.RelativePath,
                    ContentType = string.IsNullOrWhiteSpace(attachment.ContentType) ? "application/octet-stream" : attachment.ContentType,
                    SizeBytes = attachment.SizeBytes,
                    CreatedDate = DateTime.Now
                });
            }

            foreach (var image in uploadedImages)
            {
                await _announcementImageRepository.AddAsync(new AnnouncementImage
                {
                    AnnouncementId = announcementId,
                    FileName = image.StoredFileName,
                    OriginalFileName = Path.GetFileName(image.OriginalFileName),
                    RelativePath = image.RelativePath,
                    ContentType = string.IsNullOrWhiteSpace(image.ContentType) ? "application/octet-stream" : image.ContentType,
                    SizeBytes = image.SizeBytes,
                    CreatedDate = DateTime.Now
                });
            }
        }

        private async Task CleanupUploadedAssetsAsync(
            int announcementId,
            IReadOnlyCollection<AnnouncementAssetUploadRecord> uploadedAttachments,
            IReadOnlyCollection<AnnouncementAssetUploadRecord> uploadedImages,
            CancellationToken cancellationToken)
        {
            foreach (var attachment in uploadedAttachments)
            {
                await _announcementAttachmentApiClient.DeleteAsync(announcementId, attachment.StoredFileName, cancellationToken);
            }

            foreach (var image in uploadedImages)
            {
                await _announcementImageApiClient.DeleteAsync(announcementId, image.StoredFileName, cancellationToken);
            }
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

            if (plainText.Length <= 180)
            {
                return plainText;
            }

            return $"{plainText[..177]}...";
        }

        private sealed record AnnouncementAssetUploadRecord(
            string OriginalFileName,
            string ContentType,
            long SizeBytes,
            string StoredFileName,
            string RelativePath);

        private sealed record AnnouncementAssetUploadBundleResult(
            bool IsSuccess,
            string Message,
            IReadOnlyCollection<AnnouncementAssetUploadRecord> UploadedAttachments,
            IReadOnlyCollection<AnnouncementAssetUploadRecord> UploadedImages)
        {
            public static AnnouncementAssetUploadBundleResult Success(
                IReadOnlyCollection<AnnouncementAssetUploadRecord> uploadedAttachments,
                IReadOnlyCollection<AnnouncementAssetUploadRecord> uploadedImages)
            {
                return new AnnouncementAssetUploadBundleResult(true, string.Empty, uploadedAttachments, uploadedImages);
            }

            public static AnnouncementAssetUploadBundleResult Fail(
                string message,
                IReadOnlyCollection<AnnouncementAssetUploadRecord> uploadedAttachments,
                IReadOnlyCollection<AnnouncementAssetUploadRecord> uploadedImages)
            {
                return new AnnouncementAssetUploadBundleResult(false, message, uploadedAttachments, uploadedImages);
            }
        }
    }
}
