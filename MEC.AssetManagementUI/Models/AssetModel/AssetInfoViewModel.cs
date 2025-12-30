using MEC.Domain.Entity.Asset;
using MEC.Domain.Entity.Loan;
using MEC.Domain.Entity.Invoice;

namespace MEC.AssetManagementUI.Models.AssetModel
{
    // Mevcut düzenleme alanlarını miras alıyoruz
    public class AssetInfoViewModel : AssetCreateViewModel
    {
        public int Id { get; set; }
        public Invoice? Invoice { get; set; }
        public List<AssetImage>? Images { get; set; }
        public List<Loan>? Loans { get; set; }
        public DateTime? InvoiceDate { get; set; }
    }
}