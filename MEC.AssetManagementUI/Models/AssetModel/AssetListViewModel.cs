

using MEC.Domain.Entity.Asset;

namespace MEC.AssetManagementUI.Models.AssetModel
{
    public class AssetListViewModel
    {
        public int Id { get; set; }
        public string SerialNumber { get; set; }
        public string Description { get; set; }
        public decimal? Cost { get; set; }
        public string PurchaseDate { get; set; }

        // İlişkili tablolardan gelecek veriler
        public string SchoolName { get; set; }
        public string AssetTypeName { get; set; }

        // Badge (Etiket) rengi için
        public string StatusName { get; set; }
        public string StatusColor { get; set; } // "success", "danger" vb.
    }
}
