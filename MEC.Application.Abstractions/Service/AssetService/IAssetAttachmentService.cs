using MEC.Application.Abstractions.Application;
using MEC.Domain.Entity.Asset;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEC.Application.Abstractions.Service.AssetService
{
    public interface IAssetAttachmentService : IApplicationService
    {
        Task<List<AssetAttachment>> GetAllAttachmentsAsync();
        Task<List<AssetAttachment>> GetAttachmentsByAssetIdAsync(int assetId);
        Task<AssetAttachment> GetAttachmentByAssetAndNameAsync(int assetId, string fileName);
        Task CreateAsync(AssetAttachment attachment);
        Task UpdateAttachmentAsync(AssetAttachment attachment);
        Task DeleteAsync(int id);
    }
}
