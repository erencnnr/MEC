using MEC.Application.Abstractions.Service.LoanService.Model;
using MEC.Application.Abstractions.Service.LoanService;
using MEC.AssetManagementUI.Models.LoanModel;
using Microsoft.AspNetCore.Mvc;
using MEC.Application.Abstractions.Service.EmployeeService;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using ClosedXML.Excel;

namespace MEC.AssetManagementUI.Controllers
{
    public class LoanController : Controller
    {
        private readonly ILoanService _loanService;
        private readonly IEmployeeService _employeeService;

        public LoanController(ILoanService loanService, IEmployeeService employeeService)
        {
            _loanService = loanService;
            _employeeService = employeeService;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] LoanFilterRequestModel request)
        {
            var loans = await _loanService.GetLoanListAsync(request);
            var employees = await _employeeService.GetAllEmployeesAsync();

            // Çalışan listesini Dictionary'e çevirip hızlı erişim sağlıyoruz (ID -> Ad Soyad)
            var empDict = employees.ToDictionary(k => k.Id, v => $"{v.FirstName} {v.LastName}");

            var model = loans.Select(x => new LoanListViewModel
            {
                Id = x.Id,
                AssetId = x.AssetId,
                AssetName = x.Asset?.Name ?? "-",
                SerialNumber = x.Asset?.SerialNumber ?? "-",

                // Zimmet Alan
                AssignedToName = x.AssignedTo != null ? $"{x.AssignedTo.FirstName} {x.AssignedTo.LastName}" : "-",

                // Zimmet Veren (CreatedBy ID'sini Employee listesinde arıyoruz)
                AssignedByName = empDict.ContainsKey((int)x.AssignedById) ? empDict[(int)x.AssignedById] : "Sistem/Bilinmiyor",

                LoanDate = x.LoanDate.ToShortDateString(),
                ReturnDate = x.ReturnDate?.ToShortDateString(),
                // View tarafında IsActive ile sekmelere ayıracağız
            }).ToList();

            // Filtre Dropdownları için ViewBag
            ViewBag.Employees = employees.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.FirstName} {x.LastName}"
            }).ToList();

            ViewBag.CurrentFilters = request;

            return View("LoanList",model);
        }

        [HttpPost]
        public async Task<IActionResult> BulkReturn(List<int> selectedLoanIds, DateTime returnDate)
        {
            if (selectedLoanIds == null || !selectedLoanIds.Any())
            {
                TempData["Error"] = "Lütfen seçim yapınız.";
                return RedirectToAction("Index");
            }

            await _loanService.BulkReturnLoansAsync(selectedLoanIds, returnDate);
            TempData["Success"] = "Seçili zimmetler iade alındı.";
            return RedirectToAction("Index");
        }
        
        [HttpGet]
        public async Task<IActionResult> ExportToExcel(LoanFilterRequestModel request)
        {
            // 1. Filtre kriterlerine (Arama, Personel vb.) uyan TÜM veriyi çek
            var loans = await _loanService.GetLoanListAsync(request);

            // 2. SEKME FİLTRESİ (ActiveTab kontrolü)
            // Eğer "history" sekmesindeyse iade tarihi olanları (geçmiş), 
            // değilse ("active" veya boş) iade tarihi olmayanları (aktif) filtrele.
            if (request.ActiveTab == "history")
            {
                loans = loans.Where(x => x.ReturnDate != null).ToList();
            }
            else
            {
                loans = loans.Where(x => x.ReturnDate == null).ToList();
            }

            // 3. Çalışan isimlerini eşleştirmek için sözlük (Dictionary) oluştur
            var employees = await _employeeService.GetAllEmployeesAsync();
            var empDict = employees.ToDictionary(k => k.Id, v => $"{v.FirstName} {v.LastName}");

            // 4. Excel Oluştur
            using (var workbook = new XLWorkbook())
            {
                // Sekme adına göre sayfa adı verelim
                string sheetName = request.ActiveTab == "history" ? "Zimmet Geçmişi" : "Aktif Zimmetler";
                var worksheet = workbook.Worksheets.Add(sheetName);

                // --- BAŞLIKLAR ---
                worksheet.Cell(1, 1).Value = "Demirbaş Adı";
                worksheet.Cell(1, 2).Value = "Seri No";
                worksheet.Cell(1, 3).Value = "Zimmet Alan";
                worksheet.Cell(1, 4).Value = "Zimmet Veren";
                worksheet.Cell(1, 5).Value = "Zimmet Tarihi";
                // İade Tarihi istenen listede yoktu ama geçmiş sekmesi için opsiyonel eklenebilir. 
                // Şimdilik isteğinize sadık kalarak eklemiyorum, sadece Durum'u ekliyorum.
                worksheet.Cell(1, 6).Value = "Durum";

                // Başlık Stili
                var headerRange = worksheet.Range("A1:F1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

                // --- VERİLERİ YAZMA ---
                int row = 2;
                foreach (var item in loans)
                {
                    worksheet.Cell(row, 1).Value = item.Asset?.Name ?? "-";
                    worksheet.Cell(row, 2).Value = item.Asset?.SerialNumber ?? "-";

                    // Zimmet Alan
                    var assignedTo = item.AssignedTo != null ? $"{item.AssignedTo.FirstName} {item.AssignedTo.LastName}" : "-";
                    worksheet.Cell(row, 3).Value = assignedTo;

                    // Zimmet Veren
                    var assignedBy = empDict.ContainsKey((int)(int)item.AssignedById) ? empDict[(int)item.AssignedById] : "-";
                    worksheet.Cell(row, 4).Value = assignedBy;

                    // Tarih Formatı
                    worksheet.Cell(row, 5).Value = item.LoanDate;
                    worksheet.Cell(row, 5).Style.DateFormat.Format = "dd.MM.yyyy";

                    // Durum
                    string status = item.ReturnDate == null ? "Zimmetli" : "İade Alındı";
                    worksheet.Cell(row, 6).Value = status;

                    // İade alındıysa yeşil, zimmetliyse sarı hücre rengi (Opsiyonel Görsellik)
                    if (item.ReturnDate == null)
                        worksheet.Cell(row, 6).Style.Font.FontColor = XLColor.OrangeRed;
                    else
                        worksheet.Cell(row, 6).Style.Font.FontColor = XLColor.Green;

                    row++;
                }

                // Sütun genişliklerini otomatik ayarla
                worksheet.Columns().AdjustToContents();

                // Dosyayı İndirt
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"Zimmet_{sheetName.Replace(" ", "_")}_{DateTime.Now:ddMMyyyy}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }
    }
}
