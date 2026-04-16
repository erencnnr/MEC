namespace MEC.Application.Abstractions.Service.LeaveService.Model
{
    public class AdminDashboardModel
    {
        public string AdminName { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public int PendingLeaveCount { get; set; }
        public int TotalAnnouncementCount { get; set; }
        public int TodayAnnouncementCount { get; set; }
        public int NegativeLeaveBalanceCount { get; set; }
        public List<AdminRecentLeaveItemModel> RecentLeaveRequests { get; set; } = new();
        public List<AdminRecentAnnouncementItemModel> RecentAnnouncements { get; set; } = new();
    }

    public class AdminRecentLeaveItemModel
    {
        public string EmployeeName { get; set; } = string.Empty;
        public string LeaveType { get; set; } = string.Empty;
        public decimal RequestedDays { get; set; }
        public int Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class AdminRecentAnnouncementItemModel
    {
        public string Title { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
