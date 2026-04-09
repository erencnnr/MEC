using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.School;
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

        private readonly IGenericRepository<LibraryFolder> _libraryFolderRepository;
        private readonly IGenericRepository<LibraryDocument> _libraryDocumentRepository;
        private readonly ILibraryAttachmentApiClient _libraryAttachmentApiClient;

        public ContentController(
            IGenericRepository<LibraryFolder> libraryFolderRepository,
            IGenericRepository<LibraryDocument> libraryDocumentRepository,
            ILibraryAttachmentApiClient libraryAttachmentApiClient)
        {
            _libraryFolderRepository = libraryFolderRepository;
            _libraryDocumentRepository = libraryDocumentRepository;
            _libraryAttachmentApiClient = libraryAttachmentApiClient;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(int? folderId = null, int? documentId = null)
        {
            var model = await BuildViewModelAsync(folderId, documentId);
            return View(model);
        }

        [HttpPost("CreateFolder")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFolder(string? name, int? parentFolderId)
        {
            var normalizedName = NormalizeFolderName(name);
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                TempData["ContentError"] = "Klasör adı boş bırakılamaz.";
                return RedirectToAction(nameof(Index), new { folderId = parentFolderId });
            }

            var folders = (await _libraryFolderRepository.GetAllAsync()).ToList();
            if (!ParentFolderExists(parentFolderId, folders))
            {
                TempData["ContentError"] = "Seçilen üst klasör bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            if (HasFolderNameConflict(folders, normalizedName, parentFolderId, null))
            {
                TempData["ContentError"] = "Aynı klasör altında bu isimde başka bir klasör zaten var.";
                return RedirectToAction(nameof(Index), new { folderId = parentFolderId });
            }

            var nextDisplayOrder = folders
                .Where(x => x.ParentFolderId == parentFolderId)
                .Select(x => x.DisplayOrder)
                .DefaultIfEmpty(0)
                .Max() + 1;

            var folder = new LibraryFolder
            {
                Name = normalizedName,
                ParentFolderId = parentFolderId,
                DisplayOrder = nextDisplayOrder,
                CreatedDate = DateTime.Now,
                UpdateDate = DateTime.Now
            };

            await _libraryFolderRepository.AddAsync(folder);
            TempData["ContentSuccess"] = "Klasör oluşturuldu.";
            return RedirectToAction(nameof(Index), new { folderId = folder.Id });
        }

        [HttpPost("RenameFolder/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RenameFolder(int id, string? name)
        {
            var folders = (await _libraryFolderRepository.GetAllAsync()).ToList();
            var folder = folders.FirstOrDefault(x => x.Id == id);
            if (folder == null)
            {
                TempData["ContentError"] = "Güncellenecek klasör bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            var normalizedName = NormalizeFolderName(name);
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                TempData["ContentError"] = "Klasör adı boş bırakılamaz.";
                return RedirectToAction(nameof(Index), new { folderId = id });
            }

            if (HasFolderNameConflict(folders, normalizedName, folder.ParentFolderId, id))
            {
                TempData["ContentError"] = "Aynı klasör altında bu isimde başka bir klasör zaten var.";
                return RedirectToAction(nameof(Index), new { folderId = id });
            }

            folder.Name = normalizedName;
            folder.UpdateDate = DateTime.Now;
            _libraryFolderRepository.Update(folder);

            TempData["ContentSuccess"] = "Klasör adı güncellendi.";
            return RedirectToAction(nameof(Index), new { folderId = id });
        }

        [HttpPost("DeleteFolder/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFolder(int id)
        {
            var folders = (await _libraryFolderRepository.GetAllAsync()).ToList();
            var folder = folders.FirstOrDefault(x => x.Id == id);
            if (folder == null)
            {
                TempData["ContentError"] = "Silinecek klasör bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            var descendantFolderIds = GetDescendantFolderIds(id, folders);
            descendantFolderIds.Add(id);

            var documents = (await _libraryDocumentRepository.GetAllAsync(x => descendantFolderIds.Contains(x.FolderId))).ToList();
            foreach (var document in documents)
            {
                if (!string.IsNullOrWhiteSpace(document.FileName))
                {
                    await _libraryAttachmentApiClient.DeleteAsync(document.Id, document.FileName);
                }

                _libraryDocumentRepository.Delete(document);
            }

            var folderLookup = folders.ToDictionary(x => x.Id);
            var foldersToDelete = folders
                .Where(x => descendantFolderIds.Contains(x.Id))
                .OrderByDescending(x => GetFolderDepth(x, folderLookup))
                .ToList();

            foreach (var item in foldersToDelete)
            {
                _libraryFolderRepository.Delete(item);
            }

            await NormalizeFolderOrdersAsync(folder.ParentFolderId);

            TempData["ContentSuccess"] = "Klasör ve bağlı içerikleri silindi.";
            return RedirectToAction(nameof(Index), new { folderId = folder.ParentFolderId });
        }

        [HttpPost("UploadDocuments")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocuments(int folderId, List<IFormFile>? files)
        {
            var folder = await _libraryFolderRepository.GetByIdAsync(folderId);
            if (folder == null)
            {
                TempData["ContentError"] = "Doküman yüklenecek klasör bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

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

                var document = new LibraryDocument
                {
                    FolderId = folderId,
                    OriginalFileName = Path.GetFileName(file.FileName),
                    ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                    SizeBytes = file.Length,
                    CreatedDate = DateTime.Now,
                    UpdateDate = DateTime.Now
                };

                await _libraryDocumentRepository.AddAsync(document);

                var uploadResult = await _libraryAttachmentApiClient.UploadAsync(document.Id, file);
                if (!uploadResult.IsSuccess)
                {
                    _libraryDocumentRepository.Delete(document);
                    failedMessages.Add($"{file.FileName} yüklenemedi: {uploadResult.Message}");
                    continue;
                }

                document.FileName = uploadResult.FileName;
                document.RelativePath = uploadResult.RelativePath;
                document.ContentType = string.IsNullOrWhiteSpace(uploadResult.ContentType) ? document.ContentType : uploadResult.ContentType;
                document.UpdateDate = DateTime.Now;
                _libraryDocumentRepository.Update(document);
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
            var document = await _libraryDocumentRepository.GetByIdAsync(id);
            if (document == null)
            {
                TempData["ContentError"] = "Güncellenecek doküman bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            var normalizedName = NormalizeDocumentName(name, document.OriginalFileName);
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                TempData["ContentError"] = "Doküman adı boş bırakılamaz.";
                return RedirectToAction(nameof(Index), new { folderId = document.FolderId, documentId = id });
            }

            document.OriginalFileName = normalizedName;
            document.UpdateDate = DateTime.Now;
            _libraryDocumentRepository.Update(document);

            TempData["ContentSuccess"] = "Doküman adı güncellendi.";
            return RedirectToAction(nameof(Index), new { folderId = document.FolderId, documentId = id });
        }

        [HttpPost("DeleteDocument/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var document = await _libraryDocumentRepository.GetByIdAsync(id);
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

            var folderId = document.FolderId;
            _libraryDocumentRepository.Delete(document);

            TempData["ContentSuccess"] = "Doküman silindi.";
            return RedirectToAction(nameof(Index), new { folderId });
        }

        private async Task<AdminContentManagementViewModel> BuildViewModelAsync(int? folderId, int? documentId)
        {
            var folders = (await _libraryFolderRepository.GetAllAsync())
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ToList();
            var documents = (await _libraryDocumentRepository.GetAllAsync())
                .OrderByDescending(x => x.CreatedDate)
                .ThenByDescending(x => x.Id)
                .ToList();

            var selectedFolder = ResolveSelectedFolder(folderId, folders);
            var selectedFolderId = selectedFolder?.Id;
            var selectedDocuments = selectedFolderId.HasValue
                ? documents.Where(x => x.FolderId == selectedFolderId.Value).ToList()
                : new List<LibraryDocument>();
            var selectedDocument = documentId.HasValue
                ? selectedDocuments.FirstOrDefault(x => x.Id == documentId.Value)
                : null;

            return new AdminContentManagementViewModel
            {
                SelectedFolderId = selectedFolderId,
                SelectedFolderName = selectedFolder?.Name ?? string.Empty,
                SelectedDocumentId = selectedDocument?.Id,
                SelectedDocument = selectedDocument != null ? MapDocumentItem(selectedDocument) : null,
                FolderTree = BuildFolderTree(folders, documents, null, selectedFolderId, documentId),
                Breadcrumbs = selectedFolder != null ? BuildBreadcrumbs(selectedFolder.Id, folders) : new List<LibraryBreadcrumbItemViewModel>(),
                Documents = selectedDocuments.Select(MapDocumentItem).ToList(),
                Items = selectedFolderId.HasValue
                    ? BuildContentItems(selectedFolderId.Value, folders, documents, documentId)
                    : new List<LibraryContentItemViewModel>()
            };
        }

        private List<LibraryContentItemViewModel> BuildContentItems(
            int selectedFolderId,
            IReadOnlyCollection<LibraryFolder> folders,
            IReadOnlyCollection<LibraryDocument> documents,
            int? selectedDocumentId)
        {
            var folderItems = folders
                .Where(x => x.ParentFolderId == selectedFolderId)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .Select(x => new LibraryContentItemViewModel
                {
                    FolderId = x.Id,
                    Name = x.Name,
                    IsFolder = true,
                    NavigateUrl = Url.Action(nameof(Index), new { folderId = x.Id }) ?? $"/Admin/Content?folderId={x.Id}",
                    MetaText = "Alt klasör"
                });

            var documentItems = documents
                .Where(x => x.FolderId == selectedFolderId)
                .OrderByDescending(x => x.CreatedDate)
                .ThenBy(x => x.OriginalFileName)
                .Select(x =>
                {
                    var isPdf = IsPdf(x);
                    var selectUrl = Url.Action(nameof(Index), new { folderId = selectedFolderId, documentId = x.Id }) ?? $"/Admin/Content?folderId={selectedFolderId}&documentId={x.Id}";
                    var openUrl = Url.Action("OpenDocument", "Library", new { id = x.Id }) ?? string.Empty;
                    var previewUrl = Url.Action("PreviewDocument", "Library", new { id = x.Id }) ?? string.Empty;

                    return new LibraryContentItemViewModel
                    {
                        DocumentId = x.Id,
                        Name = x.OriginalFileName,
                        IsFolder = false,
                        IsPdf = isPdf,
                        IsSelected = selectedDocumentId == x.Id,
                        NavigateUrl = selectUrl,
                        SelectUrl = selectUrl,
                        OpenUrl = openUrl,
                        PreviewUrl = previewUrl,
                        MetaText = BuildDocumentMetaText(x)
                    };
                });

            return folderItems.Concat(documentItems).ToList();
        }

        private List<LibraryTreeNodeViewModel> BuildFolderTree(
            IReadOnlyCollection<LibraryFolder> folders,
            IReadOnlyCollection<LibraryDocument> documents,
            int? parentFolderId,
            int? selectedFolderId,
            int? selectedDocumentId)
        {
            return folders
                .Where(x => x.ParentFolderId == parentFolderId)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .Select(folder =>
                {
                    var childFolders = BuildFolderTree(folders, documents, folder.Id, selectedFolderId, selectedDocumentId);
                    var childDocuments = documents
                        .Where(x => x.FolderId == folder.Id)
                        .OrderByDescending(x => x.CreatedDate)
                        .ThenBy(x => x.OriginalFileName)
                        .Select(x => new LibraryTreeNodeViewModel
                        {
                            DocumentId = x.Id,
                            Name = x.OriginalFileName,
                            IsFolder = false,
                            IsPdf = IsPdf(x),
                            IsSelected = selectedDocumentId == x.Id,
                            NavigateUrl = Url.Action(nameof(Index), new { folderId = folder.Id, documentId = x.Id }) ?? $"/Admin/Content?folderId={folder.Id}&documentId={x.Id}",
                            OpenInNewTab = false
                        });

                    return new LibraryTreeNodeViewModel
                    {
                        FolderId = folder.Id,
                        Name = folder.Name,
                        IsFolder = true,
                        IsSelected = selectedFolderId == folder.Id,
                        NavigateUrl = Url.Action(nameof(Index), new { folderId = folder.Id }) ?? $"/Admin/Content?folderId={folder.Id}",
                        Children = childFolders.Concat(childDocuments).ToList()
                    };
                })
                .ToList();
        }

        private LibraryDocumentItemViewModel MapDocumentItem(LibraryDocument document)
        {
            var previewUrl = Url.Action("PreviewDocument", "Library", new { id = document.Id }) ?? string.Empty;
            var openUrl = Url.Action("OpenDocument", "Library", new { id = document.Id }) ?? string.Empty;

            return new LibraryDocumentItemViewModel
            {
                Id = document.Id,
                OriginalFileName = document.OriginalFileName,
                ContentType = document.ContentType,
                SizeBytes = document.SizeBytes,
                CreatedDate = document.CreatedDate,
                IsPdf = IsPdf(document),
                OpenUrl = openUrl,
                PreviewUrl = previewUrl
            };
        }

        private static string BuildDocumentMetaText(LibraryDocument document)
        {
            var parts = new List<string>();
            if (document.CreatedDate.HasValue)
            {
                parts.Add(document.CreatedDate.Value.ToString("dd.MM.yyyy"));
            }

            if (document.SizeBytes > 0)
            {
                var sizeInKb = document.SizeBytes / 1024d;
                parts.Add(sizeInKb < 1024 ? $"{sizeInKb:0.#} KB" : $"{sizeInKb / 1024d:0.#} MB");
            }

            return parts.Count > 0 ? string.Join(" · ", parts) : "Doküman";
        }

        private static List<LibraryBreadcrumbItemViewModel> BuildBreadcrumbs(int folderId, IReadOnlyCollection<LibraryFolder> folders)
        {
            var lookup = folders.ToDictionary(x => x.Id);
            var path = new List<LibraryBreadcrumbItemViewModel>();
            var currentId = folderId;

            while (lookup.TryGetValue(currentId, out var folder))
            {
                path.Add(new LibraryBreadcrumbItemViewModel
                {
                    Id = folder.Id,
                    Name = folder.Name
                });

                if (!folder.ParentFolderId.HasValue)
                {
                    break;
                }

                currentId = folder.ParentFolderId.Value;
            }

            path.Reverse();
            return path;
        }

        private static LibraryFolder? ResolveSelectedFolder(int? folderId, IReadOnlyCollection<LibraryFolder> folders)
        {
            if (folderId.HasValue)
            {
                var byId = folders.FirstOrDefault(x => x.Id == folderId.Value);
                if (byId != null)
                {
                    return byId;
                }
            }

            return folders
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .FirstOrDefault();
        }

        private static bool ParentFolderExists(int? parentFolderId, IReadOnlyCollection<LibraryFolder> folders)
        {
            return !parentFolderId.HasValue || folders.Any(x => x.Id == parentFolderId.Value);
        }

        private static bool HasFolderNameConflict(IReadOnlyCollection<LibraryFolder> folders, string name, int? parentFolderId, int? currentFolderId)
        {
            return folders.Any(x =>
                x.ParentFolderId == parentFolderId &&
                x.Id != currentFolderId &&
                string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeFolderName(string? name)
        {
            return (name ?? string.Empty).Trim();
        }

        private static string NormalizeDocumentName(string? name, string currentFileName)
        {
            var trimmedName = (name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                return string.Empty;
            }

            var currentExtension = Path.GetExtension(currentFileName);
            var normalizedBaseName = Path.GetFileNameWithoutExtension(trimmedName).Trim();
            if (string.IsNullOrWhiteSpace(normalizedBaseName))
            {
                normalizedBaseName = Path.GetFileNameWithoutExtension(currentFileName).Trim();
            }

            return string.IsNullOrWhiteSpace(currentExtension)
                ? normalizedBaseName
                : $"{normalizedBaseName}{currentExtension}";
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

        private static bool IsPdf(LibraryDocument document)
        {
            return string.Equals(Path.GetExtension(document.OriginalFileName), ".pdf", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(document.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase);
        }

        private static List<int> GetDescendantFolderIds(int folderId, IReadOnlyCollection<LibraryFolder> folders)
        {
            var directChildren = folders.Where(x => x.ParentFolderId == folderId).Select(x => x.Id).ToList();
            var result = new List<int>(directChildren);

            foreach (var childId in directChildren)
            {
                result.AddRange(GetDescendantFolderIds(childId, folders));
            }

            return result;
        }

        private static int GetFolderDepth(LibraryFolder folder, IReadOnlyDictionary<int, LibraryFolder> folderLookup)
        {
            var depth = 0;
            var current = folder;
            while (current.ParentFolderId.HasValue && folderLookup.TryGetValue(current.ParentFolderId.Value, out var parentFolder))
            {
                depth++;
                current = parentFolder;
            }

            return depth;
        }

        private async Task NormalizeFolderOrdersAsync(int? parentFolderId)
        {
            var siblings = (await _libraryFolderRepository.GetAllAsync(x => x.ParentFolderId == parentFolderId))
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ToList();

            for (var index = 0; index < siblings.Count; index++)
            {
                var targetOrder = index + 1;
                if (siblings[index].DisplayOrder == targetOrder)
                {
                    continue;
                }

                siblings[index].DisplayOrder = targetOrder;
                siblings[index].UpdateDate = DateTime.Now;
                _libraryFolderRepository.Update(siblings[index]);
            }
        }
    }
}
