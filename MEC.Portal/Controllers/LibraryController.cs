using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.School;
using MEC.Portal.Models;
using MEC.Portal.Services;
using Microsoft.AspNetCore.Mvc;

namespace MEC.Portal.Controllers
{
    public class LibraryController : Controller
    {
        private readonly IGenericRepository<LibraryFolder> _libraryFolderRepository;
        private readonly IGenericRepository<LibraryDocument> _libraryDocumentRepository;
        private readonly ILibraryAttachmentApiClient _libraryAttachmentApiClient;

        public LibraryController(
            IGenericRepository<LibraryFolder> libraryFolderRepository,
            IGenericRepository<LibraryDocument> libraryDocumentRepository,
            ILibraryAttachmentApiClient libraryAttachmentApiClient)
        {
            _libraryFolderRepository = libraryFolderRepository;
            _libraryDocumentRepository = libraryDocumentRepository;
            _libraryAttachmentApiClient = libraryAttachmentApiClient;
        }

        [HttpGet("/Library")]
        public async Task<IActionResult> Index(int? folderId = null, int? documentId = null)
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

            var model = new LibraryIndexViewModel
            {
                SelectedFolderId = selectedFolderId,
                SelectedFolderName = selectedFolder?.Name ?? string.Empty,
                SelectedDocumentId = selectedDocument?.Id,
                SelectedDocument = selectedDocument != null ? MapDocumentItem(selectedDocument) : null,
                FolderTree = BuildFolderTree(folders, null, selectedFolderId),
                Breadcrumbs = selectedFolder != null ? BuildBreadcrumbs(selectedFolder.Id, folders) : new List<LibraryBreadcrumbItemViewModel>(),
                Documents = selectedDocuments.Select(MapDocumentItem).ToList()
            };

            return View(model);
        }

        [HttpGet("/Library/Documents/{id:int}/open")]
        public async Task<IActionResult> OpenDocument(int id)
        {
            var document = await _libraryDocumentRepository.GetByIdAsync(id);
            if (document == null || string.IsNullOrWhiteSpace(document.FileName))
            {
                return RedirectToAction(nameof(Index));
            }

            return Redirect(_libraryAttachmentApiClient.GetFileUrl(document.Id, document.FileName));
        }

        [HttpGet("/Library/Documents/{id:int}/preview")]
        public async Task<IActionResult> PreviewDocument(int id)
        {
            var document = await _libraryDocumentRepository.GetByIdAsync(id);
            if (document == null || string.IsNullOrWhiteSpace(document.FileName))
            {
                return RedirectToAction(nameof(Index));
            }

            if (!IsPdf(document))
            {
                return RedirectToAction(nameof(OpenDocument), new { id });
            }

            var downloadResult = await _libraryAttachmentApiClient.DownloadAsync(document.Id, document.FileName);
            if (!downloadResult.IsSuccess)
            {
                TempData["LibraryError"] = downloadResult.Message;
                return RedirectToAction(nameof(Index), new { folderId = document.FolderId });
            }

            return File(downloadResult.Content, "application/pdf");
        }

        private LibraryDocumentItemViewModel MapDocumentItem(LibraryDocument document)
        {
            var previewUrl = Url.Action(nameof(PreviewDocument), "Library", new { id = document.Id }) ?? string.Empty;
            var openUrl = Url.Action(nameof(OpenDocument), "Library", new { id = document.Id }) ?? string.Empty;

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

        private static bool IsPdf(LibraryDocument document)
        {
            return string.Equals(System.IO.Path.GetExtension(document.OriginalFileName), ".pdf", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(document.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase);
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

        private static List<LibraryFolderTreeNodeViewModel> BuildFolderTree(
            IReadOnlyCollection<LibraryFolder> folders,
            int? parentFolderId,
            int? selectedFolderId)
        {
            return folders
                .Where(x => x.ParentFolderId == parentFolderId)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .Select(x => new LibraryFolderTreeNodeViewModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    IsSelected = selectedFolderId == x.Id,
                    Children = BuildFolderTree(folders, x.Id, selectedFolderId)
                })
                .ToList();
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
    }
}
