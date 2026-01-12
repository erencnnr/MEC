using ClosedXML.Excel;
using MEC.Domain.Common;
using MEC.Application.Abstractions.Service.EmployeeService;
using MEC.Application.Abstractions.Service.LoanService;
using MEC.Application.Abstractions.Service.LoanService.Model;
using MEC.AssetManagementUI.Models.LoanModel;
using MEC.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
            // 1. Sayfalı Veriyi Çek
            var pagedLoans = await _loanService.GetPagedLoanListAsync(request);

            // 2. Çalışan Bilgilerini Çek (İsim eşleştirmesi için)
            var employees = await _employeeService.GetAllEmployeesAsync();
            var empDict = employees.ToDictionary(k => k.Id, v => $"{v.FirstName} {v.LastName}");

            // 3. PagedResult<Loan> -> PagedResult<LoanListViewModel> Dönüşümü
            var viewModel = new PagedResult<LoanListViewModel>
            {
                CurrentPage = pagedLoans.CurrentPage,
                PageCount = pagedLoans.PageCount,
                PageSize = pagedLoans.PageSize,
                RowCount = pagedLoans.RowCount,
                Results = pagedLoans.Results.Select(x => new LoanListViewModel
                {
                    Id = x.Id,
                    AssetId = x.AssetId,
                    AssetName = x.Asset?.Name ?? "-",
                    SerialNumber = x.Asset?.SerialNumber ?? "-",

                    // Zimmet Alan
                    AssignedToName = x.AssignedTo != null ? $"{x.AssignedTo.FirstName} {x.AssignedTo.LastName}" : "-",

                    // Zimmet Veren (Isim sözlükten bakılır)
                    AssignedByName = (x.AssignedById.HasValue && empDict.ContainsKey(x.AssignedById.Value))
                                     ? empDict[x.AssignedById.Value]
                                     : "-",

                    LoanDate = x.LoanDate.ToShortDateString(),
                    ReturnDate = x.ReturnDate?.ToShortDateString()
                }).ToList()
            };

            // 4. Filtre Dropdownları
            ViewBag.Employees = employees.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.FirstName} {x.LastName}"
            }).ToList();

            // Filtreleri View'da korumak için
            ViewBag.CurrentFilters = request;

            return View("LoanList", viewModel);
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
        public async Task<IActionResult> ExportToExcel([FromQuery] LoanFilterRequestModel request)
        {
            // Servis zaten ActiveTab ve diğer filtrelere göre doğru veriyi getiriyor.
            // Ekstra filtrelemeye (Where) gerek yok.
            var loans = await _loanService.GetLoanListAsync(request);

            var employees = await _employeeService.GetAllEmployeesAsync();
            var empDict = employees.ToDictionary(k => k.Id, v => $"{v.FirstName} {v.LastName}");

            using (var workbook = new XLWorkbook())
            {
                string sheetName = request.ActiveTab == "history" ? "Zimmet Geçmişi" : "Aktif Zimmetler";
                // Excel sayfa ismi max 31 karakter olabilir
                if (sheetName.Length > 31) sheetName = sheetName.Substring(0, 31);

                var worksheet = workbook.Worksheets.Add(sheetName);

                // --- BAŞLIKLAR ---
                worksheet.Cell(1, 1).Value = "Demirbaş Adı";
                worksheet.Cell(1, 2).Value = "Seri No";
                worksheet.Cell(1, 3).Value = "Zimmet Alan";
                worksheet.Cell(1, 4).Value = "Zimmet Veren";
                worksheet.Cell(1, 5).Value = "Zimmet Tarihi";
                worksheet.Cell(1, 6).Value = "İade Tarihi"; // İsteğe bağlı eklenebilir
                worksheet.Cell(1, 7).Value = "Durum";

                var headerRange = worksheet.Range("A1:G1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

                // --- VERİLER ---
                int row = 2;
                foreach (var item in loans)
                {
                    worksheet.Cell(row, 1).Value = item.Asset?.Name ?? "-";
                    worksheet.Cell(row, 2).Value = item.Asset?.SerialNumber ?? "-";
                    worksheet.Cell(row, 3).Value = item.AssignedTo != null ? $"{item.AssignedTo.FirstName} {item.AssignedTo.LastName}" : "-";

                    var assignedBy = (item.AssignedById.HasValue && empDict.ContainsKey(item.AssignedById.Value))
                                     ? empDict[item.AssignedById.Value]
                                     : "-";
                    worksheet.Cell(row, 4).Value = assignedBy;

                    worksheet.Cell(row, 5).Value = item.LoanDate;
                    worksheet.Cell(row, 5).Style.DateFormat.Format = "dd.MM.yyyy";

                    if (item.ReturnDate.HasValue)
                    {
                        worksheet.Cell(row, 6).Value = item.ReturnDate.Value;
                        worksheet.Cell(row, 6).Style.DateFormat.Format = "dd.MM.yyyy";
                    }
                    else
                    {
                        worksheet.Cell(row, 6).Value = "-";
                    }

                    string status = item.ReturnDate == null ? "Zimmetli" : "İade Alındı";
                    worksheet.Cell(row, 7).Value = status;

                    if (item.ReturnDate == null)
                        worksheet.Cell(row, 7).Style.Font.FontColor = XLColor.OrangeRed;
                    else
                        worksheet.Cell(row, 7).Style.Font.FontColor = XLColor.Green;

                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"Zimmet_{DateTime.Now:ddMMyyyy_HHmm}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }
    }
}