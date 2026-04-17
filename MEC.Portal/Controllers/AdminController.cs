using MEC.Application.Abstractions.Common.Models;
using MEC.Application.Abstractions.Service.EmployeeService;
using MEC.Application.Abstractions.Service.EmployeeService.Model;
using MEC.Application.Abstractions.Service.LeaveService;
using MEC.Application.Abstractions.Service.LeaveService.Model;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Application.Abstractions.Service.SchoolService.Model;
using MEC.Domain.Common.Enum;
using MEC.Portal.Models;
using MEC.Portal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MEC.Portal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private const string UpdateLeaveStatusMethodName = "UpdateLeaveStatus";
        private const int LeaveRequestsPageSize = 10;
        private const int PortalUsersPageSize = 10;

        private readonly ILeaveService _leaveService;
        private readonly IAnnouncementService _announcementService;
        private readonly IEmployeePortalService _employeePortalService;
        private readonly ISliderService _sliderService;
        private readonly ISliderImageApiClient _sliderImageApiClient;
        private readonly IPortalUserSyncApiClient _portalUserSyncApiClient;

        public AdminController(
            ILeaveService leaveService,
            IAnnouncementService announcementService,
            IEmployeePortalService employeePortalService,
            ISliderService sliderService,
            ISliderImageApiClient sliderImageApiClient,
            IPortalUserSyncApiClient portalUserSyncApiClient)
        {
            _leaveService = leaveService;
            _announcementService = announcementService;
            _employeePortalService = employeePortalService;
            _sliderService = sliderService;
            _sliderImageApiClient = sliderImageApiClient;
            _portalUserSyncApiClient = portalUserSyncApiClient;
        }

        public async Task<IActionResult> Index()
        {
            var dashboard = await _leaveService.GetAdminDashboardAsync(User.Identity?.Name ?? "Admin");

            var model = new AdminDashboardViewModel
            {
                AdminName = dashboard.AdminName,
                GeneratedAt = dashboard.GeneratedAt,
                PendingLeaveCount = dashboard.PendingLeaveCount,
                TotalAnnouncementCount = dashboard.TotalAnnouncementCount,
                TodayAnnouncementCount = dashboard.TodayAnnouncementCount,
                NegativeLeaveBalanceCount = dashboard.NegativeLeaveBalanceCount,
                RecentLeaveRequests = dashboard.RecentLeaveRequests
                    .Take(5)
                    .Select(x => new AdminRecentLeaveItemViewModel
                    {
                        EmployeeName = x.EmployeeName,
                        LeaveType = x.LeaveType,
                        RequestedDays = x.RequestedDays,
                        Status = x.Status,
                        StartDate = x.StartDate,
                        EndDate = x.EndDate,
                        CreatedDate = x.CreatedDate
                    })
                    .ToList(),
                RecentAnnouncements = dashboard.RecentAnnouncements
                    .Select(x => new AdminRecentAnnouncementItemViewModel
                    {
                        Title = x.Title,
                        IsActive = x.IsActive,
                        CreatedDate = x.CreatedDate
                    })
                    .ToList()
            };

            return View(model);
        }

        public IActionResult Announcements()
        {
            return RedirectToAction("Index", "Announcement");
        }

        [HttpGet("/Admin/LeaveRequests")]
        public async Task<IActionResult> LeaveRequests(int page = 1, int? status = null)
        {
            var selectedStatus = NormalizeLeaveStatusFilter(status);
            var result = await _leaveService.GetAdminLeaveRequestsAsync(new AdminLeaveRequestListQueryModel
            {
                Page = page,
                PageSize = LeaveRequestsPageSize,
                Status = selectedStatus
            });

            var model = new AdminLeaveRequestListViewModel
            {
                Items = result.Items.Select(MapLeaveRequestItem).ToList(),
                StatusOptions = CreateLeaveStatusOptions(),
                SelectedStatus = selectedStatus,
                CurrentPage = result.CurrentPage,
                TotalPages = result.TotalPages,
                TotalCount = result.TotalCount,
                PageSize = result.PageSize
            };

            return View(model);
        }

        [HttpGet("/Admin/LeaveRequests/{id:int}")]
        public async Task<IActionResult> LeaveRequestDetail(int id)
        {
            var item = await _leaveService.GetAdminLeaveRequestDetailAsync(id);
            if (item == null)
            {
                return RedirectToAction(nameof(LeaveRequests));
            }

            var model = new AdminLeaveRequestDetailViewModel
            {
                Item = MapLeaveRequestItem(item)
            };

            return View(model);
        }

        [HttpGet("/Admin/LeaveReport")]
        public async Task<IActionResult> LeaveReport(int? employeeId = null, int? leaveTypeId = null, string? startDate = null, string? endDate = null)
        {
            var result = await _leaveService.GetAdminLeaveReportAsync(new AdminLeaveReportQueryModel
            {
                EmployeeId = employeeId,
                LeaveTypeId = leaveTypeId,
                StartDate = startDate,
                EndDate = endDate
            });

            return View(MapLeaveReport(result));
        }

        [HttpGet("/Admin/PortalUsers")]
        public async Task<IActionResult> PortalUsers(string? status = "active", int page = 1)
        {
            var result = await _employeePortalService.GetPortalUsersAsync(new PortalUserListQueryModel
            {
                Status = status,
                Page = page,
                PageSize = PortalUsersPageSize
            });

            var model = new AdminPortalUserListViewModel
            {
                Status = result.Status,
                CurrentPage = result.CurrentPage,
                TotalPages = result.TotalPages,
                TotalCount = result.TotalCount,
                PageSize = result.PageSize,
                Items = result.Items.Select(MapPortalUserListItem).ToList()
            };

            return View(model);
        }

        [HttpPost("/Admin/PortalUsers/Sync")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncPortalUsers(CancellationToken cancellationToken)
        {
            var result = await _portalUserSyncApiClient.SyncPortalUsersAsync(cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(new
                {
                    success = true,
                    level = "success",
                    message = "Kullanıcılar başarıyla senkronize edildi.",
                    processedUsers = result.ProcessedUsers
                });
            }

            return StatusCode(500, new
            {
                success = false,
                level = "danger",
                message = "Hata! Senkronizasyon başarısız.",
                detail = result.Message
            });
        }

        [HttpGet("/Admin/PortalUsers/{id:int}")]
        public async Task<IActionResult> PortalUserDetail(int id)
        {
            var portalUser = await _employeePortalService.GetPortalUserEditAsync(id);
            if (portalUser == null)
            {
                return RedirectToAction(nameof(PortalUsers));
            }

            return View(MapPortalUserEditModel(portalUser));
        }

        [HttpPost("/Admin/PortalUsers/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PortalUserDetail(int id, AdminPortalUserEditViewModel model)
        {
            if (id != model.Id)
            {
                model.Id = id;
            }

            var existingPortalUser = await _employeePortalService.GetPortalUserEditAsync(id);
            if (existingPortalUser == null)
            {
                return RedirectToAction(nameof(PortalUsers));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _employeePortalService.UpdatePortalUserAsync(new PortalUserEditModel
            {
                Id = model.Id,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                HireDate = model.HireDate,
                BirthDate = model.BirthDate,
                LeaveDays = model.LeaveDays,
                IsAdmin = model.IsAdmin,
                IsDeleted = model.IsDeleted
            });

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            TempData["PortalUserSuccess"] = result.Message;
            return RedirectToAction(nameof(PortalUserDetail), new { id });
        }

        [HttpGet("/Admin/Slider")]
        public async Task<IActionResult> Slider()
        {
            var model = await BuildSliderViewModelAsync();
            return View(model);
        }

        [HttpPost("/Admin/Slider/Upload")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadSliderImages(List<IFormFile>? files)
        {
            if (files == null || files.Count == 0)
            {
                TempData["SliderError"] = "YÃ¼klenecek en az bir gÃ¶rsel seÃ§in.";
                return RedirectToAction(nameof(Slider));
            }

            var uploadedCount = 0;
            var failedMessages = new List<string>();

            foreach (var file in files.Where(x => x != null && x.Length > 0))
            {
                if (!IsAllowedSliderImage(file))
                {
                    failedMessages.Add($"{file.FileName} desteklenmeyen bir dosya tÃ¼rÃ¼.");
                    continue;
                }

                var uploadResult = await _sliderImageApiClient.UploadAsync(file);
                if (!uploadResult.IsSuccess)
                {
                    failedMessages.Add($"{file.FileName} yÃ¼klenemedi: {uploadResult.Message}");
                    continue;
                }

                await _sliderService.AddSliderImageAsync(new SliderImageCreateModel
                {
                    FileName = uploadResult.FileName,
                    OriginalFileName = file.FileName,
                    RelativePath = uploadResult.RelativePath,
                    ContentType = string.IsNullOrWhiteSpace(uploadResult.ContentType) ? file.ContentType ?? string.Empty : uploadResult.ContentType,
                    SizeBytes = file.Length
                });

                uploadedCount++;
            }

            if (uploadedCount > 0)
            {
                TempData["SliderSuccess"] = uploadedCount == 1
                    ? "Slider gÃ¶rseli yÃ¼klendi."
                    : $"{uploadedCount} slider gÃ¶rseli yÃ¼klendi.";
            }

            if (failedMessages.Count > 0)
            {
                TempData["SliderError"] = string.Join(" ", failedMessages);
            }

            return RedirectToAction(nameof(Slider));
        }

        [HttpPost("/Admin/Slider/Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSliderImage(int id)
        {
            var item = await _sliderService.GetSliderImageAsync(id);
            if (item == null)
            {
                TempData["SliderError"] = "Silinecek slider gÃ¶rseli bulunamadÄ±.";
                return RedirectToAction(nameof(Slider));
            }

            var deleteResult = await _sliderImageApiClient.DeleteAsync(item.FileName);
            if (!deleteResult.IsSuccess)
            {
                TempData["SliderError"] = string.IsNullOrWhiteSpace(deleteResult.Message)
                    ? "Slider gÃ¶rseli silinemedi."
                    : deleteResult.Message;
                return RedirectToAction(nameof(Slider));
            }

            await _sliderService.DeleteSliderImageMetadataAsync(id);

            TempData["SliderSuccess"] = "Slider gÃ¶rseli silindi.";
            return RedirectToAction(nameof(Slider));
        }

        [HttpPost("/Admin/Slider/Reorder")]
        public async Task<IActionResult> ReorderSlider([FromBody] SliderReorderRequest? request)
        {
            if (request?.OrderedIds == null || request.OrderedIds.Count == 0)
            {
                return BadRequest(new { message = "GeÃ§erli bir slider sÄ±rasÄ± gÃ¶nderilmedi." });
            }

            var result = await _sliderService.ReorderSliderAsync(new SliderReorderModel
            {
                OrderedIds = request.OrderedIds
            });

            return result.IsSuccess
                ? Ok(new { message = result.Message })
                : BadRequest(new { message = result.Message });
        }

        [HttpGet("/Admin/LeaveReport/Export")]
        public async Task<IActionResult> ExportLeaveReport(int? employeeId = null, int? leaveTypeId = null, string? startDate = null, string? endDate = null)
        {
            var export = await _leaveService.ExportAdminLeaveReportAsync(new AdminLeaveReportQueryModel
            {
                EmployeeId = employeeId,
                LeaveTypeId = leaveTypeId,
                StartDate = startDate,
                EndDate = endDate
            });

            return File(
                export.Content,
                export.ContentType,
                export.FileName);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateLeaveStatus(int id, int status, string? returnUrl = null)
        {
            var result = await _leaveService.UpdateLeaveStatusWithLogAsync(new LeaveStatusUpdateRequestModel
            {
                LeaveId = id,
                Status = status,
                CurrentUser = User.Identity?.Name ?? "anonymous",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                MethodName = UpdateLeaveStatusMethodName
            });

            if (result.IsSuccess)
            {
                TempData["AdminLeaveStatusLevel"] = "success";
                TempData["AdminLeaveStatusMessage"] = status == (int)LeaveStatus.Approved
                    ? "İzin talebi onaylandı."
                    : status == (int)LeaveStatus.Rejected
                        ? "İzin talebi reddedildi."
                        : result.Message;

                return RedirectToLeaveReturnUrl(returnUrl);
            }

            TempData["AdminLeaveStatusLevel"] = "error";
            TempData["AdminLeaveStatusMessage"] = string.IsNullOrWhiteSpace(result.Message)
                ? "İşlem tamamlanamadı."
                : result.Message;

            return RedirectToLeaveReturnUrl(returnUrl);
        }

        private IActionResult RedirectToLeaveReturnUrl(string? returnUrl)
        {
            return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? LocalRedirect(returnUrl)
                : RedirectToAction(nameof(LeaveRequests));
        }

        private static int? NormalizeLeaveStatusFilter(int? status)
        {
            return status.HasValue && Enum.IsDefined(typeof(LeaveStatus), status.Value)
                ? status.Value
                : null;
        }

        private static List<AdminLeaveStatusFilterOptionViewModel> CreateLeaveStatusOptions()
        {
            return new List<AdminLeaveStatusFilterOptionViewModel>
            {
                new() { Value = (int)LeaveStatus.Pending, Label = "Onay Bekliyor" },
                new() { Value = (int)LeaveStatus.Approved, Label = "Onaylandı" },
                new() { Value = (int)LeaveStatus.Rejected, Label = "Reddedildi" },
                new() { Value = (int)LeaveStatus.Cancelled, Label = "İptal" }
            };
        }

        private static AdminLeaveRequestViewModel MapLeaveRequestItem(AdminLeaveRequestItemModel item)
        {
            return new AdminLeaveRequestViewModel
            {
                Id = item.Id,
                EmployeeId = item.EmployeeId,
                LeaveTypeId = item.LeaveTypeId,
                EmployeeName = item.EmployeeName,
                LeaveType = item.LeaveType,
                RequestedDays = item.RequestedDays,
                StartDate = item.StartDate,
                EndDate = item.EndDate,
                Reason = item.Reason,
                Status = item.Status,
                RemainingLeaveDays = item.RemainingLeaveDays,
                CreatedDate = item.CreatedDate,
                StatusLabel = item.StatusLabel,
                StatusTone = item.StatusTone,
                DecisionDisplay = item.DecisionDisplay,
                CanTakeAction = item.CanTakeAction
            };
        }

        private static AdminLeaveReportViewModel MapLeaveReport(AdminLeaveReportResultModel result)
        {
            return new AdminLeaveReportViewModel
            {
                Items = result.Items.Select(x => new AdminLeaveReportItemViewModel
                {
                    Id = x.Id,
                    EmployeeName = x.EmployeeName,
                    LeaveType = x.LeaveType,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    RequestedDays = x.RequestedDays,
                    StatusLabel = x.StatusLabel,
                    StatusTone = x.StatusTone,
                    CreatedDate = x.CreatedDate
                }).ToList(),
                EmployeeOptions = result.EmployeeOptions.Select(x => new AdminLeaveReportFilterOptionViewModel
                {
                    Id = x.Id,
                    Label = x.Label
                }).ToList(),
                LeaveTypeOptions = result.LeaveTypeOptions.Select(x => new AdminLeaveReportFilterOptionViewModel
                {
                    Id = x.Id,
                    Label = x.Label
                }).ToList(),
                SelectedEmployeeId = result.SelectedEmployeeId,
                SelectedLeaveTypeId = result.SelectedLeaveTypeId,
                StartDate = result.StartDate,
                EndDate = result.EndDate,
                TotalCount = result.TotalCount
            };
        }

        private async Task<AdminSliderViewModel> BuildSliderViewModelAsync()
        {
            var items = (await _sliderService.GetSliderImagesAsync())
                .Select(MapSliderItem)
                .ToList();

            return new AdminSliderViewModel
            {
                Items = items
            };
        }

        private static AdminPortalUserListItemViewModel MapPortalUserListItem(PortalUserListItemModel portalUser)
        {
            return new AdminPortalUserListItemViewModel
            {
                Id = portalUser.Id,
                FullName = portalUser.FullName,
                Email = portalUser.Email,
                PhoneNumber = portalUser.PhoneNumber,
                LeaveDays = portalUser.LeaveDays,
                HireDate = portalUser.HireDate,
                IsAdmin = portalUser.IsAdmin,
                IsDeleted = portalUser.IsDeleted
            };
        }

        private static AdminPortalUserEditViewModel MapPortalUserEditModel(PortalUserEditModel portalUser)
        {
            return new AdminPortalUserEditViewModel
            {
                Id = portalUser.Id,
                FirstName = portalUser.FirstName,
                LastName = portalUser.LastName,
                Email = portalUser.Email,
                PhoneNumber = portalUser.PhoneNumber,
                HireDate = portalUser.HireDate,
                BirthDate = portalUser.BirthDate,
                LeaveDays = portalUser.LeaveDays,
                IsAdmin = portalUser.IsAdmin,
                IsDeleted = portalUser.IsDeleted
            };
        }

        private AdminSliderItemViewModel MapSliderItem(SliderImageModel image)
        {
            return new AdminSliderItemViewModel
            {
                Id = image.Id,
                FileName = image.FileName,
                OriginalFileName = image.OriginalFileName,
                DisplayOrder = image.DisplayOrder,
                CreatedDate = image.CreatedDate,
                ImageUrl = _sliderImageApiClient.GetFileUrl(image.FileName)
            };
        }

        private static bool IsAllowedSliderImage(IFormFile file)
        {
            var extension = System.IO.Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                return false;
            }

            return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".gif", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
        }
    }
}

