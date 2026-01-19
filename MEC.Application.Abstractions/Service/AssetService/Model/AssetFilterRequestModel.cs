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
        public List<int>? SchoolIds { get; set; }      
        public List<int>? AssetTypeIds { get; set; }   
        public List<int>? AssetStatusIds { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortOrder { get; set; }
    }
}
