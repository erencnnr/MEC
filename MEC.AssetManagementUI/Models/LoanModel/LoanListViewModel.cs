namespace MEC.AssetManagementUI.Models.LoanModel
{
    public class LoanListViewModel
    {
        public int Id { get; set; }
        public int AssetId { get; set; }
        public string AssetName { get; set; }
        public string SerialNumber { get; set; }
        public string AssignedToName { get; set; } 
        public string AssignedByName { get; set; }
        public string LoanDate { get; set; }
        public string? ReturnDate { get; set; }
        public bool IsActive => string.IsNullOrEmpty(ReturnDate);
    }
}
