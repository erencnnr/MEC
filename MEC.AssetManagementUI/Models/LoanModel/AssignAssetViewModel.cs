using System;
using System.ComponentModel.DataAnnotations;

namespace MEC.AssetManagementUI.Models.LoanModel
{
    public class AssignAssetViewModel
    {
        public int AssetId { get; set; }

        [Required(ErrorMessage = "Lütfen bir personel seçiniz.")]
        public int AssignedToId { get; set; } // Kime zimmetlendi

        [Required(ErrorMessage = "Zimmet tarihi zorunludur.")]
        [DataType(DataType.Date)]
        public DateTime LoanDate { get; set; } = DateTime.Now;

        public string? Notes { get; set; }
    }
}