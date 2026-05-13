using ClosedXML.Excel;
using MEC.Application.Abstractions.Service.EmployeeService;
using MEC.Application.Abstractions.Service.LoanService;
using MEC.Application.Abstractions.Service.LoanService.Model;
using MEC.Domain.Common;
using MEC.Portal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MEC.Portal.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/Loans")]
    public class LoansController : Controller
    {
        private readonly ILoanService _loanService;
        private readonly ILoanStatusService _loanStatusService;
        private readonly IEmployeePortalService _employeePortalService;

        public LoansController(
            ILoanService loanService,
            ILoanStatusService loanStatusService,
            IEmployeePortalService employeePortalService)
        {
            _loanService = loanService;
            _loanStatusService = loanStatusService;
            _employeePortalService = employeePortalService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index([FromQuery] LoanFilterRequestModel request)
        {
            request.ActiveTab = string.IsNullOrWhiteSpace(request.ActiveTab) ? "active" : request.ActiveTab;
            request.SortOrder = string.IsNullOrWhiteSpace(request.SortOrder) ? "LoanDate_Desc" : request.SortOrder;
            request.Page = request.Page < 1 ? 1 : request.Page;
            request.PageSize = request.PageSize <= 0 ? 20 : request.PageSize;

            var pagedLoans = await _loanService.GetPagedLoanListAsync(request);
            var employees = await _employeePortalService.GetActivePortalUsersAsync();
            var employeeNames = employees.ToDictionary(
                user => user.Id,
                user => BuildEmployeeName(user.FirstName, user.LastName, user.Email));

            var model = new LoanIndexViewModel
            {
                Filters = request,
                EmployeeOptions = employees
                    .Select(user => new SelectListItem
                    {
                        Value = user.Id.ToString(),
                        Text = BuildEmployeeName(user.FirstName, user.LastName, user.Email)
                    })
                    .OrderBy(item => item.Text)
                    .ToList(),
                AssignedByOptions = employees
                    .Select(user => new SelectListItem
                    {
                        Value = user.Id.ToString(),
                        Text = BuildEmployeeName(user.FirstName, user.LastName, user.Email)
                    })
                    .OrderBy(item => item.Text)
                    .ToList(),
                Loans = new PagedResult<LoanListItemViewModel>
                {
                    CurrentPage = pagedLoans.CurrentPage,
                    PageCount = pagedLoans.PageCount,
                    PageSize = pagedLoans.PageSize,
                    RowCount = pagedLoans.RowCount,
                    Results = pagedLoans.Results.Select(loan => new LoanListItemViewModel
                    {
                        Id = loan.Id,
                        AssetId = loan.AssetId,
                        AssetName = string.IsNullOrWhiteSpace(loan.Asset?.Name) ? "-" : loan.Asset.Name,
                        SerialNumber = string.IsNullOrWhiteSpace(loan.Asset?.SerialNumber) ? "-" : loan.Asset.SerialNumber,
                        AssignedToName = loan.AssignedTo != null
                            ? BuildEmployeeName(loan.AssignedTo.FirstName, loan.AssignedTo.LastName, loan.AssignedTo.Email)
                            : "-",
                        AssignedByName = loan.AssignedById.HasValue && employeeNames.TryGetValue(loan.AssignedById.Value, out var assignedBy)
                            ? assignedBy
                            : "-",
                        LoanDate = loan.LoanDate.ToString("dd.MM.yyyy"),
                        ReturnDate = loan.ReturnDate?.ToString("dd.MM.yyyy")
                    }).ToList()
                }
            };

            return View(model);
        }

        [HttpPost("BulkReturn")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkReturn(List<int> selectedLoanIds, string? returnUrl = null)
        {
            if (selectedLoanIds == null || selectedLoanIds.Count == 0)
            {
                TempData["Error"] = "Lütfen iade alınacak en az bir zimmet seçin.";
                return RedirectToLocal(returnUrl);
            }

            var loans = await _loanService.GetLoanListAsync();
            var returnStatus = await GetReturnedStatusAsync();
            var updatedCount = 0;

            foreach (var loan in loans.Where(item => selectedLoanIds.Contains(item.Id) && item.ReturnDate == null))
            {
                loan.ReturnDate = DateTime.Now.Date;
                if (returnStatus != null)
                {
                    loan.LoanStatusId = returnStatus.Id;
                }

                await _loanService.UpdateLoanAsync(loan);
                updatedCount++;
            }

            if (updatedCount == 0)
            {
                TempData["Error"] = "Seçilen kayıtlar için iade işlemi uygulanamadı.";
            }
            else if (updatedCount == 1)
            {
                TempData["Success"] = "Zimmet kaydı başarıyla iade alındı.";
            }
            else
            {
                TempData["Success"] = $"{updatedCount} zimmet kaydı başarıyla iade alındı.";
            }

            return RedirectToLocal(returnUrl);
        }

        [HttpGet("Export")]
        public async Task<IActionResult> Export([FromQuery] LoanFilterRequestModel request)
        {
            request.ActiveTab = string.IsNullOrWhiteSpace(request.ActiveTab) ? "active" : request.ActiveTab;
            var loans = await _loanService.GetLoanListAsync(request);
            var employees = await _employeePortalService.GetActivePortalUsersAsync();
            var employeeNames = employees.ToDictionary(
                user => user.Id,
                user => BuildEmployeeName(user.FirstName, user.LastName, user.Email));

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(
                string.Equals(request.ActiveTab, "history", StringComparison.OrdinalIgnoreCase)
                    ? "Zimmet Gecmisi"
                    : "Aktif Zimmetler");

            worksheet.Cell(1, 1).Value = "Envanter";
            worksheet.Cell(1, 2).Value = "Seri No";
            worksheet.Cell(1, 3).Value = "Zimmet Alan";
            worksheet.Cell(1, 4).Value = "Zimmet Veren";
            worksheet.Cell(1, 5).Value = "Veriliş Tarihi";
            worksheet.Cell(1, 6).Value = "İade Tarihi";
            worksheet.Cell(1, 7).Value = "Durum";

            var headerRange = worksheet.Range("A1:G1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            var row = 2;
            foreach (var loan in loans)
            {
                worksheet.Cell(row, 1).Value = loan.Asset?.Name ?? "-";
                worksheet.Cell(row, 2).Value = loan.Asset?.SerialNumber ?? "-";
                worksheet.Cell(row, 3).Value = loan.AssignedTo != null
                    ? BuildEmployeeName(loan.AssignedTo.FirstName, loan.AssignedTo.LastName, loan.AssignedTo.Email)
                    : "-";
                worksheet.Cell(row, 4).Value = loan.AssignedById.HasValue && employeeNames.TryGetValue(loan.AssignedById.Value, out var assignedBy)
                    ? assignedBy
                    : "-";
                worksheet.Cell(row, 5).Value = loan.LoanDate;
                worksheet.Cell(row, 5).Style.DateFormat.Format = "dd.MM.yyyy";

                if (loan.ReturnDate.HasValue)
                {
                    worksheet.Cell(row, 6).Value = loan.ReturnDate.Value;
                    worksheet.Cell(row, 6).Style.DateFormat.Format = "dd.MM.yyyy";
                }
                else
                {
                    worksheet.Cell(row, 6).Value = "-";
                }

                var isActive = loan.ReturnDate == null;
                worksheet.Cell(row, 7).Value = isActive ? "Zimmetli" : "İade Alındı";
                worksheet.Cell(row, 7).Style.Font.FontColor = isActive ? XLColor.OrangeRed : XLColor.Green;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileName = string.Equals(request.ActiveTab, "history", StringComparison.OrdinalIgnoreCase)
                ? $"Zimmet_Gecmisi_{DateTime.Now:ddMMyyyy_HHmm}.xlsx"
                : $"Aktif_Zimmetler_{DateTime.Now:ddMMyyyy_HHmm}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        private async Task<MEC.Domain.Entity.Loan.LoanStatus?> GetReturnedStatusAsync()
        {
            var statuses = await _loanStatusService.GetLoanStatusListAsync();
            return statuses.FirstOrDefault(status =>
                string.Equals(status.Name, "İade Alındı", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status.Name, "Iade Alındı", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status.Name, "Iade Alindi", StringComparison.OrdinalIgnoreCase));
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        private static string BuildEmployeeName(string? firstName, string? lastName, string? email)
        {
            var fullName = string.Join(" ", new[] { firstName, lastName }
                .Where(value => !string.IsNullOrWhiteSpace(value)))
                .Trim();

            return string.IsNullOrWhiteSpace(fullName) ? (email ?? "-") : fullName;
        }
    }
}
