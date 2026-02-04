using System;
using System.ComponentModel.DataAnnotations;

namespace MEC.AssetManagementUI.Models.ServiceHistoryModel
{
    public class ServiceHistoryCreateViewModel
    {
        public int AssetId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime SendDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ReturnDate { get; set; }

        [Required]
        [MaxLength(255)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? ServiceCompany { get; set; }

        public decimal? Cost { get; set; }

        public bool IsWarranty { get; set; }
    }
}
