using MEC.Application.Abstractions.Common.Models;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Application.Abstractions.Service.SchoolService.Model;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.School;

namespace MEC.Application.Service.SchoolService
{
    public class LibraryService : ILibraryService
    {
        private readonly IGenericRepository<LibraryFolder> _libraryFolderRepository;
        private readonly IGenericRepository<LibraryDocument> _libraryDocumentRepository;

        public LibraryService(
            IGenericRepository<LibraryFolder> libraryFolderRepository,
            IGenericRepository<LibraryDocument> libraryDocumentRepository)
        {
            _libraryFolderRepository = libraryFolderRepository;
            _libraryDocumentRepository = libraryDocumentRepository;
        }

        public async Task<LibraryExplorerModel> GetExplorerAsync(int? folderId, int? documentId)
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

            return new LibraryExplorerModel
            {
                SelectedFolderId = selectedFolderId,
                SelectedFolderName = selectedFolder?.Name ?? string.Empty,
                SelectedDocumentId = selectedDocument?.Id,
                SelectedDocument = selectedDocument != null ? MapDocument(selectedDocument) : null,
                FolderTree = BuildFolderTree(folders, documents, null, selectedFolderId, documentId),
                Breadcrumbs = selectedFolder != null ? BuildBreadcrumbs(selectedFolder.Id, folders) : new List<LibraryBreadcrumbItemModel>(),
                Items = selectedFolderId.HasValue
                    ? BuildContentItems(selectedFolderId.Value, folders, documents, documentId)
                    : new List<LibraryContentItemModel>()
            };
        }

        public async Task<LibraryDocumentModel?> GetDocumentAsync(int id)
        {
            var document = await _libraryDocumentRepository.GetByIdAsync(id);
            return document == null ? null : MapDocument(document);
        }

        public async Task<OperationResultModel<int>> CreateFolderAsync(string? name, int? parentFolderId)
        {
            var normalizedName = NormalizeFolderName(name);
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return OperationResultModel<int>.Fail("Klasör adı boş bırakılamaz.");
            }

            var folders = (await _libraryFolderRepository.GetAllAsync()).ToList();
            if (!ParentFolderExists(parentFolderId, folders))
            {
                return OperationResultModel<int>.Fail("Seçilen üst klasör bulunamadı.");
            }

            if (HasFolderNameConflict(folders, normalizedName, parentFolderId, null))
            {
                return OperationResultModel<int>.Fail("Aynı klasör altında bu isimde başka bir klasör zaten var.");
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
            return OperationResultModel<int>.Success(folder.Id, "Klasör oluşturuldu.");
        }

        public async Task<OperationResultModel> RenameFolderAsync(int id, string? name)
        {
            var folders = (await _libraryFolderRepository.GetAllAsync()).ToList();
            var folder = folders.FirstOrDefault(x => x.Id == id);
            if (folder == null)
            {
                return OperationResultModel.Fail("Güncellenecek klasör bulunamadı.");
            }

            var normalizedName = NormalizeFolderName(name);
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return OperationResultModel.Fail("Klasör adı boş bırakılamaz.");
            }

            if (HasFolderNameConflict(folders, normalizedName, folder.ParentFolderId, id))
            {
                return OperationResultModel.Fail("Aynı klasör altında bu isimde başka bir klasör zaten var.");
            }

