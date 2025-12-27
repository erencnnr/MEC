using MEC.Application.Abstractions.Service.AssetService;
using MEC.Application.Abstractions.Service.AssetService.Model;
using MEC.Application.Abstractions.Service.EmployeeService;
using MEC.Application.Abstractions.Service.LoanService;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.AssetManagementUI.Models.AssetModel;
using MEC.AssetManagementUI.Models.LoanModel;
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
        private readonly ILoanService _loanService;
        private readonly ILoanStatusService _loanStatusService;
        private readonly IAssetImageService _imageService;
        private readonly IEmployeeService _employeeService;

        public AssetController(IAssetService assetService, ISchoolService schoolService, IAssetTypeService assetTypeService, IAssetStatusService assetStatusService,
            ILoanService loanService, IAssetImageService imageService, IEmployeeService employeeService, ILoanStatusService loanStatusService)
        {
            _assetService = assetService;
            _schoolService = schoolService;
            _assetTypeService = assetTypeService;
            _assetStatusService = assetStatusService;
            _loanService = loanService;
            _imageService = imageService;
            _employeeService = employeeService;
            _loanStatusService = loanStatusService;
        }

        // -------------------------------------------------------------------------
        // LISTELEME (INDEX)
        // -------------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] AssetFilterRequestModel request)
        {
            var assets = await _assetService.GetAssetListAsync(request);

            // Eğer ToViewModel extension metodun varsa kullanabilirsin, yoksa bu şekilde kalsın.
            var model = assets.Select(x => new AssetViewModel
            {
                Id = x.Id,
                Name = x.Name,
                SerialNumber = x.SerialNumber,
                SchoolName = x.School != null ? x.School.Name : "",
                AssetStatusName = x.AssetStatus != null ? x.AssetStatus.Name : ""
            }).ToList();

            // *** DÜZELTME BURADA YAPILDI ***
            // "SelectList" yerine doğrudan "List<SelectListItem>" gönderiyoruz.
            // Çünkü View tarafında "as List<SelectListItem>" dönüşümü yapıyorsun.
            ViewBag.Schools = GetSchoolList();

            // Diğerleri SelectList olabilir çünkü foreach ile dönmüyor, asp-items ile kullanılıyor olabilir.
            var types = await _assetTypeService.GetAssetTypeListAsync();
            ViewBag.Types = new SelectList(types, "Id", "Name", request.AssetTypeId);

            var statuses = await _assetStatusService.GetAssetStatusListAsync();
            ViewBag.Statuses = new SelectList(statuses, "Id", "Name", request.AssetStatusId);

            ViewBag.CurrentFilters = request;

            return View("AssetList", model);
        }

        // -------------------------------------------------------------------------
        // YENİ EKLEME (CREATE)
        // -------------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> CreateAsset()
        {
            ViewBag.Schools = new SelectList(GetSchoolList(), "Value", "Text");
            ViewBag.Types = new SelectList(await _assetTypeService.GetAssetTypeListAsync(), "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsset(AssetCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var statusList = await _assetStatusService.GetAssetStatusListAsync();
                var activeStatus = statusList.FirstOrDefault(x => x.Name == "Aktif" || x.Name == "Active");
                int defaultStatusId = activeStatus != null ? activeStatus.Id : statusList.First().Id;

                var asset = new Asset
                {
                    Name = model.Name,
                    SerialNumber = model.SerialNumber,
                    Description = model.Description,
                    SchoolId = model.SchoolId,
                    AssetTypeId = model.AssetTypeId,
                    AssetStatusId = defaultStatusId,
                    Cost = 0,
                    PurchaseDate = DateTime.Now
                };

                await _assetService.AddAssetAsync(asset);
                return RedirectToAction("Index");
            }

            ViewBag.Schools = new SelectList(GetSchoolList(), "Value", "Text", model.SchoolId);
            ViewBag.Types = new SelectList(await _assetTypeService.GetAssetTypeListAsync(), "Id", "Name", model.AssetTypeId);

            return View(model);
        }

        // -------------------------------------------------------------------------
        // DETAY / DÜZENLEME (ASSET INFO)
        // -------------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> AssetInfo(int id)
        {
            var asset = await _assetService.GetAssetByIdAsync(id);
            if (asset == null) return NotFound();

            var assetImages = await _imageService.GetImagesByAssetIdAsync(id);
            var assetLoans = await _loanService.GetLoansByAssetIdAsync(id);
            var employees = await _employeeService.GetEmployeeListAsync();

            var model = new AssetInfoViewModel
            {
                Id = asset.Id,
                Name = asset.Name,
                SerialNumber = asset.SerialNumber,
                Description = asset.Description,
                SchoolId = asset.SchoolId,
                AssetTypeId = asset.AssetTypeId,
                WarrantyEndDate = asset.WarrantyEndDate,
                Invoice = asset.Invoice,
                Images = assetImages,
                Loans = assetLoans
            };

            // *** DÜZELTME: Burada da Liste formatında gönderiyoruz ***
            ViewBag.Schools = GetSchoolList();

            ViewBag.Types = new SelectList(await _assetTypeService.GetAssetTypeListAsync(), "Id", "Name", asset.AssetTypeId);
            ViewBag.Employees = new SelectList(employees.Where(x => !x.IsDeleted), "Id", "FirstName", "LastName");
            ViewBag.AssetId = id;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AssetInfo(int id, AssetInfoViewModel model)
        {
            if (ModelState.IsValid)
            {
                var asset = await _assetService.GetAssetByIdAsync(id);
                if (asset == null) return NotFound();

                asset.Name = model.Name;
                asset.SerialNumber = model.SerialNumber;
                asset.Description = model.Description;
                asset.SchoolId = model.SchoolId;
                asset.AssetTypeId = model.AssetTypeId;
                asset.WarrantyEndDate = model.WarrantyEndDate;

                await _assetService.UpdateAssetAsync(asset);
                return RedirectToAction("Index");
            }

            // Hata durumunda ViewBag doldurma
            ViewBag.Schools = GetSchoolList();
            ViewBag.Types = new SelectList(await _assetTypeService.GetAssetTypeListAsync(), "Id", "Name", model.AssetTypeId);
            ViewBag.AssetId = id;

            model.Images = await _imageService.GetImagesByAssetIdAsync(id);
            model.Loans = await _loanService.GetLoansByAssetIdAsync(id);

            return View(model);
        }

        // -------------------------------------------------------------------------
        // ZİMMET İŞLEMLERİ
        // -------------------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> AssignAsset(AssignAssetViewModel model)
        {
            if (model.LoanDate.Date > DateTime.Now.Date)
            {
                TempData["Error"] = "Zimmet tarihi bugünden ileri bir tarih olamaz.";
                TempData["ActiveTab"] = "loans";
                return RedirectToAction("AssetInfo", new { id = model.AssetId });
            }

            bool hasActiveLoan = await _loanService.HasActiveLoanAsync(model.AssetId);
            if (hasActiveLoan)
            {
                TempData["Error"] = "Bu demirbaş üzerinde zaten aktif bir zimmet bulunmaktadır! Önce iade almalısınız.";
                TempData["ActiveTab"] = "loans";
                return RedirectToAction("AssetInfo", new { id = model.AssetId });
            }

            var statuses = await _loanStatusService.GetLoanStatusListAsync();
            var assignedStatus = statuses.FirstOrDefault(x => x.Name == "Zimmetli");
            int statusId = assignedStatus != null ? assignedStatus.Id : 1;

            var loan = new Domain.Entity.Loan.Loan
            {
                AssetId = model.AssetId,
                AssignedToId = model.AssignedToId,
                AssignedById = 1,
                LoanDate = model.LoanDate,
                Notes = model.Notes,
                LoanStatusId = statusId,
                ReturnDate = null
            };

            await _loanService.AddLoanAsync(loan);

            TempData["Success"] = "Zimmetleme işlemi başarıyla tamamlandı.";
            TempData["ActiveTab"] = "loans";

            return RedirectToAction("AssetInfo", new { id = model.AssetId });
        }

        [HttpPost]
        public async Task<IActionResult> ReturnAsset(int loanId, int assetId, DateTime returnDate)
        {
            if (returnDate.Date > DateTime.Now.Date)
            {
                TempData["Error"] = "İade tarihi bugünden büyük olamaz.";
                return RedirectToAction("AssetInfo", new { id = assetId });
            }

            var loans = await _loanService.GetLoanListAsync();
            var loan = loans.FirstOrDefault(x => x.Id == loanId);

            if (loan != null)
            {
                var statuses = await _loanStatusService.GetLoanStatusListAsync();
                var returnStatus = statuses.FirstOrDefault(x => x.Name == "İade Alındı");

                loan.ReturnDate = returnDate;
                loan.LoanStatusId = returnStatus != null ? returnStatus.Id : loan.LoanStatusId;

                await _loanService.UpdateLoanAsync(loan);
                TempData["Success"] = "İade işlemi tamamlandı.";
                TempData["ActiveTab"] = "loans";
            }

            return RedirectToAction("AssetInfo", new { id = assetId });
        }

        // -------------------------------------------------------------------------
        // RESİM İŞLEMLERİ (AJAX)
        // -------------------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> AddImage(int assetId, string fileName)
        {
            try
            {
                string apiBaseUrl = "https://localhost:7152/static/"; // Portu kontrol et
                var assetImage = new AssetImage
                {
                    AssetId = assetId,
                    Url = apiBaseUrl + fileName,
                    Path = fileName
                };

                await _imageService.CreateAsync(assetImage);
                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteImage(int id)
        {
            try
            {
                await _imageService.DeleteAsync(id);
                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Silme işlemi sırasında hata oluştu." });
            }
        }

        // -------------------------------------------------------------------------
        // API / AJAX GET METOTLARI
        // -------------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetAssets([FromQuery] AssetFilterRequestModel request)
        {
            var assets = await _assetService.GetAssetListAsync(request);
            var result = assets.Select(x => new
            {
                x.Id,
                x.Name,
                x.SerialNumber,
                School = x.School != null ? new { x.School.Name } : null,
                AssetStatus = x.AssetStatus != null ? new { x.AssetStatus.Name } : null
            });

            return Json(result);
        }

        // -------------------------------------------------------------------------
        // YARDIMCI METOT (MANUEL OKUL LİSTESİ)
        // -------------------------------------------------------------------------
        private List<SelectListItem> GetSchoolList()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Text = "Merkez İlkokulu", Value = "1" },
                new SelectListItem { Text = "Cumhuriyet Lisesi", Value = "2" },
                new SelectListItem { Text = "Atatürk Ortaokulu", Value = "3" }
            };
        }
    }
}