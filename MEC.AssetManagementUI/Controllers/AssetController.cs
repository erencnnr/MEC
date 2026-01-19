using ClosedXML.Excel;
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
using MEC.Domain.Common;
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
        private readonly ISchoolClassService _schoolClassService;
        private readonly IAssetImageService _assetImageService;
        private readonly IAssetAttachmentService _assetAttachmentService;
        

        public AssetController(IAssetService assetService, ISchoolService schoolService, IAssetTypeService assetTypeService, IAssetStatusService assetStatusService,
            ILoanService loanService, IAssetImageService imageService, IEmployeeService employeeService, ILoanStatusService loanStatusService, ISchoolClassService schoolClassService,
            IAssetImageService assetImageService, IAssetAttachmentService assetAttachmentService)
        {
            _assetService = assetService;
            _schoolService = schoolService;
            _assetTypeService = assetTypeService;
            _assetStatusService = assetStatusService;
            _loanService = loanService;
            _imageService = imageService;
            _employeeService = employeeService;
            _loanStatusService = loanStatusService;
            _schoolClassService = schoolClassService;
            _assetImageService = assetImageService;
            _assetAttachmentService = assetAttachmentService;
            
        }
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] AssetFilterRequestModel request)
        {
            // 1. Servisten Sayfalı Veriyi Çek
            // Not: request.Page ve request.PageSize (20) modelden otomatik gelir.
            var pagedAssets = await _assetService.GetPagedAssetListAsync(request);

            // 2. Entity -> ViewModel Dönüşümü (Sayfalama verilerini koruyarak)
            var viewModel = new PagedResult<AssetListViewModel>
            {
                CurrentPage = pagedAssets.CurrentPage,
                PageCount = pagedAssets.PageCount,
                PageSize = pagedAssets.PageSize,
                RowCount = pagedAssets.RowCount,
                // Listeyi ViewModel'e çevir
                Results = pagedAssets.Results.Select(x => x.ToViewModel()).ToList()
            };

            // 3. Dropdownları Doldur
            ViewBag.Schools = new SelectList(await _schoolService.GetSchoolListAsync(), "Id", "Name");
            ViewBag.Types = new SelectList(await _assetTypeService.GetAssetTypeListAsync(), "Id", "Name");
            ViewBag.Statuses = new SelectList(await _assetStatusService.GetAssetStatusListAsync(), "Id", "Name");
            ViewBag.CurrentSort = request.SortOrder;
            // Filtreleri View'da korumak için
            ViewBag.CurrentFilters = request;

            return View("AssetList", viewModel);
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
                    WarrantyEndDate = model.WarrantyEndDate,
                    InvoiceDate = model.InvoiceDate,
                    SchoolClassId = model.SchoolClassId
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
            var employees = await _employeeService.GetAllEmployeesAsync();
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
                InvoiceDate = asset.InvoiceDate,
                // Yeni Alanlar
                Invoice = asset.Invoice,
                Images = assetImages,
                Loans = assetLoans
            };

            // 4. Dropdownları Hazırla
            ViewBag.Schools = new SelectList(await _schoolService.GetSchoolListAsync(), "Id", "Name", asset.SchoolId);
            ViewBag.Types = new SelectList(await _assetTypeService.GetAssetTypeListAsync(), "Id", "Name", asset.AssetTypeId);
            ViewBag.Employees = new SelectList(employees.Where(x => !x.IsDeleted)
                                            .Select(x => new {
                                                Id = x.Id,
                                                FullName = $"{x.FirstName} {x.LastName}"
                                            })
                                            .OrderBy(x => x.FullName),
                                    "Id", "FullName");
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

        [HttpPost]
        public async Task<IActionResult> AddImage(int assetId, string fileName)
        {
            // 1. Bu Asset için bu isimde bir resim zaten var mı kontrol et
            // (Servis katmanınızda böyle bir metod olduğunu varsayıyorum)
            var existingImage = await _assetImageService.GetImageByAssetAndNameAsync(assetId, fileName);

            if (existingImage != null)
            {
                // Dosya zaten diskte güncellendi (API tarafında).
                // DB'de kayıt zaten var, sadece update tarihini güncelleyebiliriz veya hiçbir şey yapmayız.
                existingImage.UpdateDate = DateTime.Now;
                await _assetImageService.UpdateAssetImageAsync(existingImage);

                return Json(new { success = true, message = "Mevcut resim güncellendi." });
            }

            // 2. Kayıt yoksa yeni ekle
            var newImage = new AssetImage
            {
                AssetId = assetId,
                Path = fileName, // Sadece isim tutuluyor, klasör ID'den biliniyor
                CreatedDate = DateTime.Now
            };

            await _assetImageService.CreateAsync(newImage);

            return Json(new { success = true, message = "Yeni resim eklendi." });
        }

        [HttpGet] // Form GET ile çalıştığı için HttpGet kullanıyoruz
        public async Task<IActionResult> ExportToExcel(AssetFilterRequestModel request)
        {
            var assets = await _assetService.GetAssetListAsync(request);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Envanter");

                worksheet.Cell(1, 1).Value = "Seri Numarası";
                worksheet.Cell(1, 2).Value = "Ad";
                worksheet.Cell(1, 3).Value = "Türü";
                worksheet.Cell(1, 4).Value = "Konum / Okul";
                worksheet.Cell(1, 5).Value = "Sınıf / Şube";
                worksheet.Cell(1, 6).Value = "Durum";
                worksheet.Cell(1, 7).Value = "Açıklama";
                worksheet.Cell(1, 8).Value = "Alım Tarihi";
                worksheet.Cell(1, 9).Value = "Garanti Bitiş Tarihi";
                worksheet.Cell(1, 10).Value = "Fatura Tarihi";

                // Başlık Stili (Bold ve Arka Plan)
                var headerRange = worksheet.Range("A1:J1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                // Verileri Satırlara Yazma
                int row = 2;
                foreach (var item in assets)
                {
                    worksheet.Cell(row, 1).Value = item.SerialNumber;
                    worksheet.Cell(row, 2).Value = item.Name;
                    worksheet.Cell(row, 3).Value = item.AssetType?.Name ?? "-";
                    worksheet.Cell(row, 4).Value = item.School?.Name ?? "-";
                    worksheet.Cell(row, 5).Value = item.SchoolClass?.Name ?? "-";
                    worksheet.Cell(row, 6).Value = item.AssetStatus?.Name ?? "-";
                    worksheet.Cell(row, 7).Value = item.Description;

                    // Tarih formatları
                    worksheet.Cell(row, 8).Value = item.PurchaseDate;
                    worksheet.Cell(row, 9).Value = item.WarrantyEndDate;
                    worksheet.Cell(row, 10).Value = item.InvoiceDate;

                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Demirbas_Listesi_{DateTime.Now:ddMMyyyy}.xlsx");
                }
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetClassesBySchool(int schoolId)
        {
            // Servisinizde GetListAsync(predicate) gibi bir yapı olduğunu varsayıyorum.
            // Eğer yoksa ISchoolClassService'e bu metodu eklemeniz gerekebilir.
            // Örnek kullanım:
            var classes = await _schoolClassService.GetSchoolClassListAsync(); // Tümünü çekip filtereliyoruz (veya servise parametre geçin)
            var filteredClasses = classes.Where(x => x.SchoolId == schoolId)
                                         .Select(x => new { id = x.Id, name = x.Name })
                                         .OrderBy(x => x.name)
                                         .ToList();

            return Json(filteredClasses);
        }
        [HttpPost]
        public async Task<IActionResult> DeleteImage(int assetId, string fileName)
        {
            try
            {
                // 1. Kaydı bul
                var imageRecord = await _assetImageService.GetImageByAssetAndNameAsync(assetId, fileName);

                // 2. Kayıt varsa sil
                if (imageRecord != null)
                {
                    await _assetImageService.DeleteAsync(imageRecord.Id);
                    return Json(new { success = true, message = "Veritabanından silindi." });
                }

                return Json(new { success = false, message = "Kayıt bulunamadı." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "DB Hatası: " + ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> AddAttachment(int assetId, string fileName)
        {
            try
            {
                // Varsa güncelle, yoksa ekle mantığı
                // (Eğer aynı isimde dosya yüklenirse DB'de mükerrer olmasın diye kontrol)
                // Servisinizde bu metodun (GetByAssetAndName) olduğunu varsayıyoruz, yoksa eklenmeli.
                // Yoksa direkt CreateAsync de yapabilirsiniz ama mükerrer kayıt oluşabilir.

                var attachment = new AssetAttachment
                {
                    AssetId = assetId,
                    Path = fileName, // Dosya adı
                    CreatedDate = DateTime.Now
                };

                await _assetAttachmentService.CreateAsync(attachment);

                return Json(new { success = true, message = "Ek başarıyla kaydedildi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "DB Hatası: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAttachment(int assetId, string fileName)
        {
            try
            {
                // İsimden ve AssetId'den kaydı bul (Servisinizde bu metodun olması gerekir)
                // Eğer yoksa GetAll yapıp LINQ ile de bulabilirsiniz.
                var attachment = await _assetAttachmentService.GetAttachmentByAssetAndNameAsync(assetId, fileName);

                if (attachment != null)
                {
                    await _assetAttachmentService.DeleteAsync(attachment.Id);
                    return Json(new { success = true, message = "Veritabanından silindi." });
                }

                // DB'de yoksa bile diskten silinmesi için true dönüyoruz
                return Json(new { success = true, message = "Kayıt bulunamadı, disk işlemine geçiliyor." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "DB Hatası: " + ex.Message });
            }
        }
    }
}
