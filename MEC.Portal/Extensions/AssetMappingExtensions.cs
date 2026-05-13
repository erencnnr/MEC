using MEC.Domain.Entity.Asset;
using MEC.Portal.Models;

namespace MEC.Portal.Extensions
{
    public static class AssetMappingExtensions
    {
        public static AssetListViewModel ToViewModel(this Asset asset)
        {
            return new AssetListViewModel
            {
                Id = asset.Id,
                SerialNumber = asset.SerialNumber ?? "-",
                Description = asset.Description ?? "-",
                Name = asset.Name ?? "-",
                Cost = asset.Cost,
                CreatedDate = asset.CreatedDate.HasValue ? asset.CreatedDate.Value.ToString("dd.MM.yyyy") : "-",
                WarrantyEndDate = asset.WarrantyEndDate.HasValue ? asset.WarrantyEndDate.Value.ToString("dd.MM.yyyy") : "-",
                InvoiceDate = asset.InvoiceDate.HasValue ? asset.InvoiceDate.Value.ToString("dd.MM.yyyy") : "-",
                SchoolName = asset.School?.Name ?? "Tanımsız",
                SchoolClassName = asset.SchoolClass?.Name ?? "-",
                AssetTypeName = asset.AssetType?.Name ?? "Tanımsız",
                StatusName = asset.AssetStatus?.Name ?? "Belirsiz",
                StatusColor = !string.IsNullOrWhiteSpace(asset.AssetStatus?.ColorCode)
                    ? asset.AssetStatus.ColorCode
                    : "secondary"
            };
        }
    }
}
