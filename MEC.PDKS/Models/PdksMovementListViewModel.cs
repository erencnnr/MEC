using Microsoft.AspNetCore.Mvc.Rendering;

namespace MEC.PDKS.Models
{
    public class PdksMovementListViewModel
    {
        public PdksMovementFilterViewModel Filter { get; set; } = new();
        public List<SelectListItem> UserOptions { get; set; } = new();
        public List<PdksMovementListItemViewModel> Items { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public class PdksMovementFilterViewModel
    {
        public int? KullaniciId { get; set; }
        public DateTime? Gun { get; set; }
        public DateTime? BaslangicTarihi { get; set; }
        public DateTime? BitisTarihi { get; set; }
    }

    public class PdksMovementListItemViewModel
    {
        public int Id { get; set; }
        public int KullaniciId { get; set; }
        public string KullaniciAdi { get; set; } = string.Empty;
        public string SicilNo { get; set; } = string.Empty;
        public DateTime HareketZamani { get; set; }
        public string HareketTipi { get; set; } = string.Empty;
        public string? CihazAdi { get; set; }
        public string? Not { get; set; }
    }
}
