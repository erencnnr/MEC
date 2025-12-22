using MEC.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEC.Domain.Entity.Asset
{
    [Table("asset_image")]
    public class AssetImage : BaseEntity
    {
        public int Id { get; set; }
        public string? Url { get; set; }
        public string? Path { get; set; }

        public int AssetId { get; set; }
        public Asset Asset { get; set; }
    }
}
