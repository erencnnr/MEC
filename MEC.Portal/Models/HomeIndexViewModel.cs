using MEC.Domain.Entity.School;
using System;
using System.Collections.Generic;

namespace MEC.Portal.Models
{
    public class HomeIndexViewModel
    {
        public List<Announcement> Announcements { get; set; } = new();
        public List<Announcement> News { get; set; } = new();
        public List<HomeEmployeeDirectoryItemViewModel> Employees { get; set; } = new();
        public List<HomeSliderItemViewModel> SliderItems { get; set; } = new();
    }

    public class HomeSliderItemViewModel
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }

    public class HomeEmployeeDirectoryItemViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime? HireDate { get; set; }
    }
}
