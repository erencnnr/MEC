using MEC.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace MEC.Domain.Entity.School
{
    [Table("food_menu_day")]
    public class FoodMenuDay : BaseEntity
    {
        [Column("food_menu_month_id")]
        public int FoodMenuMonthId { get; set; }

        [Column("menu_date")]
        public DateTime MenuDate { get; set; }

        [Column("items_text")]
        public string ItemsText { get; set; } = string.Empty;

        [Column("source_page_number")]
        public int? SourcePageNumber { get; set; }

        [Column("display_order")]
        public int DisplayOrder { get; set; }

        [ForeignKey(nameof(FoodMenuMonthId))]
        public FoodMenuMonth? FoodMenuMonth { get; set; }
    }
}
