namespace MEC.PDKS.Models
{
    public class PdksUserListViewModel
    {
        public List<PdksUserListItemViewModel> Items { get; set; } = new();
    }

    public class PdksUserListItemViewModel
    {
        public int Id { get; set; }
        public string SicilNo { get; set; } = string.Empty;
        public string AdSoyad { get; set; } = string.Empty;
        public bool AktifMi { get; set; }
    }
}
