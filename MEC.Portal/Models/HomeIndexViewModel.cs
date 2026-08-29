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
        public bool CanViewEmployeeDirectory { get; set; }
        public bool IsEmployeeDirectoryLocationRestricted { get; set; }
        public List<HomeSliderItemViewModel> SliderItems { get; set; } = new();
        public bool ShowBirthdayPopup { get; set; }
        public string BirthdayPopupImageUrl { get; set; } = string.Empty;
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
