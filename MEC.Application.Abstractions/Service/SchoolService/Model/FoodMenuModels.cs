using MEC.Application.Abstractions.Common.Models;
using MEC.Domain.Common.Enum;

namespace MEC.Application.Abstractions.Service.SchoolService.Model
{
    public class FoodMenuMonthModel
    {
        public int Id { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public FoodMenuMonthStatus Status { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public int PageCount { get; set; }
        public string ParseWarnings { get; set; } = string.Empty;
        public DateTime? ImportedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime? CreatedDate { get; set; }
        public List<FoodMenuDayModel> Days { get; set; } = new();
    }

    public class FoodMenuDayModel
    {
        public int Id { get; set; }
        public DateTime MenuDate { get; set; }
        public string ItemsText { get; set; } = string.Empty;
        public int? SourcePageNumber { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class FoodMenuImportModel : StoredFileModel
    {
        public int MonthId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public byte[] PdfContent { get; set; } = Array.Empty<byte>();
    }

    public class FoodMenuMonthDaySaveModel
    {
        public int MonthId { get; set; }
        public List<FoodMenuMonthDaySaveItemModel> Days { get; set; } = new();
    }

    public class FoodMenuMonthDaySaveItemModel
    {
        public DateTime MenuDate { get; set; }
        public string ItemsText { get; set; } = string.Empty;
        public int? SourcePageNumber { get; set; }
    }
}
