using MEC.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace MEC.Domain.Entity.School
{
    [Table("library_folder")]
    public class LibraryFolder : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        [Column("parent_folder_id")]
        public int? ParentFolderId { get; set; }

        [Column("display_order")]
        public int DisplayOrder { get; set; }

        public LibraryFolder? ParentFolder { get; set; }
        public ICollection<LibraryFolder> ChildFolders { get; set; } = new List<LibraryFolder>();
        public ICollection<LibraryDocument> Documents { get; set; } = new List<LibraryDocument>();
    }
}
