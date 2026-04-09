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

            var model = new LibraryIndexViewModel
            {
                SelectedFolderId = selectedFolderId,
                SelectedFolderName = selectedFolder?.Name ?? string.Empty,
                FolderTree = BuildFolderTree(folders, documents, null, selectedFolderId),
                Breadcrumbs = selectedFolder != null ? BuildBreadcrumbs(selectedFolder.Id, folders) : new List<LibraryBreadcrumbItemViewModel>(),
                Items = selectedFolderId.HasValue
                    ? BuildContentItems(selectedFolderId.Value, folders, documents)
                    : new List<LibraryContentItemViewModel>()
            };

            if (documentId.HasValue && documents.Any(x => x.Id == documentId.Value))
            {
                var selectedDocument = documents.First(x => x.Id == documentId.Value);
                if (selectedFolderId == selectedDocument.FolderId)
                {
                    model.Items = model.Items
                        .Select(x =>
                        {
                            if (x.DocumentId == selectedDocument.Id)
                            {
                                x.MetaText = $"{x.MetaText} · Seçili";
                            }

                            return x;
                        })
                        .ToList();
                }
            }

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

        private List<LibraryContentItemViewModel> BuildContentItems(
            int selectedFolderId,
            IReadOnlyCollection<LibraryFolder> folders,
            IReadOnlyCollection<LibraryDocument> documents)
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
                    NavigateUrl = Url.Action(nameof(Index), "Library", new { folderId = x.Id }) ?? $"/Library?folderId={x.Id}",
                    MetaText = "Klasör"
                });

            var documentItems = documents
                .Where(x => x.FolderId == selectedFolderId)
                .OrderByDescending(x => x.CreatedDate)
                .ThenBy(x => x.OriginalFileName)
                .Select(x =>
                {
                    var isPdf = IsPdf(x);
                    var openUrl = Url.Action(nameof(OpenDocument), "Library", new { id = x.Id }) ?? string.Empty;
                    var previewUrl = Url.Action(nameof(PreviewDocument), "Library", new { id = x.Id }) ?? string.Empty;

                    return new LibraryContentItemViewModel
                    {
                        DocumentId = x.Id,
                        Name = x.OriginalFileName,
                        IsFolder = false,
                        IsPdf = isPdf,
                        NavigateUrl = isPdf ? previewUrl : openUrl,
                        OpenUrl = openUrl,
                        PreviewUrl = previewUrl,
                        SelectUrl = $"/Library?folderId={selectedFolderId}&documentId={x.Id}",
                        MetaText = BuildDocumentMetaText(x)
                    };
                });

            return folderItems.Concat(documentItems).ToList();
        }

        private List<LibraryTreeNodeViewModel> BuildFolderTree(
            IReadOnlyCollection<LibraryFolder> folders,
            IReadOnlyCollection<LibraryDocument> documents,
            int? parentFolderId,
            int? selectedFolderId)
        {
            return folders
                .Where(x => x.ParentFolderId == parentFolderId)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .Select(folder =>
                {
                    var childFolders = BuildFolderTree(folders, documents, folder.Id, selectedFolderId);
                    var childDocuments = documents
                        .Where(x => x.FolderId == folder.Id)
                        .OrderByDescending(x => x.CreatedDate)
                        .ThenBy(x => x.OriginalFileName)
                        .Select(x =>
                        {
                            var isPdf = IsPdf(x);
                            return new LibraryTreeNodeViewModel
                            {
                                DocumentId = x.Id,
                                Name = x.OriginalFileName,
                                IsFolder = false,
                                IsPdf = isPdf,
                                OpenInNewTab = true,
                                NavigateUrl = isPdf
                                    ? Url.Action(nameof(PreviewDocument), "Library", new { id = x.Id }) ?? string.Empty
                                    : Url.Action(nameof(OpenDocument), "Library", new { id = x.Id }) ?? string.Empty
                            };
                        });

                    return new LibraryTreeNodeViewModel
                    {
                        FolderId = folder.Id,
                        Name = folder.Name,
                        IsFolder = true,
                        IsSelected = selectedFolderId == folder.Id,
                        NavigateUrl = Url.Action(nameof(Index), "Library", new { folderId = folder.Id }) ?? $"/Library?folderId={folder.Id}",
                        Children = childFolders.Concat(childDocuments).ToList()
                    };
                })
                .ToList();
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
