using System;

namespace MEC.AssetManagementUI.Models.AssetModel
{
    public class AssetViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string SerialNumber { get; set; }
        public string SchoolName { get; set; }
        public string AssetStatusName { get; set; }
    }
}