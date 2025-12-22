using MEC.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEC.Domain.Entity.Asset
{
    public class AssetStatus : BaseEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? ColorCode { get; set; }
    }
}
