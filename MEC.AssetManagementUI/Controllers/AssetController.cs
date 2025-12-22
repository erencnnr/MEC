using MEC.Application.Abstractions.Service.AssetService;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Application.Service.SchoolService;
using MEC.AssetManagementUI.Extensions;
using MEC.AssetManagementUI.Models.AssetModel;
using MEC.Domain.Entity.Asset;
using Microsoft.AspNetCore.Mvc;

namespace MEC.AssetManagementUI.Controllers
{
    public class AssetController : Controller
    {
        private readonly IAssetService _assetService;
        public AssetController(IAssetService assetService)
        {
            _assetService = assetService;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Servisten verileri çek
            var assets = await _assetService.GetAssetListAsync();

            // 2. Entity -> ViewModel Dönüşümü (Mapping)
            var model = assets.Select(x => x.ToViewModel()).ToList();

            return View("AssetList",model);
        }

        [HttpGet]
        public async Task<IActionResult> GetAssets()
        {
            var assets = await _assetService.GetAssetListAsync();
            return Ok(assets); 
        }
    }
}
