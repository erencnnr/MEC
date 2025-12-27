using MEC.Application.Abstractions.Service.AssetService;
using MEC.Application.Abstractions.Service.AssetService.Model;
using MEC.Application.Abstractions.Service.EmployeeService;
using MEC.Application.Abstractions.Service.LoanService;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Application.Service.AssetService;
using MEC.Application.Service.LoanService;
using MEC.Application.Service.SchoolService;
using MEC.AssetManagementUI.Extensions;
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
        private ILoanStatusService _loanStatusService;
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
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] AssetFilterRequestModel request)
        {
            var assets = await _assetService.GetAssetListAsync(request);

            var model = assets.Select(x => x.ToViewModel()).ToList();

            ViewBag.Schools = new SelectList(await _schoolService.GetSchoolListAsync(), "Id", "Name", request.SchoolId);
            ViewBag.Types = new SelectList(await _assetTypeService.GetAssetTypeListAsync(), "Id", "Name", request.AssetTypeId);
            ViewBag.Statuses = new SelectList(await _assetStatusService.GetAssetStatusListAsync(), "Id", "Name", request.AssetStatusId);
            ViewBag.CurrentFilters = request;

            return View("AssetList",model);
        }
        [HttpGet]
        public async Task<IActionResult> CreateAsset()
        {
            
            ViewBag.Schools = new SelectList(await _schoolService.GetSchoolListAsync(), "Id", "Name");
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

                var asset = new MEC.Domain.Entity.Asset.Asset
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

            ViewBag.Schools = new SelectList(await _schoolService.GetSchoolListAsync(), "Id", "Name", model.SchoolId);
            ViewBag.Types = new SelectList(await _assetTypeService.GetAssetTypeListAsync(), "Id", "Name", model.AssetTypeId);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> AssetInfo(int id)
        {
            // 1. Varlık bilgisini (Fatura dahil) çek
            var asset = await _assetService.GetAssetByIdAsync(id);
            if (asset == null) return NotFound();

            // 2. Resim ve Zimmet listelerini servislerden çek
            var assetImages = await _imageService.GetImagesByAssetIdAsync(id);
            var assetLoans = await _loanService.GetLoansByAssetIdAsync(id);
            var employees = await _employeeService.GetEmployeeListAsync();
            // 3. ViewModel'i Doldur
            var model = new AssetInfoViewModel
            {
                Id = asset.Id,
                Name = asset.Name,
                SerialNumber = asset.SerialNumber,
                Description = asset.Description,
                SchoolId = asset.SchoolId,
                AssetTypeId = asset.AssetTypeId,
                WarrantyEndDate = asset.WarrantyEndDate,
                // Yeni Alanlar
                Invoice = asset.Invoice,
                Images = assetImages,
                Loans = assetLoans
            };

            // 4. Dropdownları Hazırla
            ViewBag.Schools = new SelectList(await _schoolService.GetSchoolListAsync(), "Id", "Name", asset.SchoolId);
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

                // Sadece düzenlenebilir alanları güncelle
                asset.Name = model.Name;
                asset.SerialNumber = model.SerialNumber;
                asset.Description = model.Description;
                asset.SchoolId = model.SchoolId;
                asset.AssetTypeId = model.AssetTypeId;
                asset.WarrantyEndDate = model.WarrantyEndDate;
                await _assetService.UpdateAssetAsync(asset);

                // Başarılı olursa listeye veya aynı sayfaya dönebilirsin
                return RedirectToAction("Index");
            }

            // Hata durumunda verileri tekrar yükle (Dropdownlar vb.)
            ViewBag.Schools = new SelectList(await _schoolService.GetSchoolListAsync(), "Id", "Name", model.SchoolId);
            ViewBag.Types = new SelectList(await _assetTypeService.GetAssetTypeListAsync(), "Id", "Name", model.AssetTypeId);
            ViewBag.AssetId = id;

            // Listeler null gitmesin diye tekrar çekiyoruz (Opsiyonel, view null check yapıyorsa gerekmeyebilir)
            model.Images = await _imageService.GetImagesByAssetIdAsync(id);
            model.Loans = await _loanService.GetLoansByAssetIdAsync(id);

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> AssignAsset(AssignAssetViewModel model)
        {
            if (model.LoanDate.Date > DateTime.Now.Date)
            {
                TempData["Error"] = "Zimmet tarihi bugünden ileri bir tarih olamaz.";
                TempData["ActiveTab"] = "loans"; // <--- Hata olsa da Zimmet sekmesinde kal
                return RedirectToAction("AssetInfo", new { id = model.AssetId });
            }

            bool hasActiveLoan = await _loanService.HasActiveLoanAsync(model.AssetId);
            if (hasActiveLoan)
            {
                TempData["Error"] = "Bu demirbaş üzerinde zaten aktif bir zimmet bulunmaktadır! Önce iade almalısınız.";
                TempData["ActiveTab"] = "loans"; // <--- Zimmet sekmesinde kal
                return RedirectToAction("AssetInfo", new { id = model.AssetId });
            }

            var statuses = await _loanStatusService.GetLoanStatusListAsync();
            var assignedStatus = statuses.FirstOrDefault(x => x.Name == "Zimmetli");
            int statusId = assignedStatus != null ? assignedStatus.Id : 1; 

            var loan = new MEC.Domain.Entity.Loan.Loan
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

            // 1. Zimmet Kaydını Getir
            var loans = await _loanService.GetLoanListAsync(); // veya GetByIdAsync
            var loan = loans.FirstOrDefault(x => x.Id == loanId);

            if (loan != null)
            {
                // 2. "İade Alındı" Durumunu Bul
                var statuses = await _loanStatusService.GetLoanStatusListAsync();
                var returnStatus = statuses.FirstOrDefault(x => x.Name == "İade Alındı");

                // 3. Güncelle
                loan.ReturnDate = returnDate;
                loan.LoanStatusId = returnStatus != null ? returnStatus.Id : loan.LoanStatusId;

                await _loanService.UpdateLoanAsync(loan);
                TempData["Success"] = "İade işlemi tamamlandı.";
                TempData["ActiveTab"] = "loans";
            }

            return RedirectToAction("AssetInfo", new { id = assetId });
        }

        [HttpPost]
        public async Task<IActionResult> AddImage(int assetId, string fileName)
        {
            try
            {
                // API adresiniz (Portu kendi çalışan portunuzla değiştirmeyi unutmayın)
                string apiBaseUrl = "https://localhost:7152/static/";

                var assetImage = new MEC.Domain.Entity.Asset.AssetImage
                {
                    AssetId = assetId,
                    Url = apiBaseUrl + fileName, // Tarayıcının erişeceği URL
                    Path = fileName             // Dosya adı veya fiziksel yol (İsteğinize göre)
                                                // IsDeleted = false;       // BU SATIRI SİLDİK
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
                // Not: Şimdilik sadece veritabanından kaydı siliyoruz.
                // Fiziksel dosyayı (C:\Images) silmek için API'ye ayrı bir istek atılması gerekir.
                // Ancak DB'den silmek UI'dan kaybolması için yeterlidir.

                await _imageService.DeleteAsync(id);
                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Silme işlemi sırasında hata oluştu." });
            }
        }
    }
}
