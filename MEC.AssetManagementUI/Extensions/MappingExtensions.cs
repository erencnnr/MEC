using MEC.AssetManagementUI.Models.AssetModel;
using MEC.Domain.Entity.Asset;

namespace MEC.AssetManagementUI.Extensions
{
    public static class MappingExtensions
    {
        public static AssetListViewModel ToViewModel(this Asset x)
        {
            return new AssetListViewModel
            {
                Id = x.Id,
                SerialNumber = x.SerialNumber ?? "-",
                Description = x.Description ?? "-",
                Name = x.Name ?? "-",
                Cost = x.Cost,
                PurchaseDate = x.PurchaseDate.HasValue ? x.PurchaseDate.Value.ToShortDateString() : "-",
                WarrantyEndDate = x.WarrantyEndDate.HasValue ? x.WarrantyEndDate.Value.ToShortDateString() : "-",
                SchoolName = x.School != null ? x.School.Name : "Tanımsız",
                AssetTypeName = x.AssetType != null ? x.AssetType.Name : "Tanımsız",
                StatusName = x.AssetStatus != null ? x.AssetStatus.Name : "Belirsiz",
                StatusColor = x.AssetStatus != null && !string.IsNullOrEmpty(x.AssetStatus.ColorCode)
                              ? x.AssetStatus.ColorCode
                              : "secondary"
            };
        }
    }
}
