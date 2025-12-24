using MEC.Application.Abstractions.Application;
using MEC.Domain.Entity.Asset;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace MEC.Application.Abstractions.Service.AssetService
{
    public interface IAssetImageService : IApplicationService
    {
        Task<List<AssetImage>> GetAssetImageListAsync();
        Task<List<AssetImage>> GetImagesByAssetIdAsync(int assetId);
    }
}
