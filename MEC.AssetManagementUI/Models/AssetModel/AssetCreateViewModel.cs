using System;
using System.ComponentModel.DataAnnotations;

namespace MEC.AssetManagementUI.Models.AssetModel
{
    public class AssetCreateViewModel
    {
        [Display(Name = "Demirbaş Adı")]
        [Required(ErrorMessage = "Demirbaş adı zorunludur.")]
        public string Name { get; set; }

        [Display(Name = "Seri Numarası")]
        public string SerialNumber { get; set; }

        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        // Dropdown Seçimleri İçin ID'ler
        [Required(ErrorMessage = "Lütfen bir okul seçiniz.")]
        public int SchoolId { get; set; }

        [Required(ErrorMessage = "Lütfen bir tür seçiniz.")]
        public int AssetTypeId { get; set; }

    }
}