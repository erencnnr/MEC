namespace MEC.Portal.Models
{
    public class LeaveRequestViewModel
    {
        public string StartDate { get; set; } // İzin Başlangıç Tarihi
        public string EndDate { get; set; }   // İzin Bitiş Tarihi
        public string Reason { get; set; }    // İzin Nedeni / Açıklama
    }
}
