using MEC.Application.Abstractions.Service.AssetService;
using MEC.Application.Abstractions.Service.AssetService.Model;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Application.Service.SchoolService;
using MEC.AssetManagementUI.Extensions;
using MEC.AssetManagementUI.Models.AssetModel;
using MEC.Domain.Entity.Asset;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MEC.AssetManagementUI.Controllers
{
    public class AssetController : Controller
    {
        private readonly IAssetService _assetService;
        private readonly ISchoolService _schoolService;
        private readonly IAssetTypeService _assetTypeService;
        private readonly IAssetStatusService _assetStatusService;
        public AssetController(IAssetService assetService, ISchoolService schoolService, IAssetTypeService assetTypeService, IAssetStatusService assetStatusService)
        {
            _assetService = assetService;
            _schoolService = schoolService;
            _assetTypeService = assetTypeService;
            _assetStatusService = assetStatusService;
        }
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] AssetFilterRequestModel request)
        {
            // 1. Servisten verileri çek
            var assets = await _assetService.GetAssetListAsync(request);

            // 2. Entity -> ViewModel Dönüşümü (Mapping)
            var model = assets.Select(x => x.ToViewModel()).ToList();

            ViewBag.Schools = new SelectList(await _schoolService.GetSchoolListAsync(), "Id", "Name", request.SchoolId);
            ViewBag.Types = new SelectList(await _assetTypeService.GetAssetTypeListAsync(), "Id", "Name", request.AssetTypeId);
            ViewBag.Statuses = new SelectList(await _assetStatusService.GetAssetStatusListAsync(), "Id", "Name", request.AssetStatusId);
            ViewBag.CurrentFilters = request;

            return View("AssetList",model);
        }

    }
}
