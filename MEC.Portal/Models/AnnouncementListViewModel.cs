using System;
using System.Collections.Generic;

namespace MEC.Portal.Models
{
    public class AnnouncementListViewModel
    {
        public List<AnnouncementCardViewModel> Items { get; set; } = new();
        public string Status { get; set; } = "active";
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 6;
    }

    public class AnnouncementCardViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public DateTime? CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }
}
