using MEC.Application.Abstractions.Common.Models;

namespace MEC.Application.Abstractions.Service.SchoolService.Model
{
    public class LibraryTreeNodeModel
    {
        public int? FolderId { get; set; }
        public int? DocumentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsFolder { get; set; }
        public bool IsPdf { get; set; }
        public bool IsSelected { get; set; }
        public List<LibraryTreeNodeModel> Children { get; set; } = new();
    }

    public class LibraryContentItemModel
    {
        public int? FolderId { get; set; }
        public int? DocumentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsFolder { get; set; }
        public bool IsPdf { get; set; }
        public string MetaText { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    public class LibraryBreadcrumbItemModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class LibraryDocumentModel
    {
        public int Id { get; set; }
        public int FolderId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public DateTime? CreatedDate { get; set; }
        public bool IsPdf { get; set; }
    }

    public class LibraryDocumentSearchResultModel
    {
        public int DocumentId { get; set; }
        public int FolderId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FolderPath { get; set; } = string.Empty;
        public string MetaText { get; set; } = string.Empty;
        public bool IsPdf { get; set; }
    }

    public class LibraryExplorerModel
    {
        public List<LibraryTreeNodeModel> FolderTree { get; set; } = new();
        public List<LibraryBreadcrumbItemModel> Breadcrumbs { get; set; } = new();
        public List<LibraryContentItemModel> Items { get; set; } = new();
        public int? SelectedFolderId { get; set; }
        public string SelectedFolderName { get; set; } = string.Empty;
        public int? SelectedDocumentId { get; set; }
        public LibraryDocumentModel? SelectedDocument { get; set; }
        public string SearchTerm { get; set; } = string.Empty;
        public List<LibraryDocumentSearchResultModel> SearchResults { get; set; } = new();
    }

    public class LibraryDocumentCreateModel : StoredFileModel
    {
        public int FolderId { get; set; }
    }

    public class LibraryFolderDeletePrepareModel
    {
        public int? ParentFolderId { get; set; }
        public List<LibraryDocumentModel> Documents { get; set; } = new();
    }
}
