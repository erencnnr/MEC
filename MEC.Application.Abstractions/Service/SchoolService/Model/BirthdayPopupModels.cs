using MEC.Application.Abstractions.Common.Models;

namespace MEC.Application.Abstractions.Service.SchoolService.Model
{
    public class BirthdayPopupImageModel
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class BirthdayPopupImageCreateModel : StoredFileModel
    {
    }
}
