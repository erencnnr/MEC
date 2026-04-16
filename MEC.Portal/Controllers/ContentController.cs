using MEC.Application.Abstractions.Common.Models;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Application.Abstractions.Service.SchoolService.Model;
using MEC.Portal.Models;
using MEC.Portal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MEC.Portal.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/Content")]
    public class ContentController : Controller
    {
        private static readonly HashSet<string> AllowedDocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".txt", ".doc", ".docx", ".xls", ".xlsx"
        };

        private readonly ILibraryService _libraryService;
        private readonly ILibraryAttachmentApiClient _libraryAttachmentApiClient;

        public ContentController(
            ILibraryService libraryService,
            ILibraryAttachmentApiClient libraryAttachmentApiClient)
        {
            _libraryService = libraryService;
            _libraryAttachmentApiClient = libraryAttachmentApiClient;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(int? folderId = null, int? documentId = null)
        {
            var explorer = await _libraryService.GetExplorerAsync(folderId, documentId);
            return View(MapAdminContent(explorer));
        }

        [HttpPost("CreateFolder")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFolder(string? name, int? parentFolderId)
        {
            var result = await _libraryService.CreateFolderAsync(name, parentFolderId);
            if (!result.IsSuccess)
            {
                TempData["ContentError"] = result.Message;
                return RedirectToAction(nameof(Index), new { folderId = parentFolderId });
            }

            TempData["ContentSuccess"] = result.Message;
            return RedirectToAction(nameof(Index), new { folderId = result.Data });
        }

        [HttpPost("RenameFolder/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RenameFolder(int id, string? name)
        {
            var result = await _libraryService.RenameFolderAsync(id, name);
            TempData[result.IsSuccess ? "ContentSuccess" : "ContentError"] = result.Message;
            return RedirectToAction(nameof(Index), new { folderId = id });
        }

        [HttpPost("DeleteFolder/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFolder(int id)
        {
            var prepare = await _libraryService.PrepareDeleteFolderAsync(id);
            if (!prepare.IsSuccess || prepare.Data == null)
            {
                TempData["ContentError"] = prepare.Message;
                return RedirectToAction(nameof(Index));
            }

            foreach (var document in prepare.Data.Documents.Where(x => !string.IsNullOrWhiteSpace(x.FileName)))
            {
                await _libraryAttachmentApiClient.DeleteAsync(document.Id, document.FileName);
            }

            var result = await _libraryService.DeleteFolderMetadataAsync(id);
            TempData[result.IsSuccess ? "ContentSuccess" : "ContentError"] = result.Message;
            return RedirectToAction(nameof(Index), new { folderId = prepare.Data.ParentFolderId });
        }

        [HttpPost("UploadDocuments")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocuments(int folderId, List<IFormFile>? files)
        {
            if (files == null || files.Count == 0)
            {
                TempData["ContentError"] = "Yüklenecek en az bir doküman seçin.";
                return RedirectToAction(nameof(Index), new { folderId });
            }

            var uploadedCount = 0;
            var failedMessages = new List<string>();

            foreach (var file in files.Where(x => x != null && x.Length > 0))
            {
                if (!IsAllowedDocument(file))
                {
                    failedMessages.Add($"{file.FileName} desteklenmeyen bir dosya türü.");
                    continue;
                }

                var createResult = await _libraryService.CreateDocumentPlaceholderAsync(new LibraryDocumentCreateModel
                {
                    FolderId = folderId,
                    OriginalFileName = file.FileName,
                    ContentType = file.ContentType,
                    SizeBytes = file.Length
                });

                if (!createResult.IsSuccess || createResult.Data == null)
                {
                    failedMessages.Add($"{file.FileName} yüklenemedi: {createResult.Message}");
                    continue;
                }

                var uploadResult = await _libraryAttachmentApiClient.UploadAsync(createResult.Data.Id, file);
                if (!uploadResult.IsSuccess)
                {
                    await _libraryService.DeleteDocumentMetadataAsync(createResult.Data.Id);
                    failedMessages.Add($"{file.FileName} yüklenemedi: {uploadResult.Message}");
                    continue;
                }

                await _libraryService.CompleteDocumentUploadAsync(createResult.Data.Id, new StoredFileModel
                {
                    FileName = uploadResult.FileName,
                    OriginalFileName = file.FileName,
                    RelativePath = uploadResult.RelativePath,
                    ContentType = string.IsNullOrWhiteSpace(uploadResult.ContentType) ? file.ContentType ?? string.Empty : uploadResult.ContentType,
                    SizeBytes = file.Length
                });

                uploadedCount++;
            }

            if (uploadedCount > 0)
            {
                TempData["ContentSuccess"] = uploadedCount == 1
                    ? "Doküman yüklendi."
                    : $"{uploadedCount} doküman yüklendi.";
            }

            if (failedMessages.Count > 0)
            {
                TempData["ContentError"] = string.Join(" ", failedMessages);
            }

            return RedirectToAction(nameof(Index), new { folderId });
        }

        [HttpPost("RenameDocument/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RenameDocument(int id, string? name)
        {
            var document = await _libraryService.GetDocumentAsync(id);
            if (document == null)
            {
                TempData["ContentError"] = "Güncellenecek doküman bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _libraryService.RenameDocumentAsync(id, name);
            TempData[result.IsSuccess ? "ContentSuccess" : "ContentError"] = result.Message;
            return RedirectToAction(nameof(Index), new { folderId = document.FolderId, documentId = id });
        }

        [HttpPost("DeleteDocument/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var document = await _libraryService.GetDocumentAsync(id);
            if (document == null)
            {
                TempData["ContentError"] = "Silinecek doküman bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrWhiteSpace(document.FileName))
            {
                var deleteResult = await _libraryAttachmentApiClient.DeleteAsync(document.Id, document.FileName);
                if (!deleteResult.IsSuccess)
                {
                    TempData["ContentError"] = deleteResult.Message;
                    return RedirectToAction(nameof(Index), new { folderId = document.FolderId });
                }
            }

            var result = await _libraryService.DeleteDocumentMetadataAsync(id);
            TempData[result.IsSuccess ? "ContentSuccess" : "ContentError"] = result.Message;
            return RedirectToAction(nameof(Index), new { folderId = document.FolderId });
        }

        private AdminContentManagementViewModel MapAdminContent(LibraryExplorerModel explorer)
        {
            return new AdminContentManagementViewModel
            {
                SelectedFolderId = explorer.SelectedFolderId,
                SelectedFolderName = explorer.SelectedFolderName,
                SelectedDocumentId = explorer.SelectedDocumentId,
                SelectedDocument = explorer.SelectedDocument != null ? MapDocumentItem(explorer.SelectedDocument) : null,
                FolderTree = explorer.FolderTree.Select(MapTreeNode).ToList(),
                Breadcrumbs = explorer.Breadcrumbs.Select(MapBreadcrumb).ToList(),
                Documents = explorer.Items.Where(x => !x.IsFolder && x.DocumentId.HasValue).Select(x => new LibraryDocumentItemViewModel
                {
                    Id = x.DocumentId!.Value,
                    OriginalFileName = x.Name,
                    IsPdf = x.IsPdf,
                    OpenUrl = Url.Action("OpenDocument", "Library", new { id = x.DocumentId.Value }) ?? string.Empty,
                    PreviewUrl = Url.Action("PreviewDocument", "Library", new { id = x.DocumentId.Value }) ?? string.Empty
                }).ToList(),
                Items = explorer.Items.Select(x => MapContentItem(x, explorer.SelectedFolderId)).ToList()
            };
        }

        private LibraryTreeNodeViewModel MapTreeNode(LibraryTreeNodeModel node)
        {
            return new LibraryTreeNodeViewModel
            {
                FolderId = node.FolderId,
                DocumentId = node.DocumentId,
                Name = node.Name,
                IsFolder = node.IsFolder,
                IsPdf = node.IsPdf,
                IsSelected = node.IsSelected,
                NavigateUrl = node.IsFolder
                    ? Url.Action(nameof(Index), new { folderId = node.FolderId }) ?? $"/Admin/Content?folderId={node.FolderId}"
                    : Url.Action(nameof(Index), new { folderId = node.FolderId, documentId = node.DocumentId }) ?? string.Empty,
                Children = node.Children.Select(MapTreeNode).ToList()
            };
        }

        private LibraryContentItemViewModel MapContentItem(LibraryContentItemModel item, int? selectedFolderId)
        {
            var selectUrl = item.DocumentId.HasValue && selectedFolderId.HasValue
                ? Url.Action(nameof(Index), new { folderId = selectedFolderId.Value, documentId = item.DocumentId.Value }) ?? string.Empty
                : string.Empty;
            var openUrl = item.DocumentId.HasValue
                ? Url.Action("OpenDocument", "Library", new { id = item.DocumentId.Value }) ?? string.Empty
                : string.Empty;
            var previewUrl = item.DocumentId.HasValue
                ? Url.Action("PreviewDocument", "Library", new { id = item.DocumentId.Value }) ?? string.Empty
                : string.Empty;

            return new LibraryContentItemViewModel
            {
                FolderId = item.FolderId,
                DocumentId = item.DocumentId,
                Name = item.Name,
                IsFolder = item.IsFolder,
                IsPdf = item.IsPdf,
                IsSelected = item.IsSelected,
                NavigateUrl = item.IsFolder
                    ? Url.Action(nameof(Index), new { folderId = item.FolderId }) ?? $"/Admin/Content?folderId={item.FolderId}"
                    : selectUrl,
                SelectUrl = selectUrl,
                OpenUrl = openUrl,
                PreviewUrl = previewUrl,
                MetaText = item.IsFolder ? "Alt klasör" : item.MetaText
            };
        }

        private LibraryDocumentItemViewModel MapDocumentItem(LibraryDocumentModel document)
        {
            return new LibraryDocumentItemViewModel
            {
                Id = document.Id,
                OriginalFileName = document.OriginalFileName,
                ContentType = document.ContentType,
                SizeBytes = document.SizeBytes,
                CreatedDate = document.CreatedDate,
                IsPdf = document.IsPdf,
                OpenUrl = Url.Action("OpenDocument", "Library", new { id = document.Id }) ?? string.Empty,
                PreviewUrl = Url.Action("PreviewDocument", "Library", new { id = document.Id }) ?? string.Empty
            };
        }

        private static LibraryBreadcrumbItemViewModel MapBreadcrumb(LibraryBreadcrumbItemModel item)
        {
            return new LibraryBreadcrumbItemViewModel
            {
                Id = item.Id,
                Name = item.Name
            };
        }

        private static bool IsAllowedDocument(IFormFile file)
        {
            if (file.Length <= 0)
            {
                return false;
            }

            var extension = Path.GetExtension(file.FileName);
            return !string.IsNullOrWhiteSpace(extension) && AllowedDocumentExtensions.Contains(extension);
        }
    }
}
