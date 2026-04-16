namespace MEC.Application.Abstractions.Common.Models
{
    public class PagedResultModel<T>
    {
        public List<T> Items { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
    }
}
