using Microsoft.AspNetCore.Mvc;
using MEC.Application.Abstractions.Service.AssetService;
using MEC.Domain.Entity.Asset;
using System.Threading.Tasks;

namespace MEC.AssetManagementUI.Controllers
{
    public class AssetStatusController : Controller
    {
        private readonly IAssetStatusService _service;

        public AssetStatusController(IAssetStatusService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var list = await _service.GetAssetStatusListAsync();
            return View("AssetStatusList",list);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdate(AssetStatus assetStatus)
        {
            if (assetStatus.Id == 0)
            {
                await _service.CreateAssetStatusAsync(assetStatus);
            }
            else
            {
                await _service.UpdateAssetStatusAsync(assetStatus);
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int id)
        {
            var errorMessage = await _service.DeleteAssetStatusAsync(id);
            if (!string.IsNullOrEmpty(errorMessage))
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            return RedirectToAction("Index");
        }
    }
}
