namespace MEC.Portal.Models
{
    public class LibraryFolderTreeNodeViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
        public List<LibraryFolderTreeNodeViewModel> Children { get; set; } = new();
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

    public class AdminContentManagementViewModel
    {
        public List<LibraryFolderTreeNodeViewModel> FolderTree { get; set; } = new();
        public List<LibraryBreadcrumbItemViewModel> Breadcrumbs { get; set; } = new();
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
        public List<LibraryFolderTreeNodeViewModel> FolderTree { get; set; } = new();
        public List<LibraryBreadcrumbItemViewModel> Breadcrumbs { get; set; } = new();
        public List<LibraryDocumentItemViewModel> Documents { get; set; } = new();
        public int? SelectedFolderId { get; set; }
        public string SelectedFolderName { get; set; } = string.Empty;
        public int? SelectedDocumentId { get; set; }
        public LibraryDocumentItemViewModel? SelectedDocument { get; set; }
        public bool HasFolders => FolderTree.Count > 0;
        public bool HasSelectedFolder => SelectedFolderId.HasValue;
    }
}
