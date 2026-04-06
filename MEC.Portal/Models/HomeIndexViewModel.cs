using MEC.Domain.Entity.School;
using System.Collections.Generic;

namespace MEC.Portal.Models
{
    public class HomeIndexViewModel
    {
        public List<Announcement> Announcements { get; set; } = new();
        public List<HomeSliderItemViewModel> SliderItems { get; set; } = new();
    }

    public class HomeSliderItemViewModel
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }
}
