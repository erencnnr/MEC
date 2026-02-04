using MEC.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEC.Domain.Entity.Asset
{
    [Table("service_history")]
    public class ServiceHistory : BaseEntity
    {
        public int Id { get; set; }

        // Asset ile ilişki
        public int AssetId { get; set; }
        public Asset Asset { get; set; }

        public DateTime SendDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string Description { get; set; }
        public string? ServiceCompany { get; set; }
        public decimal? Cost { get; set; }
        public bool IsWarranty { get; set; }
    }
}
