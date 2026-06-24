using System.ComponentModel.DataAnnotations.Schema;

namespace MEC.PDKS.Models.Entities
{
    [Table("pdks_user")]
    public class PdksUser
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("sicil_no")]
        public string SicilNo { get; set; } = string.Empty;

        [Column("ad_soyad")]
        public string AdSoyad { get; set; } = string.Empty;

        [Column("aktif_mi")]
        public bool AktifMi { get; set; }

        [Column("created_date")]
        public DateTime? CreatedDate { get; set; }

        [Column("update_date")]
        public DateTime? UpdateDate { get; set; }

        public ICollection<PdksMovement> Movements { get; set; } = new List<PdksMovement>();
    }
}
