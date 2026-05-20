using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Application.Abstractions.Service.SchoolService.Model;
using MEC.Domain.Common.Enum;
using MEC.Domain.Entity.School;
using MEC.Portal.Models;
using MEC.Portal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MEC.Portal.Controllers
{
    [Route("Admin/News")]
    public class NewsController : Controller
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
        private readonly IAnnouncementAttachmentApiClient _announcementAttachmentApiClient;
        private readonly IAnnouncementImageApiClient _announcementImageApiClient;

        public NewsController(
            IAnnouncementService announcementService,
            IAnnouncementAttachmentApiClient announcementAttachmentApiClient,
            IAnnouncementImageApiClient announcementImageApiClient)
        {
            _announcementService = announcementService;
            _announcementAttachmentApiClient = announcementAttachmentApiClient;
            _announcementImageApiClient = announcementImageApiClient;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("")]
        public async Task<IActionResult> Index(string? status = "active", int page = 1)
        {
            var result = await _announcementService.GetAnnouncementBoardAsync(new AnnouncementListQueryModel
            {
                Status = status,
                Page = page,
                PageSize = PageSize,
                ContentType = AnnouncementContentType.News
            });

            return View("~/Views/Announcement/Index.cshtml", BuildNewsListModel(result));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View("~/Views/Announcement/Create.cshtml", BuildNewsCreateModel());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AnnouncementFormViewModel model, CancellationToken cancellationToken)
        {
            ApplyNewsCreateLabels(model);
            ValidateFiles(model);

            if (!ModelState.IsValid)
            {
                return View("~/Views/Announcement/Create.cshtml", model);
            }

            var news = new Announcement
            {
                Title = model.Title,
                Content = model.Content,
                IsActive = model.IsActive,
                ContentType = AnnouncementContentType.News
            };

            await _announcementService.AddAnnouncementAsync(news);

            try
            {
                var uploadResult = await UploadAssetsAsync(news.Id, model, cancellationToken);
                if (!uploadResult.IsSuccess)
                {
                    await CleanupUploadedAssetsAsync(news.Id, uploadResult.UploadedAttachments, uploadResult.UploadedImages, cancellationToken);
                    await _announcementService.DeleteAnnouncementAsync(news.Id);
                    ModelState.AddModelError(string.Empty, uploadResult.Message);
                    return View("~/Views/Announcement/Create.cshtml", model);
                }

                await PersistUploadedAssetsAsync(news.Id, uploadResult.UploadedAttachments, uploadResult.UploadedImages);
            }
            catch
            {
                await _announcementService.DeleteAnnouncementAsync(news.Id);
                throw;
            }

            TempData["AnnouncementSuccess"] = "Haber kaydedildi.";
            return RedirectToAction(nameof(Index), new { status = news.IsActive ? "active" : "passive" });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var news = await _announcementService.GetAnnouncementByIdAsync(id, AnnouncementContentType.News);
            if (news == null)
            {
                return NotFound();
            }

            return View("~/Views/Announcement/Edit.cshtml", await BuildNewsEditModelAsync(news));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AnnouncementFormViewModel model, CancellationToken cancellationToken)
        {
            if (id != model.Id)
            {
                model.Id = id;
            }

            ApplyNewsEditLabels(model);
            ValidateFiles(model);

            var currentNews = await _announcementService.GetAnnouncementByIdAsync(id, AnnouncementContentType.News);
            if (currentNews == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await PopulateExistingAssetsAsync(model, id);
                return View("~/Views/Announcement/Edit.cshtml", model);
            }

            var uploadResult = await UploadAssetsAsync(id, model, cancellationToken);
            if (!uploadResult.IsSuccess)
            {
                await CleanupUploadedAssetsAsync(id, uploadResult.UploadedAttachments, uploadResult.UploadedImages, cancellationToken);
                ModelState.AddModelError(string.Empty, uploadResult.Message);
                await PopulateExistingAssetsAsync(model, id);
                return View("~/Views/Announcement/Edit.cshtml", model);
            }

            currentNews.Title = model.Title;
            currentNews.Content = model.Content;
            currentNews.IsActive = model.IsActive;
            await _announcementService.UpdateAnnouncementAsync(currentNews);
            await PersistUploadedAssetsAsync(id, uploadResult.UploadedAttachments, uploadResult.UploadedImages);

            TempData["AnnouncementSuccess"] = "Haber güncellendi.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("Edit/{id:int}/DeleteAttachment/{attachmentId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttachment(int id, int attachmentId, CancellationToken cancellationToken)
        {
            var news = await _announcementService.GetAnnouncementByIdAsync(id, AnnouncementContentType.News);
            if (news == null)
            {
                return NotFound();
            }

            var attachment = await _announcementService.GetAnnouncementAttachmentAsync(attachmentId);
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

            await _announcementService.DeleteAnnouncementAttachmentMetadataAsync(attachmentId);
            TempData["AnnouncementSuccess"] = "Haber eki silindi.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("Edit/{id:int}/DeleteImage/{imageId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int id, int imageId, CancellationToken cancellationToken)
        {
            var news = await _announcementService.GetAnnouncementByIdAsync(id, AnnouncementContentType.News);
            if (news == null)
            {
                return NotFound();
            }

            var image = await _announcementService.GetAnnouncementImageAsync(imageId);
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

            await _announcementService.DeleteAnnouncementImageMetadataAsync(imageId);
            TempData["AnnouncementSuccess"] = "Galeri görseli silindi.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpGet("/News/{id:int}", Name = "NewsDetail")]
        [Authorize]
        public async Task<IActionResult> Detail(int id)
        {
            var detail = await _announcementService.GetAnnouncementDetailDataAsync(id, AnnouncementContentType.News);
            if (detail == null)
            {
                return NotFound();
            }

            return View("~/Views/Announcement/Detail.cshtml", BuildNewsDetailModel(detail));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var news = await _announcementService.GetAnnouncementByIdAsync(id, AnnouncementContentType.News);
            if (news == null)
            {
                return NotFound();
            }

            var deletePrepare = await _announcementService.PrepareAnnouncementDeleteAsync(id, AnnouncementContentType.News);

            foreach (var attachment in deletePrepare.Attachments)
            {
                await _announcementAttachmentApiClient.DeleteAsync(id, attachment.FileName, cancellationToken);
            }

            foreach (var image in deletePrepare.GalleryImages)
            {
                await _announcementImageApiClient.DeleteAsync(id, image.FileName, cancellationToken);
            }

            await _announcementService.DeleteAnnouncementAsync(id);
            return RedirectToAction(nameof(Index), new { status = news.IsActive ? "active" : "passive" });
        }

        private static AnnouncementListViewModel BuildNewsListModel(AnnouncementListResultModel result)
        {
            return new AnnouncementListViewModel
            {
                Items = result.Items.Select(MapAnnouncementCard).ToList(),
                Status = result.Status,
                CurrentPage = result.CurrentPage,
                TotalPages = result.TotalPages,
                TotalCount = result.TotalCount,
                PageSize = result.PageSize,
                Eyebrow = "Haber yönetimi",
                Title = "Haberler",
                Description = "Aktif ve pasif haberleri tarih sırasıyla görüntüleyin, kartlardan seçerek güncelleme ekranına geçin.",
                CreateButtonText = "Yeni Haber",
                EmptyTitle = "Listelenecek haber bulunmuyor.",
                EmptyDescription = "Seçili filtre için henüz bir haber kaydı yok. Yeni bir haber oluşturabilirsiniz."
            };
        }

        private static AnnouncementFormViewModel BuildNewsCreateModel()
        {
            var model = new AnnouncementFormViewModel { IsActive = true };
            ApplyNewsCreateLabels(model);
            return model;
        }

        private async Task<AnnouncementFormViewModel> BuildNewsEditModelAsync(Announcement news)
        {
            var model = new AnnouncementFormViewModel
            {
                Id = news.Id,
                Title = news.Title,
                Content = news.Content,
                IsActive = news.IsActive
            };

            ApplyNewsEditLabels(model);
            await PopulateExistingAssetsAsync(model, news.Id);
            return model;
        }

        private static void ApplyNewsCreateLabels(AnnouncementFormViewModel model)
        {
            model.ControllerName = "News";
            model.IndexAction = nameof(Index);
            model.EditAction = nameof(Edit);
            model.DeleteAttachmentAction = nameof(DeleteAttachment);
            model.DeleteImageAction = nameof(DeleteImage);
            model.Eyebrow = "Haber yönetimi";
            model.PageTitle = "Yeni Haber Ekle";
            model.Description = "Portalda yayınlanacak haberi başlık, içerik ve yayın durumu ile birlikte hazırlayın.";
            model.TitleLabel = "Haber Başlığı";
            model.TitlePlaceholder = "Haber başlığını giriniz";
            model.ContentLabel = "Haber İçeriği";
            model.EditorPlaceholder = "Haber detaylarını yazın, link veya görsel ekleyin...";
            model.AttachmentLabel = "Haber Ekleri";
            model.AttachmentHelpText = "İsterseniz habere belge, doküman veya indirilebilir dosya ekleyebilirsiniz.";
            model.GalleryLabel = "Haber Galerisi";
            model.GalleryHelpText = "Detay sayfasında galeri alanında gösterilecek fotoğrafları buradan ekleyebilirsiniz.";
            model.SaveButtonText = "Kaydet";
            model.CancelButtonText = "İptal";
        }

        private static void ApplyNewsEditLabels(AnnouncementFormViewModel model)
        {
            ApplyNewsCreateLabels(model);
            model.PageTitle = "Haber Düzenle";
            model.Description = "Başlık, içerik ve yayın durumunu güncelleyerek haberi portal akışına uygun hale getirin.";
            model.AttachmentHelpText = "Yeni dosyalar eklenir; mevcut ekler korunur.";
            model.GalleryHelpText = "Yeni fotoğraflar eklenir; mevcut galeri korunur.";
        }

        private AnnouncementDetailViewModel BuildNewsDetailModel(AnnouncementDetailDataModel detail)
        {
            var news = detail.Announcement;
            return new AnnouncementDetailViewModel
            {
                Id = news.Id,
                Title = news.Title,
                Content = news.Content ?? string.Empty,
                CreatedDate = news.CreatedDate,
                UpdatedDate = news.UpdateDate,
                IsAdminView = false,
                ContentBodyTitle = "Haber Metni",
                AttachmentSectionTitle = "Haber Ekleri",
                GallerySectionTitle = "Haber Galerisi",
                RelatedSectionTitle = "Diğer Haberler",
                BackButtonText = "Tüm haberlere dön",
                SideActionText = "Tüm Haberler",
                BackController = "Home",
                BackAction = "News",
                DetailController = "News",
                GalleryImages = detail.GalleryImages
                    .Select(x => _announcementImageApiClient.GetFileUrl(x.AnnouncementId, x.FileName))
                    .ToList(),
                Attachments = detail.Attachments
                    .Select(x => new AnnouncementDetailLinkViewModel
                    {
                        Label = x.OriginalFileName,
                        Url = _announcementAttachmentApiClient.GetFileUrl(x.AnnouncementId, x.FileName)
                    })
                    .ToList(),
                RelatedAnnouncements = detail.RelatedAnnouncements
                    .Select(x => new AnnouncementDetailRelatedItemViewModel
                    {
                        Id = x.Id,
                        Title = x.Title,
                        CreatedDate = x.CreatedDate,
                        ImageUrl = detail.RelatedCoverImages.TryGetValue(x.Id, out var image)
                            ? _announcementImageApiClient.GetFileUrl(image.AnnouncementId, image.FileName)
                            : "/content/images/logo.png"
                    })
                    .ToList()
            };
        }

        private void ValidateFiles(AnnouncementFormViewModel model)
        {
            ValidateFileGroup(model.AttachmentFiles, AllowedAttachmentExtensions, nameof(model.AttachmentFiles), "Haber ekleri");
            ValidateFileGroup(model.GalleryFiles, AllowedGalleryExtensions, nameof(model.GalleryFiles), "Haber galerisi");
        }

        private void ValidateFileGroup(IEnumerable<IFormFile> files, HashSet<string> allowedExtensions, string fieldName, string label)
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

        private async Task PopulateExistingAssetsAsync(AnnouncementFormViewModel model, int announcementId)
        {
            model.ExistingAttachments = (await _announcementService.GetAnnouncementAttachmentsAsync(announcementId))
                .Select(x => new AnnouncementAssetViewModel
                {
                    Id = x.Id,
                    Label = x.OriginalFileName,
                    Url = _announcementAttachmentApiClient.GetFileUrl(x.AnnouncementId, x.FileName)
                })
                .ToList();

            model.ExistingGalleryImages = (await _announcementService.GetAnnouncementImagesAsync(announcementId))
                .Select(x => new AnnouncementAssetViewModel
                {
                    Id = x.Id,
                    Label = x.OriginalFileName,
                    Url = _announcementImageApiClient.GetFileUrl(x.AnnouncementId, x.FileName)
                })
                .ToList();
        }

        private async Task<AnnouncementAssetUploadBundleResult> UploadAssetsAsync(int announcementId, AnnouncementFormViewModel model, CancellationToken cancellationToken)
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
                await _announcementService.AddAnnouncementAttachmentAsync(new AnnouncementAssetPersistModel
                {
                    AnnouncementId = announcementId,
                    FileName = attachment.StoredFileName,
                    OriginalFileName = Path.GetFileName(attachment.OriginalFileName),
                    RelativePath = attachment.RelativePath,
                    ContentType = string.IsNullOrWhiteSpace(attachment.ContentType) ? "application/octet-stream" : attachment.ContentType,
                    SizeBytes = attachment.SizeBytes
                });
            }

            foreach (var image in uploadedImages)
            {
                await _announcementService.AddAnnouncementImageAsync(new AnnouncementAssetPersistModel
                {
                    AnnouncementId = announcementId,
                    FileName = image.StoredFileName,
                    OriginalFileName = Path.GetFileName(image.OriginalFileName),
                    RelativePath = image.RelativePath,
                    ContentType = string.IsNullOrWhiteSpace(image.ContentType) ? "application/octet-stream" : image.ContentType,
                    SizeBytes = image.SizeBytes
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

        private static AnnouncementCardViewModel MapAnnouncementCard(AnnouncementCardModel item)
        {
            return new AnnouncementCardViewModel
            {
                Id = item.Id,
                Title = item.Title,
                Summary = item.Summary,
                CreatedDate = item.CreatedDate,
                IsActive = item.IsActive
            };
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
