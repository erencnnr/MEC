using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEC.Application.Abstractions.Service.AssetService.Model
{
    public class AssetFilterRequestModel
    {
        public string? SearchText { get; set; }
        public int? SchoolId { get; set; }
        public int? AssetTypeId { get; set; }
        public int? AssetStatusId { get; set; }
    }
}
