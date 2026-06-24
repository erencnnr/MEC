using System.ComponentModel.DataAnnotations.Schema;

namespace MEC.PDKS.Models.Entities
{
    [Table("pdks_movement")]
    public class PdksMovement
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("pdks_user_id")]
        public int PdksUserId { get; set; }

        [Column("hareket_zamani")]
        public DateTime HareketZamani { get; set; }

        [Column("hareket_tipi")]
        public PdksMovementType HareketTipi { get; set; }

        [Column("cihaz_adi")]
        public string? CihazAdi { get; set; }

        [Column("not")]
        public string? Not { get; set; }

        [Column("created_date")]
        public DateTime? CreatedDate { get; set; }

        [Column("update_date")]
        public DateTime? UpdateDate { get; set; }

        [ForeignKey(nameof(PdksUserId))]
        public PdksUser? PdksUser { get; set; }
    }
}
