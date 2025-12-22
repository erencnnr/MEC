using MEC.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEC.Domain.Entity.Asset
{
    [Table("asset")]
    public class Asset : BaseEntity
    {
        public int Id { get; set; }
        public string? SerialNumber { get; set; }
        public string? Description { get; set; }
        public decimal? Cost { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public int SchoolId { get; set; }
        public School.School School { get; set; }
        public int AssetStatusId { get; set; }
        public AssetStatus AssetStatus { get; set; }
        public int AssetTypeId { get; set; }
        public AssetType AssetType { get; set; }
        public int? InvoiceId { get; set; }
        public Invoice.Invoice Invoice { get; set; }
    }
}
