using System;
using System.Collections.Generic;

namespace MEC.Portal.Models
{
    public class AdminSliderViewModel
    {
        public List<AdminSliderItemViewModel> Items { get; set; } = new();
    }

    public class AdminSliderItemViewModel
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class SliderReorderRequest
    {
        public List<int> OrderedIds { get; set; } = new();
    }
}
