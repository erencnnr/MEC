using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Application.Abstractions.Service.SchoolService.Model;
using MEC.Portal.Models;
using MEC.Portal.Services;
using Microsoft.AspNetCore.Mvc;

namespace MEC.Portal.Controllers
{
    public class LibraryController : Controller
    {
        private readonly ILibraryService _libraryService;
        private readonly ILibraryAttachmentApiClient _libraryAttachmentApiClient;

        public LibraryController(
            ILibraryService libraryService,
            ILibraryAttachmentApiClient libraryAttachmentApiClient)
        {
            _libraryService = libraryService;
            _libraryAttachmentApiClient = libraryAttachmentApiClient;
        }

        [HttpGet("/Library")]
        public async Task<IActionResult> Index(int? folderId = null, int? documentId = null)
        {
            var explorer = await _libraryService.GetExplorerAsync(folderId, documentId);
            return View(MapLibraryIndex(explorer));
        }

        [HttpGet("/Library/Documents/{id:int}/open")]
        public async Task<IActionResult> OpenDocument(int id)
        {
            var document = await _libraryService.GetDocumentAsync(id);
            if (document == null || string.IsNullOrWhiteSpace(document.FileName))
            {
                return RedirectToAction(nameof(Index));
            }

            return Redirect(_libraryAttachmentApiClient.GetFileUrl(document.Id, document.FileName));
        }

        [HttpGet("/Library/Documents/{id:int}/preview")]
        public async Task<IActionResult> PreviewDocument(int id)
        {
            var document = await _libraryService.GetDocumentAsync(id);
            if (document == null || string.IsNullOrWhiteSpace(document.FileName))
            {
                return RedirectToAction(nameof(Index));
            }

            if (!document.IsPdf)
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

        private LibraryIndexViewModel MapLibraryIndex(LibraryExplorerModel explorer)
        {
            return new LibraryIndexViewModel
            {
                SelectedFolderId = explorer.SelectedFolderId,
                SelectedFolderName = explorer.SelectedFolderName,
                FolderTree = explorer.FolderTree.Select(MapTreeNode).ToList(),
                Breadcrumbs = explorer.Breadcrumbs.Select(MapBreadcrumb).ToList(),
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
                OpenInNewTab = !node.IsFolder,
                NavigateUrl = node.IsFolder
                    ? Url.Action(nameof(Index), "Library", new { folderId = node.FolderId }) ?? $"/Library?folderId={node.FolderId}"
                    : node.IsPdf
                        ? Url.Action(nameof(PreviewDocument), "Library", new { id = node.DocumentId }) ?? string.Empty
                        : Url.Action(nameof(OpenDocument), "Library", new { id = node.DocumentId }) ?? string.Empty,
                Children = node.Children.Select(MapTreeNode).ToList()
            };
        }

        private LibraryContentItemViewModel MapContentItem(LibraryContentItemModel item, int? selectedFolderId)
        {
            var openUrl = item.DocumentId.HasValue
                ? Url.Action(nameof(OpenDocument), "Library", new { id = item.DocumentId.Value }) ?? string.Empty
                : string.Empty;
            var previewUrl = item.DocumentId.HasValue
                ? Url.Action(nameof(PreviewDocument), "Library", new { id = item.DocumentId.Value }) ?? string.Empty
                : string.Empty;

            return new LibraryContentItemViewModel
            {
                FolderId = item.FolderId,
                DocumentId = item.DocumentId,
                Name = item.Name,
                IsFolder = item.IsFolder,
                IsPdf = item.IsPdf,
                NavigateUrl = item.IsFolder
                    ? Url.Action(nameof(Index), "Library", new { folderId = item.FolderId }) ?? $"/Library?folderId={item.FolderId}"
                    : item.IsPdf ? previewUrl : openUrl,
                OpenUrl = openUrl,
                PreviewUrl = previewUrl,
                SelectUrl = item.DocumentId.HasValue && selectedFolderId.HasValue
                    ? $"/Library?folderId={selectedFolderId.Value}&documentId={item.DocumentId.Value}"
                    : string.Empty,
                MetaText = item.MetaText,
                IsSelected = item.IsSelected
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
    }
}
