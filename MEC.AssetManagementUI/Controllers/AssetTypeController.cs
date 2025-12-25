using Microsoft.AspNetCore.Mvc;
using MEC.Application.Abstractions.Service.AssetService;
using MEC.Domain.Entity.Asset;
using System.Threading.Tasks;

namespace MEC.AssetManagementUI.Controllers
{
    public class AssetTypeController : Controller
    {
        private readonly IAssetTypeService _service;

        public AssetTypeController(IAssetTypeService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var list = await _service.GetAssetTypeListAsync();
            return View("AssetTypeList",list);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdate(AssetType assetType)
        {
            if (assetType.Id == 0)
            {
                await _service.CreateAssetTypeAsync(assetType);
            }
            else
            {
                await _service.UpdateAssetTypeAsync(assetType);
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int id)
        {
            var errorMessage = await _service.DeleteAssetTypeAsync(id);
            if (!string.IsNullOrEmpty(errorMessage))
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            return RedirectToAction("Index");
        }
    }
}
