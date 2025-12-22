using MEC.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEC.Domain.Entity.Asset
{
    [Table("asset_status")]
    public class AssetStatus : BaseEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? ColorCode { get; set; }
    }
}
