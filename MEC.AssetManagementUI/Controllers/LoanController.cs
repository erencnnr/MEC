using ClosedXML.Excel;
using MEC.Application.Abstractions.Service.EmployeeService;
using MEC.Application.Abstractions.Service.LoanService;
using MEC.Application.Abstractions.Service.LoanService.Model;
using MEC.AssetManagementUI.Models.LoanModel;
using MEC.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MEC.AssetManagementUI.Controllers
{
    public class LoanController : Controller
    {
        private readonly ILoanService _loanService;
        private readonly IEmployeePortalService _employeePortalService;

        public LoanController(ILoanService loanService, IEmployeePortalService employeePortalService)
        {
            _loanService = loanService;
            _employeePortalService = employeePortalService;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] LoanFilterRequestModel request)
        {
            var pagedLoans = await _loanService.GetPagedLoanListAsync(request);
            var employees = await _employeePortalService.GetActivePortalUsersAsync();
            var empDict = employees.ToDictionary(k => k.Id, v => $"{v.FirstName} {v.LastName}");

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
                    AssignedToName = x.AssignedTo != null ? $"{x.AssignedTo.FirstName} {x.AssignedTo.LastName}" : "-",
                    AssignedByName = (x.AssignedById.HasValue && empDict.ContainsKey(x.AssignedById.Value))
                                     ? empDict[x.AssignedById.Value]
                                     : "-",
                    LoanDate = x.LoanDate.ToShortDateString(),
                    ReturnDate = x.ReturnDate?.ToShortDateString()
                }).ToList()
            };

            ViewBag.Employees = employees.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.FirstName} {x.LastName}"
            }).ToList();

            ViewBag.CurrentSort = request.SortOrder;
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
            var loans = await _loanService.GetLoanListAsync(request);
            var employees = await _employeePortalService.GetActivePortalUsersAsync();
            var empDict = employees.ToDictionary(k => k.Id, v => $"{v.FirstName} {v.LastName}");

            using var workbook = new XLWorkbook();
            var sheetName = request.ActiveTab == "history" ? "Zimmet Geçmişi" : "Aktif Zimmetler";
            if (sheetName.Length > 31)
            {
                sheetName = sheetName[..31];
            }

            var worksheet = workbook.Worksheets.Add(sheetName);
            worksheet.Cell(1, 1).Value = "Ad";
            worksheet.Cell(1, 2).Value = "Seri No";
            worksheet.Cell(1, 3).Value = "Zimmet Alan";
            worksheet.Cell(1, 4).Value = "Zimmet Veren";
            worksheet.Cell(1, 5).Value = "Zimmet Tarihi";
            worksheet.Cell(1, 6).Value = "İade Tarihi";
            worksheet.Cell(1, 7).Value = "Durum";

            var headerRange = worksheet.Range("A1:G1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            var row = 2;
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

                var status = item.ReturnDate == null ? "Zimmetli" : "İade Alındı";
                worksheet.Cell(row, 7).Value = status;
                worksheet.Cell(row, 7).Style.Font.FontColor = item.ReturnDate == null ? XLColor.OrangeRed : XLColor.Green;

                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Zimmet_{DateTime.Now:ddMMyyyy_HHmm}.xlsx");
        }
    }
}