            folder.Name = normalizedName;
            folder.UpdateDate = DateTime.Now;
            _libraryFolderRepository.Update(folder);
            return OperationResultModel.Success("Klasör adı güncellendi.");
        }

        public async Task<OperationResultModel<LibraryFolderDeletePrepareModel>> PrepareDeleteFolderAsync(int id)
        {
            var folders = (await _libraryFolderRepository.GetAllAsync()).ToList();
            var folder = folders.FirstOrDefault(x => x.Id == id);
            if (folder == null)
            {
                return OperationResultModel<LibraryFolderDeletePrepareModel>.Fail("Silinecek klasör bulunamadı.");
            }

            var descendantFolderIds = GetDescendantFolderIds(id, folders);
            descendantFolderIds.Add(id);
            var documents = (await _libraryDocumentRepository.GetAllAsync(x => descendantFolderIds.Contains(x.FolderId)))
                .Select(MapDocument)
                .ToList();

            return OperationResultModel<LibraryFolderDeletePrepareModel>.Success(new LibraryFolderDeletePrepareModel
            {
                ParentFolderId = folder.ParentFolderId,
                Documents = documents
            });
        }

        public async Task<OperationResultModel> DeleteFolderMetadataAsync(int id)
        {
            var folders = (await _libraryFolderRepository.GetAllAsync()).ToList();
            var folder = folders.FirstOrDefault(x => x.Id == id);
            if (folder == null)
            {
                return OperationResultModel.Fail("Silinecek klasör bulunamadı.");
            }

            var parentFolderId = folder.ParentFolderId;
            var descendantFolderIds = GetDescendantFolderIds(id, folders);
            descendantFolderIds.Add(id);
            var documents = (await _libraryDocumentRepository.GetAllAsync(x => descendantFolderIds.Contains(x.FolderId))).ToList();
            foreach (var document in documents)
            {
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

            await NormalizeFolderOrdersAsync(parentFolderId);
            return OperationResultModel.Success("Klasör ve bağlı içerikleri silindi.");
        }

        public async Task<OperationResultModel<LibraryDocumentModel>> CreateDocumentPlaceholderAsync(LibraryDocumentCreateModel model)
        {
            var folder = await _libraryFolderRepository.GetByIdAsync(model.FolderId);
            if (folder == null)
            {
                return OperationResultModel<LibraryDocumentModel>.Fail("Doküman yüklenecek klasör bulunamadı.");
            }

            var document = new LibraryDocument
            {
                FolderId = model.FolderId,
                OriginalFileName = Path.GetFileName(model.OriginalFileName),
                ContentType = string.IsNullOrWhiteSpace(model.ContentType) ? "application/octet-stream" : model.ContentType,
                SizeBytes = model.SizeBytes,
                CreatedDate = DateTime.Now,
                UpdateDate = DateTime.Now
            };

            await _libraryDocumentRepository.AddAsync(document);
            return OperationResultModel<LibraryDocumentModel>.Success(MapDocument(document));
        }

        public async Task<OperationResultModel> CompleteDocumentUploadAsync(int id, StoredFileModel storedFile)
        {
            var document = await _libraryDocumentRepository.GetByIdAsync(id);
            if (document == null)
            {
                return OperationResultModel.Fail("Doküman kaydı bulunamadı.");
            }

            document.FileName = storedFile.FileName;
            document.RelativePath = storedFile.RelativePath;
            document.ContentType = string.IsNullOrWhiteSpace(storedFile.ContentType) ? document.ContentType : storedFile.ContentType;
            document.UpdateDate = DateTime.Now;
            _libraryDocumentRepository.Update(document);
            return OperationResultModel.Success("Doküman yüklendi.");
        }

        public async Task<OperationResultModel> RenameDocumentAsync(int id, string? name)
        {
            var document = await _libraryDocumentRepository.GetByIdAsync(id);
            if (document == null)
            {
                return OperationResultModel.Fail("Güncellenecek doküman bulunamadı.");
            }

            var normalizedName = NormalizeDocumentName(name, document.OriginalFileName);
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return OperationResultModel.Fail("Doküman adı boş bırakılamaz.");
            }

            document.OriginalFileName = normalizedName;
            document.UpdateDate = DateTime.Now;
            _libraryDocumentRepository.Update(document);
            return OperationResultModel.Success("Doküman adı güncellendi.");
        }

        public async Task<OperationResultModel> DeleteDocumentMetadataAsync(int id)
        {
            var document = await _libraryDocumentRepository.GetByIdAsync(id);
            if (document == null)
            {
                return OperationResultModel.Fail("Silinecek doküman bulunamadı.");
            }

            _libraryDocumentRepository.Delete(document);
            return OperationResultModel.Success("Doküman silindi.");
        }

        private static List<LibraryContentItemModel> BuildContentItems(
            int selectedFolderId,
            IReadOnlyCollection<LibraryFolder> folders,
            IReadOnlyCollection<LibraryDocument> documents,
            int? selectedDocumentId)
        {
            var folderItems = folders
                .Where(x => x.ParentFolderId == selectedFolderId)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .Select(x => new LibraryContentItemModel
                {
                    FolderId = x.Id,
                    Name = x.Name,
                    IsFolder = true,
                    MetaText = "Klasör"
                });

            var documentItems = documents
                .Where(x => x.FolderId == selectedFolderId)
                .OrderByDescending(x => x.CreatedDate)
                .ThenBy(x => x.OriginalFileName)
                .Select(x => new LibraryContentItemModel
                {
                    DocumentId = x.Id,
                    Name = x.OriginalFileName,
                    IsFolder = false,
                    IsPdf = IsPdf(x),
                    IsSelected = selectedDocumentId == x.Id,
                    MetaText = BuildDocumentMetaText(x)
                });

            return folderItems.Concat(documentItems).ToList();
        }

        private static List<LibraryTreeNodeModel> BuildFolderTree(
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
                        .Select(x => new LibraryTreeNodeModel
                        {
                            FolderId = x.FolderId,
                            DocumentId = x.Id,
                            Name = x.OriginalFileName,
                            IsFolder = false,
                            IsPdf = IsPdf(x),
                            IsSelected = selectedDocumentId == x.Id
                        });

                    return new LibraryTreeNodeModel
                    {
                        FolderId = folder.Id,
                        Name = folder.Name,
                        IsFolder = true,
                        IsSelected = selectedFolderId == folder.Id,
                        Children = childFolders.Concat(childDocuments).ToList()
                    };
                })
                .ToList();
        }

        private static LibraryDocumentModel MapDocument(LibraryDocument document)
        {
            return new LibraryDocumentModel
            {
                Id = document.Id,
                FolderId = document.FolderId,
                FileName = document.FileName,
                OriginalFileName = document.OriginalFileName,
                RelativePath = document.RelativePath,
                ContentType = document.ContentType,
                SizeBytes = document.SizeBytes,
                CreatedDate = document.CreatedDate,
                IsPdf = IsPdf(document)
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

        private static List<LibraryBreadcrumbItemModel> BuildBreadcrumbs(int folderId, IReadOnlyCollection<LibraryFolder> folders)
        {
            var lookup = folders.ToDictionary(x => x.Id);
            var path = new List<LibraryBreadcrumbItemModel>();
            var currentId = folderId;

            while (lookup.TryGetValue(currentId, out var folder))
            {
                path.Add(new LibraryBreadcrumbItemModel
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
