namespace MEC.Domain.Common.Enum
{
    public enum LeaveStatus
    {
        Pending = 0,   // Bekliyor
        Approved = 1,  // Onaylandı
        Rejected = 2,  // Reddedildi
        Cancelled = 3, // İptal
        PendingFinalApproval = 4 // Okul müdürü onayladı, Genel Müdürlük onayı bekleniyor
    }
}
