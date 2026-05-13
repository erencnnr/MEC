using MEC.Domain.Entity.Asset;
using MEC.Domain.Entity.Invoice;
using MEC.Domain.Entity.Loan;
using System.ComponentModel.DataAnnotations;

namespace MEC.Portal.Models
{
    public class AssetListViewModel
    {
        public int Id { get; set; }
        public string SerialNumber { get; set; } = "-";
        public string Description { get; set; } = "-";
        public string Name { get; set; } = "-";
        public decimal? Cost { get; set; }
        public string CreatedDate { get; set; } = "-";
        public string SchoolName { get; set; } = "Tanımsız";
        public string SchoolClassName { get; set; } = "-";
        public string AssetTypeName { get; set; } = "Tanımsız";
        public string StatusName { get; set; } = "Belirsiz";
        public string StatusColor { get; set; } = "secondary";
        public string WarrantyEndDate { get; set; } = "-";
        public string InvoiceDate { get; set; } = "-";
    }

    public class AssetCreateViewModel
    {
        [Display(Name = "Ad")]
        [Required(ErrorMessage = "Ad zorunludur.")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Seri Numarası")]
        public string? SerialNumber { get; set; }

        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Lütfen bir okul seçiniz.")]
        public int SchoolId { get; set; }

        [Required(ErrorMessage = "Lütfen bir tür seçiniz.")]
        public int AssetTypeId { get; set; }

        [Display(Name = "Garanti Bitiş Tarihi")]
        [DataType(DataType.Date)]
        public DateTime? WarrantyEndDate { get; set; }

        [Display(Name = "Fatura Tarihi")]
        [DataType(DataType.Date)]
        public DateTime? InvoiceDate { get; set; }

        [Display(Name = "Sınıf / Şube")]
        public int? SchoolClassId { get; set; }
    }

    public class AssetInfoViewModel : AssetCreateViewModel
    {
        public int Id { get; set; }
        public Invoice? Invoice { get; set; }
        public List<AssetImage>? Images { get; set; }
        public List<Loan>? Loans { get; set; }
        public List<ServiceHistory>? ServiceHistories { get; set; }
    }

    public class AssignAssetViewModel
    {
        public int AssetId { get; set; }

        [Required(ErrorMessage = "Lütfen bir personel seçiniz.")]
        public int AssignedToId { get; set; }

        [Required(ErrorMessage = "Zimmet tarihi zorunludur.")]
        [DataType(DataType.Date)]
        public DateTime LoanDate { get; set; } = DateTime.Now;

        public string? Notes { get; set; }
    }

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
