using ClosedXML.Excel;
using MEC.Application.Abstractions.Service.AssetService;
using MEC.Application.Abstractions.Service.AssetService.Model;
using MEC.Application.Abstractions.Service.EmployeeService;
using MEC.Application.Abstractions.Service.LoanService;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Application.Abstractions.Service.ServiceHistoryService;
using MEC.Domain.Common;
using MEC.Domain.Entity.Asset;
using MEC.Domain.Entity.Loan;
using MEC.Portal.Extensions;
using MEC.Portal.Models;
using MEC.Portal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MEC.Portal.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/Assets")]
    public class AssetsController : Controller
    {
        private readonly IAssetService _assetService;
        private readonly ISchoolService _schoolService;
        private readonly IAssetTypeService _assetTypeService;
        private readonly IAssetStatusService _assetStatusService;
        private readonly ILoanService _loanService;
        private readonly ILoanStatusService _loanStatusService;
        private readonly IEmployeePortalService _employeePortalService;
        private readonly ISchoolClassService _schoolClassService;
        private readonly IAssetImageService _assetImageService;
        private readonly IAssetAttachmentService _assetAttachmentService;
        private readonly IAssetImageApiClient _assetImageApiClient;
        private readonly IAssetAttachmentApiClient _assetAttachmentApiClient;
        private readonly IServiceHistoryService _serviceHistoryService;

        public AssetsController(
            IAssetService assetService,
            ISchoolService schoolService,
            IAssetTypeService assetTypeService,
            IAssetStatusService assetStatusService,
            ILoanService loanService,
            ILoanStatusService loanStatusService,
            IEmployeePortalService employeePortalService,
            ISchoolClassService schoolClassService,
            IAssetImageService assetImageService,
            IAssetAttachmentService assetAttachmentService,
            IAssetImageApiClient assetImageApiClient,
            IAssetAttachmentApiClient assetAttachmentApiClient,
            IServiceHistoryService serviceHistoryService)
        {
            _assetService = assetService;
            _schoolService = schoolService;
            _assetTypeService = assetTypeService;
            _assetStatusService = assetStatusService;
            _loanService = loanService;
            _loanStatusService = loanStatusService;
            _employeePortalService = employeePortalService;
            _schoolClassService = schoolClassService;
            _assetImageService = assetImageService;
            _assetAttachmentService = assetAttachmentService;
            _assetImageApiClient = assetImageApiClient;
            _assetAttachmentApiClient = assetAttachmentApiClient;
            _serviceHistoryService = serviceHistoryService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index([FromQuery] AssetFilterRequestModel request)
        {
            var pagedAssets = await _assetService.GetPagedAssetListAsync(request);

            var model = new PagedResult<AssetListViewModel>
            {
                CurrentPage = pagedAssets.CurrentPage,
                PageCount = pagedAssets.PageCount,
                PageSize = pagedAssets.PageSize,
                RowCount = pagedAssets.RowCount,
                Results = pagedAssets.Results.Select(x => x.ToViewModel()).ToList()
            };

            await PopulateListSelectListsAsync(request);
            return View(model);
        }

        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            await PopulateCreateSelectListsAsync();
            return View(new AssetCreateViewModel());
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AssetCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateCreateSelectListsAsync(model.SchoolId, model.AssetTypeId, model.SchoolClassId);
                return View(model);
            }

            var statusList = await _assetStatusService.GetAssetStatusListAsync();
            var activeStatus = statusList.FirstOrDefault(x =>
                string.Equals(x.Name, "Aktif", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.Name, "Active", StringComparison.OrdinalIgnoreCase));

            var asset = new Asset
            {
                Name = model.Name,
                SerialNumber = model.SerialNumber,
                Description = model.Description,
                SchoolId = model.SchoolId,
                AssetTypeId = model.AssetTypeId,
                AssetStatusId = activeStatus?.Id ?? statusList.First().Id,
                Cost = 0,
                WarrantyEndDate = model.WarrantyEndDate,
                InvoiceDate = model.InvoiceDate,
                SchoolClassId = model.SchoolClassId
            };

            await _assetService.AddAssetAsync(asset);
            TempData["Success"] = "Envanter kaydı oluşturuldu.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> AssetInfo(int id, string? serviceSort = null, string? activeTab = null)
        {
            var asset = await _assetService.GetAssetByIdAsync(id);
            if (asset == null)
            {
                return NotFound();
            }

            var model = new AssetInfoViewModel
            {
                Id = asset.Id,
                Name = asset.Name ?? string.Empty,
                SerialNumber = asset.SerialNumber,
                Description = asset.Description,
                SchoolId = asset.SchoolId,
                AssetTypeId = asset.AssetTypeId,
                WarrantyEndDate = asset.WarrantyEndDate,
                InvoiceDate = asset.InvoiceDate,
                SchoolClassId = asset.SchoolClassId,
                Invoice = asset.Invoice,
                Images = await _assetImageService.GetImagesByAssetIdAsync(id),
                Loans = await _loanService.GetLoansByAssetIdAsync(id),
                ServiceHistories = await _serviceHistoryService.GetServiceHistoriesByAssetIdAsync(id, serviceSort)
            };

            await PopulateAssetInfoSelectListsAsync(asset.SchoolId, asset.AssetTypeId, asset.SchoolClassId);
            ViewBag.AssetId = id;
            ViewBag.ServiceSort = serviceSort;
            ViewBag.ActiveTab = activeTab ?? TempData["ActiveTab"]?.ToString() ?? "general";

            return View(model);
        }

        [HttpPost("{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssetInfo(int id, AssetInfoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Images = await _assetImageService.GetImagesByAssetIdAsync(id);
                model.Loans = await _loanService.GetLoansByAssetIdAsync(id);
                model.ServiceHistories = await _serviceHistoryService.GetServiceHistoriesByAssetIdAsync(id, null);
                await PopulateAssetInfoSelectListsAsync(model.SchoolId, model.AssetTypeId, model.SchoolClassId);
                ViewBag.AssetId = id;
                ViewBag.ActiveTab = "general";
                return View(model);
            }

            var asset = await _assetService.GetAssetByIdAsync(id);
            if (asset == null)
            {
                return NotFound();
            }

            asset.Name = model.Name;
            asset.SerialNumber = model.SerialNumber;
            asset.Description = model.Description;
            asset.SchoolId = model.SchoolId;
            asset.AssetTypeId = model.AssetTypeId;
            asset.WarrantyEndDate = model.WarrantyEndDate;
            asset.InvoiceDate = model.InvoiceDate;
            asset.SchoolClassId = model.SchoolClassId;

            await _assetService.UpdateAssetAsync(asset);
            TempData["Success"] = "Envanter bilgileri güncellendi.";
            TempData["ActiveTab"] = "general";
            return RedirectToAction(nameof(AssetInfo), new { id });
        }

        [HttpPost("Assign")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignAsset(AssignAssetViewModel model)
        {
            if (model.LoanDate.Date > DateTime.Now.Date)
            {
                TempData["Error"] = "Zimmet tarihi bugünden ileri bir tarih olamaz.";
                TempData["ActiveTab"] = "loans";
                return RedirectToAction(nameof(AssetInfo), new { id = model.AssetId });
            }

            if (await _loanService.HasActiveLoanAsync(model.AssetId))
            {
                TempData["Error"] = "Bu envanter üzerinde zaten aktif bir zimmet bulunmaktadır.";
                TempData["ActiveTab"] = "loans";
                return RedirectToAction(nameof(AssetInfo), new { id = model.AssetId });
            }

            var statuses = await _loanStatusService.GetLoanStatusListAsync();
            var assignedStatus = statuses.FirstOrDefault(x => string.Equals(x.Name, "Zimmetli", StringComparison.OrdinalIgnoreCase));
            var currentPortalUser = await _employeePortalService.GetActivePortalUserByEmailAsync(User.Identity?.Name ?? string.Empty);

            var loan = new Loan
            {
                AssetId = model.AssetId,
                AssignedToId = model.AssignedToId,
                AssignedById = currentPortalUser?.Id,
                LoanDate = model.LoanDate,
                Notes = model.Notes,
                LoanStatusId = assignedStatus?.Id ?? statuses.First().Id,
                ReturnDate = null
            };

            await _loanService.AddLoanAsync(loan);
            TempData["Success"] = "Zimmetleme işlemi tamamlandı.";
            TempData["ActiveTab"] = "loans";
            return RedirectToAction(nameof(AssetInfo), new { id = model.AssetId });
        }

        [HttpPost("Return")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnAsset(int loanId, int assetId, DateTime returnDate)
        {
            if (returnDate.Date > DateTime.Now.Date)
            {
                TempData["Error"] = "İade tarihi bugünden büyük olamaz.";
                TempData["ActiveTab"] = "loans";
                return RedirectToAction(nameof(AssetInfo), new { id = assetId });
            }

            var loans = await _loanService.GetLoanListAsync();
            var loan = loans.FirstOrDefault(x => x.Id == loanId);
            if (loan == null)
            {
                TempData["Error"] = "Zimmet kaydı bulunamadı.";
                TempData["ActiveTab"] = "loans";
                return RedirectToAction(nameof(AssetInfo), new { id = assetId });
            }

            var statuses = await _loanStatusService.GetLoanStatusListAsync();
            var returnStatus = statuses.FirstOrDefault(x => string.Equals(x.Name, "İade Alındı", StringComparison.OrdinalIgnoreCase));

            loan.ReturnDate = returnDate;
            if (returnStatus != null)
            {
                loan.LoanStatusId = returnStatus.Id;
            }

            await _loanService.UpdateLoanAsync(loan);
            TempData["Success"] = "İade işlemi tamamlandı.";
            TempData["ActiveTab"] = "loans";
            return RedirectToAction(nameof(AssetInfo), new { id = assetId });
        }

        [HttpPost("UploadImage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImage(int assetId, IFormFile file, CancellationToken cancellationToken)
        {
            var uploadResult = await _assetImageApiClient.UploadAsync(assetId, file, cancellationToken);
            if (!uploadResult.IsSuccess)
            {
                return Json(new { success = false, message = uploadResult.Message });
            }

            try
            {
                var existingImage = await _assetImageService.GetImageByAssetAndNameAsync(assetId, uploadResult.FileName);
                if (existingImage != null)
                {
                    existingImage.UpdateDate = DateTime.Now;
                    await _assetImageService.UpdateAssetImageAsync(existingImage);
                }
                else
                {
                    await _assetImageService.CreateAsync(new AssetImage
                    {
                        AssetId = assetId,
                        Path = uploadResult.FileName,
                        CreatedDate = DateTime.Now
                    });
                }

                return Json(new { success = true, message = "Resim başarıyla yüklendi.", fileName = uploadResult.FileName });
            }
            catch (Exception ex)
            {
                await _assetImageApiClient.DeleteAsync(assetId, uploadResult.FileName, cancellationToken);
                return Json(new { success = false, message = "Resim kaydı oluşturulamadı: " + ex.Message });
            }
        }

        [HttpGet("ListImages")]
        public async Task<IActionResult> ListAssetImages(int assetId)
        {
            var images = await _assetImageService.GetImagesByAssetIdAsync(assetId);
            var files = images
                .Where(x => !string.IsNullOrWhiteSpace(x.Path))
                .OrderByDescending(x => x.CreatedDate ?? DateTime.MinValue)
                .ThenByDescending(x => x.Id)
                .Select(x => new
                {
                    fileName = x.Path,
                    url = Url.Action(nameof(AssetImageFile), "Assets", new { assetId, fileName = x.Path })
                })
                .ToList();

            return Json(new { success = true, files });
        }

        [HttpGet("ImageFile")]
        public async Task<IActionResult> AssetImageFile(int assetId, string fileName, CancellationToken cancellationToken)
        {
            var imageRecord = await _assetImageService.GetImageByAssetAndNameAsync(assetId, fileName);
            if (imageRecord == null)
            {
                return NotFound();
            }

            var downloadResult = await _assetImageApiClient.DownloadAsync(assetId, fileName, cancellationToken);
            if (!downloadResult.IsSuccess)
            {
                return NotFound(downloadResult.Message);
            }

            return File(downloadResult.Content, downloadResult.ContentType);
        }

        [HttpPost("DeleteImage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int assetId, string fileName, CancellationToken cancellationToken)
        {
            try
            {
                var imageRecord = await _assetImageService.GetImageByAssetAndNameAsync(assetId, fileName);
                if (imageRecord == null)
                {
                    return Json(new { success = true, message = "Kayıt zaten mevcut değil." });
                }

                var deleteResult = await _assetImageApiClient.DeleteAsync(assetId, fileName, cancellationToken);
                if (!deleteResult.IsSuccess)
                {
                    return Json(new { success = false, message = deleteResult.Message });
                }

                await _assetImageService.DeleteAsync(imageRecord.Id);
                return Json(new { success = true, message = "Resim silindi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Resim silinirken hata oluştu: " + ex.Message });
            }
        }

        [HttpPost("UploadAttachment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAttachment(int assetId, IFormFile file, CancellationToken cancellationToken)
        {
            var uploadResult = await _assetAttachmentApiClient.UploadAsync(assetId, file, cancellationToken);
            if (!uploadResult.IsSuccess)
            {
                return Json(new { success = false, message = uploadResult.Message });
            }

            try
            {
                var existingAttachment = await _assetAttachmentService.GetAttachmentByAssetAndNameAsync(assetId, uploadResult.FileName);
                if (existingAttachment != null)
                {
                    existingAttachment.UpdateDate = DateTime.Now;
                    await _assetAttachmentService.UpdateAttachmentAsync(existingAttachment);
                }
                else
                {
                    await _assetAttachmentService.CreateAsync(new AssetAttachment
                    {
                        AssetId = assetId,
                        Path = uploadResult.FileName,
                        CreatedDate = DateTime.Now
                    });
                }

                return Json(new { success = true, message = "Ek başarıyla yüklendi.", fileName = uploadResult.FileName });
            }
            catch (Exception ex)
            {
                await _assetAttachmentApiClient.DeleteAsync(assetId, uploadResult.FileName, cancellationToken);
                return Json(new { success = false, message = "Ek kaydı oluşturulamadı: " + ex.Message });
            }
        }

        [HttpGet("ListAttachments")]
        public async Task<IActionResult> ListAssetAttachments(int assetId)
        {
            var attachments = await _assetAttachmentService.GetAttachmentsByAssetIdAsync(assetId);
            var files = attachments
                .Where(x => !string.IsNullOrWhiteSpace(x.Path))
                .OrderByDescending(x => x.CreatedDate ?? DateTime.MinValue)
                .ThenByDescending(x => x.Id)
                .Select(x => new
                {
                    fileName = x.Path,
                    url = Url.Action(nameof(AssetAttachmentFile), "Assets", new { assetId, fileName = x.Path })
                })
                .ToList();

            return Json(new { success = true, files });
        }

        [HttpGet("AttachmentFile")]
        public async Task<IActionResult> AssetAttachmentFile(int assetId, string fileName, CancellationToken cancellationToken)
        {
            var attachmentRecord = await _assetAttachmentService.GetAttachmentByAssetAndNameAsync(assetId, fileName);
            if (attachmentRecord == null)
            {
                return NotFound();
            }

            var downloadResult = await _assetAttachmentApiClient.DownloadAsync(assetId, fileName, cancellationToken);
            if (!downloadResult.IsSuccess)
            {
                return NotFound(downloadResult.Message);
            }

            return File(downloadResult.Content, downloadResult.ContentType, downloadResult.FileName);
        }

        [HttpPost("DeleteAttachment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttachment(int assetId, string fileName, CancellationToken cancellationToken)
        {
            try
            {
                var attachment = await _assetAttachmentService.GetAttachmentByAssetAndNameAsync(assetId, fileName);
                if (attachment == null)
                {
                    return Json(new { success = true, message = "Kayıt zaten mevcut değil." });
                }

                var deleteResult = await _assetAttachmentApiClient.DeleteAsync(assetId, fileName, cancellationToken);
                if (!deleteResult.IsSuccess)
                {
                    return Json(new { success = false, message = deleteResult.Message });
                }

                await _assetAttachmentService.DeleteAsync(attachment.Id);
                return Json(new { success = true, message = "Ek silindi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ek silinirken hata oluştu: " + ex.Message });
            }
        }

        [HttpGet("Export")]
        public async Task<IActionResult> ExportToExcel([FromQuery] AssetFilterRequestModel request)
        {
            var assets = await _assetService.GetAssetListAsync(request);

            using var workbook = new XLWorkbook();
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

            var headerRange = worksheet.Range("A1:J1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            var row = 2;
            foreach (var item in assets)
            {
                worksheet.Cell(row, 1).Value = item.SerialNumber;
                worksheet.Cell(row, 2).Value = item.Name;
                worksheet.Cell(row, 3).Value = item.AssetType?.Name ?? "-";
                worksheet.Cell(row, 4).Value = item.School?.Name ?? "-";
                worksheet.Cell(row, 5).Value = item.SchoolClass?.Name ?? "-";
                worksheet.Cell(row, 6).Value = item.AssetStatus?.Name ?? "-";
                worksheet.Cell(row, 7).Value = item.Description;
                worksheet.Cell(row, 8).Value = item.PurchaseDate;
                worksheet.Cell(row, 9).Value = item.WarrantyEndDate;
                worksheet.Cell(row, 10).Value = item.InvoiceDate;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Envanter_Listesi_{DateTime.Now:ddMMyyyy}.xlsx");
        }

        [HttpGet("GetClassesBySchool")]
        public async Task<IActionResult> GetClassesBySchool(int schoolId)
        {
            var classes = await _schoolClassService.GetSchoolClassesBySchoolIdAsync(schoolId);
            var result = classes
                .Select(x => new { id = x.Id, name = x.Name })
                .OrderBy(x => x.name)
                .ToList();

            return Json(result);
        }

        [HttpGet("{assetId:int}/ServiceHistory/Create")]
        public async Task<IActionResult> CreateServiceHistory(int assetId)
        {
            var asset = await _assetService.GetAssetByIdAsync(assetId);
            if (asset == null)
            {
                return NotFound();
            }

            ViewBag.AssetName = asset.Name;
            return View(new ServiceHistoryCreateViewModel
            {
                AssetId = assetId,
                SendDate = DateTime.Now.Date
            });
        }

        [HttpPost("{assetId:int}/ServiceHistory/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateServiceHistory(int assetId, ServiceHistoryCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.AssetName = (await _assetService.GetAssetByIdAsync(assetId))?.Name;
                return View(model);
            }

            await _serviceHistoryService.AddServiceHistoryAsync(new ServiceHistory
            {
                AssetId = model.AssetId,
                SendDate = model.SendDate,
                ReturnDate = model.ReturnDate,
                Description = model.Description,
                ServiceCompany = model.ServiceCompany,
                Cost = model.Cost,
                IsWarranty = model.IsWarranty
            });

            TempData["Success"] = "Servis kaydı eklendi.";
            TempData["ActiveTab"] = "service";
            return RedirectToAction(nameof(AssetInfo), new { id = assetId });
        }

        private async Task PopulateListSelectListsAsync(AssetFilterRequestModel request)
        {
            ViewBag.Schools = new SelectList(await _schoolService.GetSchoolListAsync(), "Id", "Name");
            ViewBag.Types = new SelectList(await _assetTypeService.GetAssetTypeListAsync(), "Id", "Name");
            ViewBag.Statuses = new SelectList(await _assetStatusService.GetAssetStatusListAsync(), "Id", "Name");
            ViewBag.CurrentSort = request.SortOrder;
            ViewBag.CurrentFilters = request;
        }

        private async Task PopulateCreateSelectListsAsync(int? schoolId = null, int? assetTypeId = null, int? schoolClassId = null)
        {
            ViewBag.Schools = new SelectList(await _schoolService.GetSchoolListAsync(), "Id", "Name", schoolId);
            ViewBag.Types = new SelectList(await _assetTypeService.GetAssetTypeListAsync(), "Id", "Name", assetTypeId);
            ViewBag.Classes = new SelectList(
                schoolId.HasValue ? await _schoolClassService.GetSchoolClassesBySchoolIdAsync(schoolId.Value) : new List<MEC.Domain.Entity.School.SchoolClass>(),
                "Id",
                "Name",
                schoolClassId);
        }

        private async Task PopulateAssetInfoSelectListsAsync(int schoolId, int assetTypeId, int? schoolClassId)
        {
            await PopulateCreateSelectListsAsync(schoolId, assetTypeId, schoolClassId);

            var employees = await _employeePortalService.GetActivePortalUsersAsync();
            ViewBag.Employees = new SelectList(
                employees.Select(x => new
                {
                    x.Id,
                    FullName = string.Join(" ", new[] { x.FirstName, x.LastName }
                        .Where(value => !string.IsNullOrWhiteSpace(value))).Trim()
                }).OrderBy(x => x.FullName),
                "Id",
                "FullName");
        }
    }
}
