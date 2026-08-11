namespace MEC.Portal.Models
{
    public class LibraryTreeNodeViewModel
    {
        public int? FolderId { get; set; }
        public int? DocumentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsFolder { get; set; }
        public bool IsPdf { get; set; }
        public bool IsSelected { get; set; }
        public string NavigateUrl { get; set; } = string.Empty;
        public bool OpenInNewTab { get; set; }
        public List<LibraryTreeNodeViewModel> Children { get; set; } = new();
    }

    public class LibraryContentItemViewModel
    {
        public int? FolderId { get; set; }
        public int? DocumentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsFolder { get; set; }
        public bool IsPdf { get; set; }
        public string NavigateUrl { get; set; } = string.Empty;
        public string OpenUrl { get; set; } = string.Empty;
        public string PreviewUrl { get; set; } = string.Empty;
        public string SelectUrl { get; set; } = string.Empty;
        public string MetaText { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    public class LibraryBreadcrumbItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class LibraryDocumentItemViewModel
    {
        public int Id { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public DateTime? CreatedDate { get; set; }
        public bool IsPdf { get; set; }
        public string OpenUrl { get; set; } = string.Empty;
        public string PreviewUrl { get; set; } = string.Empty;
    }

    public class LibraryDocumentSearchResultViewModel
    {
        public int DocumentId { get; set; }
        public int FolderId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FolderPath { get; set; } = string.Empty;
        public string MetaText { get; set; } = string.Empty;
        public bool IsPdf { get; set; }
        public string OpenUrl { get; set; } = string.Empty;
    }

    public class AdminContentManagementViewModel
    {
        public List<LibraryTreeNodeViewModel> FolderTree { get; set; } = new();
        public List<LibraryBreadcrumbItemViewModel> Breadcrumbs { get; set; } = new();
        public List<LibraryContentItemViewModel> Items { get; set; } = new();
        public List<LibraryDocumentItemViewModel> Documents { get; set; } = new();
        public int? SelectedFolderId { get; set; }
        public string SelectedFolderName { get; set; } = string.Empty;
        public int? SelectedDocumentId { get; set; }
        public LibraryDocumentItemViewModel? SelectedDocument { get; set; }
        public bool HasFolders => FolderTree.Count > 0;
        public bool HasSelectedFolder => SelectedFolderId.HasValue;
    }

    public class LibraryIndexViewModel
    {
        public List<LibraryTreeNodeViewModel> FolderTree { get; set; } = new();
        public List<LibraryBreadcrumbItemViewModel> Breadcrumbs { get; set; } = new();
        public List<LibraryContentItemViewModel> Items { get; set; } = new();
        public int? SelectedFolderId { get; set; }
        public string SelectedFolderName { get; set; } = string.Empty;
        public string SearchTerm { get; set; } = string.Empty;
        public List<LibraryDocumentSearchResultViewModel> SearchResults { get; set; } = new();
        public bool IsSearching => !string.IsNullOrWhiteSpace(SearchTerm);
        public bool HasFolders => FolderTree.Count > 0;
        public bool HasSelectedFolder => SelectedFolderId.HasValue;
    }
}
