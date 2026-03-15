using System;
using System.Collections.Generic;

namespace MEC.Portal.Models
{
    public class AdminDashboardViewModel
    {
        public string AdminName { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public int PendingLeaveCount { get; set; }
        public int TotalAnnouncementCount { get; set; }
        public int TodayAnnouncementCount { get; set; }
        public int NegativeLeaveBalanceCount { get; set; }
        public List<AdminRecentLeaveItemViewModel> RecentLeaveRequests { get; set; } = new();
        public List<AdminRecentAnnouncementItemViewModel> RecentAnnouncements { get; set; } = new();
    }

    public class AdminRecentLeaveItemViewModel
    {
        public string EmployeeName { get; set; } = string.Empty;
        public string LeaveType { get; set; } = string.Empty;
        public int RequestedDays { get; set; }
        public int Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class AdminRecentAnnouncementItemViewModel
    {
        public string Title { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
