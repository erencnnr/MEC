using MEC.Application.Abstractions.Service.OvertimeService;
using MEC.Application.Abstractions.Service.OvertimeService.Model;
using MEC.Domain.Common.Enum;
using MEC.Portal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace MEC.Portal.Controllers
{
    [Authorize(Roles = "Admin,Manager,FinalApprover")]
    public class OvertimeAdminController : Controller
    {
        private const int OvertimeRequestsPageSize = 10;
        private const string UpdateOvertimeStatusMethodName = "UpdateOvertimeStatus";

        private readonly IOvertimeService _overtimeService;

        public OvertimeAdminController(IOvertimeService overtimeService)
        {
            _overtimeService = overtimeService;
        }

        [HttpGet("/Admin/OvertimeRequests")]
        public async Task<IActionResult> Index(int page = 1, int? status = null)
        {
            var selectedStatus = NormalizeOvertimeStatusFilter(status);
            var result = await _overtimeService.GetAdminOvertimeRequestsAsync(new AdminOvertimeRequestListQueryModel
            {
                Page = page,
                PageSize = OvertimeRequestsPageSize,
                Status = selectedStatus,
                CurrentUserEmail = User.Identity?.Name ?? string.Empty
            });

            var model = new AdminOvertimeRequestListViewModel
            {
                Items = result.Items.Select(MapAdminItem).ToList(),
                StatusOptions = CreateStatusOptions(),
                SelectedStatus = selectedStatus,
                CurrentPage = result.CurrentPage,
                TotalPages = result.TotalPages,
                TotalCount = result.TotalCount,
                PageSize = result.PageSize
            };

            return View("~/Views/Admin/OvertimeRequests.cshtml", model);
        }

        [HttpGet("/Admin/OvertimeRequests/{id:int}")]
        public async Task<IActionResult> Detail(int id)
        {
            var item = await _overtimeService.GetAdminOvertimeRequestDetailAsync(id, User.Identity?.Name ?? string.Empty);
            if (item == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var model = new AdminOvertimeRequestDetailViewModel
            {
                Item = MapAdminItem(item)
            };

            return View("~/Views/Admin/OvertimeRequestDetail.cshtml", model);
        }

        [HttpGet("/Admin/OvertimeReport")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Report(int? employeeId = null, int? status = null, string? startDate = null, string? endDate = null)
        {
            var selectedStatus = NormalizeOvertimeStatusFilter(status);
            var result = await _overtimeService.GetAdminOvertimeReportAsync(new AdminOvertimeReportQueryModel
            {
                EmployeeId = employeeId,
                Status = selectedStatus,
                StartDate = startDate,
                EndDate = endDate
            });

            return View("~/Views/Admin/OvertimeReport.cshtml", MapReport(result));
        }

        [HttpGet("/Admin/OvertimeReport/Export")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportReport(int? employeeId = null, int? status = null, string? startDate = null, string? endDate = null)
        {
            var selectedStatus = NormalizeOvertimeStatusFilter(status);
            var export = await _overtimeService.ExportAdminOvertimeReportAsync(new AdminOvertimeReportQueryModel
            {
                EmployeeId = employeeId,
                Status = selectedStatus,
                StartDate = startDate,
                EndDate = endDate
            });

            return File(export.Content, export.ContentType, export.FileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOvertimeStatus(
            int id,
            int status,
            string? returnUrl = null,
            DateTime? updatedStartDate = null,
            DateTime? updatedEndDate = null,
            string? timeChangeNote = null)
        {
            var result = await _overtimeService.UpdateOvertimeStatusAsync(new OvertimeStatusUpdateRequestModel
            {
                OvertimeRequestId = id,
                Status = status,
                CurrentUser = User.Identity?.Name ?? "anonymous",
                DecisionBy = GetCurrentUserDisplayName(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                MethodName = UpdateOvertimeStatusMethodName,
                UpdatedStartDate = updatedStartDate,
                UpdatedEndDate = updatedEndDate,
                TimeChangeNote = timeChangeNote
            });

            if (result.IsSuccess)
            {
                TempData["AdminOvertimeStatusLevel"] = "success";
                TempData["AdminOvertimeStatusMessage"] = result.Message;

                return RedirectToReturnUrl(returnUrl);
            }

            TempData["AdminOvertimeStatusLevel"] = "error";
            TempData["AdminOvertimeStatusMessage"] = string.IsNullOrWhiteSpace(result.Message)
                ? "İşlem tamamlanamadı."
                : result.Message;

            return RedirectToReturnUrl(returnUrl);
        }

        private string GetCurrentUserDisplayName()
        {
            var givenName = User.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value;
            var surname = User.FindFirst(System.Security.Claims.ClaimTypes.Surname)?.Value;
            var fullName = string.Join(" ", new[] { givenName, surname }
                .Where(x => !string.IsNullOrWhiteSpace(x)))
                .Trim();

            return string.IsNullOrWhiteSpace(fullName)
                ? (User.Identity?.Name ?? "anonymous")
                : fullName;
        }

        private IActionResult RedirectToReturnUrl(string? returnUrl)
        {
            return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? LocalRedirect(returnUrl)
                : RedirectToAction(nameof(Index));
        }

        private static int? NormalizeOvertimeStatusFilter(int? status)
        {
            return status.HasValue && Enum.IsDefined(typeof(OvertimeStatus), status.Value)
                ? status
                : null;
        }

        private static List<OvertimeStatusFilterOptionViewModel> CreateStatusOptions()
        {
            return new List<OvertimeStatusFilterOptionViewModel>
            {
                new() { Value = (int)OvertimeStatus.Pending, Label = "Okul Müdürü Onayı Bekliyor" },
                new() { Value = (int)OvertimeStatus.PendingFinalApproval, Label = "Genel Müdürlük Onayı Bekliyor" },
                new() { Value = (int)OvertimeStatus.Approved, Label = "Onaylandı" },
                new() { Value = (int)OvertimeStatus.Rejected, Label = "Reddedildi" },
                new() { Value = (int)OvertimeStatus.Cancelled, Label = "İptal" }
            };
        }

        private static AdminOvertimeRequestViewModel MapAdminItem(AdminOvertimeRequestItemModel item)
        {
            return new AdminOvertimeRequestViewModel
            {
                Id = item.Id,
                EmployeeName = item.EmployeeName,
                StartDate = item.StartDate,
                EndDate = item.EndDate,
                RequestedHours = item.RequestedHours,
                Reason = item.Reason,
                Status = item.Status,
                CreatedDate = item.CreatedDate,
                StatusLabel = item.StatusLabel,
                StatusTone = item.StatusTone,
                DecisionDisplay = item.DecisionDisplay,
                LocationNames = item.LocationNames,
                ManagerDecisionDisplay = item.ManagerDecisionDisplay,
                CanTakeAction = item.CanTakeAction
            };
        }

        private static AdminOvertimeReportViewModel MapReport(AdminOvertimeReportResultModel result)
        {
            return new AdminOvertimeReportViewModel
            {
                Items = result.Items.Select(x => new AdminOvertimeReportItemViewModel
                {
                    Id = x.Id,
                    EmployeeName = x.EmployeeName,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    RequestedHours = x.RequestedHours,
                    StatusLabel = x.StatusLabel,
                    StatusTone = x.StatusTone,
                    CreatedDate = x.CreatedDate
                }).ToList(),
                EmployeeOptions = result.EmployeeOptions.Select(x => new AdminOvertimeReportFilterOptionViewModel
                {
                    Id = x.Id,
                    Label = x.Label
                }).ToList(),
                StatusOptions = result.StatusOptions.Select(x => new OvertimeStatusFilterOptionViewModel
                {
                    Value = x.Value,
                    Label = x.Label
                }).ToList(),
                SelectedEmployeeId = result.SelectedEmployeeId,
                SelectedStatus = result.SelectedStatus,
                StartDate = result.StartDate,
                EndDate = result.EndDate,
                TotalCount = result.TotalCount
            };
        }
    }
}
