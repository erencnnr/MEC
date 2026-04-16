using MEC.Application.Abstractions.Common.Models;
using MEC.Application.Abstractions.Service.SchoolService.Model;

namespace MEC.Application.Abstractions.Service.SchoolService
{
    public interface ILibraryService
    {
        Task<LibraryExplorerModel> GetExplorerAsync(int? folderId, int? documentId);
        Task<LibraryDocumentModel?> GetDocumentAsync(int id);
        Task<OperationResultModel<int>> CreateFolderAsync(string? name, int? parentFolderId);
        Task<OperationResultModel> RenameFolderAsync(int id, string? name);
        Task<OperationResultModel<LibraryFolderDeletePrepareModel>> PrepareDeleteFolderAsync(int id);
        Task<OperationResultModel> DeleteFolderMetadataAsync(int id);
        Task<OperationResultModel<LibraryDocumentModel>> CreateDocumentPlaceholderAsync(LibraryDocumentCreateModel model);
        Task<OperationResultModel> CompleteDocumentUploadAsync(int id, StoredFileModel storedFile);
        Task<OperationResultModel> RenameDocumentAsync(int id, string? name);
        Task<OperationResultModel> DeleteDocumentMetadataAsync(int id);
    }
}
