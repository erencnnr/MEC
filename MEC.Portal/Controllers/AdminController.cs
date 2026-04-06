using ClosedXML.Excel;
using MEC.Application.Abstractions.Service.LeaveService;
using MEC.Application.Abstractions.Service.LoggingService;
using MEC.Application.Abstractions.Service.LoggingService.Model;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Common;
using MEC.Domain.Common.Enum;
using MEC.Domain.Entity.Employee;
using MEC.Domain.Entity.School;
using MEC.Portal.Models;
using MEC.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LeaveEntity = MEC.Domain.Entity.Leave.Leave;

namespace MEC.AssetManagementUI.Controllers
{
    public class AdminController : Controller
    {
        private const string UpdateLeaveStatusMethodName = "UpdateLeaveStatus";
        private const int LeaveRequestsPageSize = 10;
        private const int PortalUsersPageSize = 10;

        private readonly ILeaveService _leaveService;
        private readonly IAnnouncementService _announcementService;
        private readonly IGenericRepository<Employee> _employeeRepository;
        private readonly IGenericRepository<EmployeePortal> _employeePortalRepository;
        private readonly IGenericRepository<LeaveEntity> _leaveRepository;
        private readonly IGenericRepository<SliderImage> _sliderImageRepository;
        private readonly IUserActionLogService _userActionLogService;
        private readonly ISliderImageApiClient _sliderImageApiClient;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ILeaveService leaveService,
            IAnnouncementService announcementService,
            IGenericRepository<Employee> employeeRepository,
            IGenericRepository<EmployeePortal> employeePortalRepository,
            IGenericRepository<LeaveEntity> leaveRepository,
            IGenericRepository<SliderImage> sliderImageRepository,
            IUserActionLogService userActionLogService,
            ISliderImageApiClient sliderImageApiClient,
            ILogger<AdminController> logger)
        {
            _leaveService = leaveService;
            _announcementService = announcementService;
            _employeeRepository = employeeRepository;
            _employeePortalRepository = employeePortalRepository;
            _leaveRepository = leaveRepository;
            _sliderImageRepository = sliderImageRepository;
            _userActionLogService = userActionLogService;
            _sliderImageApiClient = sliderImageApiClient;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var leaves = await _leaveService.GetAllLeavesAsync();
            var announcements = (await _announcementService.GetAllAnnouncementsAsync())
                .OrderByDescending(x => x.CreatedDate)
                .ToList();
            var employees = await _employeeRepository.GetAllAsync(x => !x.IsDeleted);
            var employeePortals = await _employeePortalRepository.GetAllAsync();
            var today = DateTime.Today;

            var employeeNames = employees.ToDictionary(
                x => x.Id,
                x => string.Join(" ", new[] { x.FirstName, x.LastName }.Where(y => !string.IsNullOrWhiteSpace(y))).Trim());

            var model = new AdminDashboardViewModel
            {
                AdminName = User.Identity?.Name ?? "Admin",
                GeneratedAt = DateTime.Now,
                PendingLeaveCount = leaves.Count(x => x.Status == (int)LeaveStatus.Pending),
                TotalAnnouncementCount = announcements.Count,
                TodayAnnouncementCount = announcements.Count(x => x.CreatedDate.HasValue && x.CreatedDate.Value.Date == today),
                NegativeLeaveBalanceCount = employeePortals.Count(x => x.LeaveDays < 0),
                RecentLeaveRequests = leaves
                    .Take(5)
                    .Select(x => new AdminRecentLeaveItemViewModel
                    {
                        EmployeeName = employeeNames.TryGetValue(x.EmployeeId, out var employeeName) && !string.IsNullOrWhiteSpace(employeeName)
                            ? employeeName
                            : $"#{x.EmployeeId}",
                        LeaveType = GetLeaveTypeName(x),
                        RequestedDays = GetRequestedDays(x),
                        Status = x.Status,
                        StartDate = x.StartDate,
                        EndDate = x.EndDate,
                        CreatedDate = x.CreatedDate
                    })
                    .ToList(),
                RecentAnnouncements = announcements
                    .Take(5)
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
        public async Task<IActionResult> LeaveRequests(int page = 1)
        {
            var currentPage = page < 1 ? 1 : page;
            var leaves = await _leaveService.GetAllLeavesAsync();
            var employees = await _employeeRepository.GetAllAsync(x => !x.IsDeleted);
            var employeeNames = employees.ToDictionary(
                x => x.Id,
                x => string.Join(" ", new[] { x.FirstName, x.LastName }.Where(y => !string.IsNullOrWhiteSpace(y))).Trim());

            var mappedItems = leaves
                .Select(x => MapLeaveRequestItem(x, employeeNames))
                .ToList();

            var totalCount = mappedItems.Count;
            var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)LeaveRequestsPageSize);
            currentPage = Math.Min(currentPage, totalPages);

            var model = new AdminLeaveRequestListViewModel
            {
                Items = mappedItems.Skip((currentPage - 1) * LeaveRequestsPageSize).Take(LeaveRequestsPageSize).ToList(),
                CurrentPage = currentPage,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = LeaveRequestsPageSize
            };

            return View(model);
        }

        [HttpGet("/Admin/LeaveRequests/{id:int}")]
        public async Task<IActionResult> LeaveRequestDetail(int id)
        {
            var leave = (await _leaveRepository.GetAllAsync(x => x.Id == id, x => x.LeaveType)).FirstOrDefault();
            if (leave == null)
            {
                return RedirectToAction(nameof(LeaveRequests));
            }

            var employee = await _employeeRepository.GetByIdAsync(leave.EmployeeId);
            var model = new AdminLeaveRequestDetailViewModel
            {
                Item = MapLeaveRequestItem(
                    leave,
                    new Dictionary<int, string> { [leave.EmployeeId] = BuildEmployeeName(employee, leave.EmployeeId) })
            };

            return View(model);
        }

        [HttpGet("/Admin/LeaveReport")]
        public async Task<IActionResult> LeaveReport(int? employeeId = null, int? leaveTypeId = null, string? startDate = null, string? endDate = null)
        {
            var model = await BuildLeaveReportViewModelAsync(employeeId, leaveTypeId, startDate, endDate);
            return View(model);
        }

        [HttpGet("/Admin/PortalUsers")]
        public async Task<IActionResult> PortalUsers(string? status = "active", int page = 1)
        {
            var normalizedStatus = NormalizePortalUserStatus(status);
            var currentPage = page < 1 ? 1 : page;
            var portalUsers = (await _employeePortalRepository.GetAllAsync())
                .Where(x => normalizedStatus == "passive" ? x.IsDeleted : !x.IsDeleted)
                .OrderBy(x => x.FirstName)
                .ThenBy(x => x.LastName)
                .ThenBy(x => x.Email)
                .ToList();

            var totalCount = portalUsers.Count;
            var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)PortalUsersPageSize);
            currentPage = Math.Min(currentPage, totalPages);

            var model = new AdminPortalUserListViewModel
            {
                Status = normalizedStatus,
                CurrentPage = currentPage,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = PortalUsersPageSize,
                Items = portalUsers
                    .Skip((currentPage - 1) * PortalUsersPageSize)
                    .Take(PortalUsersPageSize)
                    .Select(MapPortalUserListItem)
                    .ToList()
            };

            return View(model);
        }

        [HttpGet("/Admin/PortalUsers/{id:int}")]
        public async Task<IActionResult> PortalUserDetail(int id)
        {
            var portalUser = await _employeePortalRepository.GetByIdAsync(id);
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

            var portalUser = await _employeePortalRepository.GetByIdAsync(id);
            if (portalUser == null)
            {
                return RedirectToAction(nameof(PortalUsers));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            portalUser.FirstName = model.FirstName.Trim();
            portalUser.LastName = model.LastName.Trim();
            portalUser.Email = model.Email.Trim();
            portalUser.PhoneNumber = model.PhoneNumber.Trim();
            portalUser.HireDate = model.HireDate;
            portalUser.BirthDate = model.BirthDate;
            portalUser.LeaveDays = model.LeaveDays;
            portalUser.IsDeleted = model.IsDeleted;
            portalUser.UpdateDate = DateTime.Now;

            _employeePortalRepository.Update(portalUser);

            TempData["PortalUserSuccess"] = "Portal kullanıcısı güncellendi.";
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
                TempData["SliderError"] = "Yüklenecek en az bir görsel seçin.";
                return RedirectToAction(nameof(Slider));
            }

            var existingItems = (await _sliderImageRepository.GetAllAsync())
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.CreatedDate)
                .ToList();

            var nextDisplayOrder = existingItems.Count == 0 ? 1 : existingItems.Max(x => x.DisplayOrder) + 1;
            var uploadedCount = 0;
            var failedMessages = new List<string>();

            foreach (var file in files.Where(x => x != null && x.Length > 0))
            {
                if (!IsAllowedSliderImage(file))
                {
                    failedMessages.Add($"{file.FileName} desteklenmeyen bir dosya türü.");
                    continue;
                }

                var uploadResult = await _sliderImageApiClient.UploadAsync(file);
                if (!uploadResult.IsSuccess)
                {
                    failedMessages.Add($"{file.FileName} yüklenemedi: {uploadResult.Message}");
                    continue;
                }

                await _sliderImageRepository.AddAsync(new SliderImage
                {
                    FileName = uploadResult.FileName,
                    OriginalFileName = file.FileName,
                    RelativePath = uploadResult.RelativePath,
                    ContentType = string.IsNullOrWhiteSpace(uploadResult.ContentType) ? file.ContentType ?? string.Empty : uploadResult.ContentType,
                    SizeBytes = file.Length,
                    DisplayOrder = nextDisplayOrder++,
                    CreatedDate = DateTime.Now,
                    UpdateDate = DateTime.Now
                });

                uploadedCount++;
            }

            if (uploadedCount > 0)
            {
                TempData["SliderSuccess"] = uploadedCount == 1
                    ? "Slider görseli yüklendi."
                    : $"{uploadedCount} slider görseli yüklendi.";
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
            var item = await _sliderImageRepository.GetByIdAsync(id);
            if (item == null)
            {
                TempData["SliderError"] = "Silinecek slider görseli bulunamadı.";
                return RedirectToAction(nameof(Slider));
            }

            var deleteResult = await _sliderImageApiClient.DeleteAsync(item.FileName);
            if (!deleteResult.IsSuccess)
            {
                TempData["SliderError"] = string.IsNullOrWhiteSpace(deleteResult.Message)
                    ? "Slider görseli silinemedi."
                    : deleteResult.Message;
                return RedirectToAction(nameof(Slider));
            }

            _sliderImageRepository.Delete(item);
            await NormalizeSliderOrderAsync();

            TempData["SliderSuccess"] = "Slider görseli silindi.";
            return RedirectToAction(nameof(Slider));
        }

        [HttpPost("/Admin/Slider/Reorder")]
        public async Task<IActionResult> ReorderSlider([FromBody] SliderReorderRequest? request)
        {
            if (request?.OrderedIds == null || request.OrderedIds.Count == 0)
            {
                return BadRequest(new { message = "Geçerli bir slider sırası gönderilmedi." });
            }

            var items = (await _sliderImageRepository.GetAllAsync())
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.CreatedDate)
                .ToList();

            var itemById = items.ToDictionary(x => x.Id);
            var normalizedIds = request.OrderedIds.Where(itemById.ContainsKey).Distinct().ToList();
            if (normalizedIds.Count != items.Count)
            {
                return BadRequest(new { message = "Slider sırası eksik veya hatalı." });
            }

            for (var index = 0; index < normalizedIds.Count; index++)
            {
                var item = itemById[normalizedIds[index]];
                item.DisplayOrder = index + 1;
                item.UpdateDate = DateTime.Now;
                _sliderImageRepository.Update(item);
            }

            return Ok(new { message = "Slider sırası güncellendi." });
        }

        [HttpGet("/Admin/LeaveReport/Export")]
        public async Task<IActionResult> ExportLeaveReport(int? employeeId = null, int? leaveTypeId = null, string? startDate = null, string? endDate = null)
        {
            var model = await BuildLeaveReportViewModelAsync(employeeId, leaveTypeId, startDate, endDate);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Izin Raporu");

            worksheet.Cell(1, 1).Value = "Personel";
            worksheet.Cell(1, 2).Value = "İzin Türü";
            worksheet.Cell(1, 3).Value = "Başlangıç";
            worksheet.Cell(1, 4).Value = "Bitiş";
            worksheet.Cell(1, 5).Value = "Kullanılan Gün";
            worksheet.Cell(1, 6).Value = "Durum";
            worksheet.Cell(1, 7).Value = "Oluşturma Tarihi";

            var headerRange = worksheet.Range(1, 1, 1, 7);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#e4ebef");
            headerRange.Style.Font.FontColor = XLColor.FromHtml("#18285c");

            for (var index = 0; index < model.Items.Count; index++)
            {
                var item = model.Items[index];
                var row = index + 2;

                worksheet.Cell(row, 1).Value = item.EmployeeName;
                worksheet.Cell(row, 2).Value = item.LeaveType;
                worksheet.Cell(row, 3).Value = item.StartDate.ToString("dd.MM.yyyy HH:mm");
                worksheet.Cell(row, 4).Value = item.EndDate.ToString("dd.MM.yyyy HH:mm");
                worksheet.Cell(row, 5).Value = item.RequestedDays;
                worksheet.Cell(row, 6).Value = item.StatusLabel;
                worksheet.Cell(row, 7).Value = item.CreatedDate?.ToString("dd.MM.yyyy") ?? "-";
            }

            worksheet.Column(5).Style.NumberFormat.Format = "0.##";
            worksheet.Columns().AdjustToContents();
            worksheet.SheetView.FreezeRows(1);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"izin-raporu-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";
            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateLeaveStatus(int id, int status)
        {
            var leave = await _leaveRepository.GetByIdAsync(id);
            var employee = leave != null ? await _employeeRepository.GetByIdAsync(leave.EmployeeId) : null;
            var employeeName = BuildEmployeeName(employee, leave?.EmployeeId);
            var currentUser = User.Identity?.Name ?? "anonymous";
            var targetStatus = GetLeaveStatusDisplayName(status);

            try
            {
                var result = await _leaveService.UpdateLeaveStatusAsync(id, status);

                if (result)
                {
                    await TryLogLeaveStatusChangeAsync(
                        level: "Information",
                        message: $"İzin durumu güncellendi. İzin Id: {id}, Çalışan: {employeeName}, İşlem Yapan: {currentUser}, Yeni Durum: {targetStatus}.");

                    return RedirectToAction(nameof(LeaveRequests));
                }

                await TryLogLeaveStatusChangeAsync(
                    level: "Warning",
                    message: $"İzin durumu güncellenemedi. İzin Id: {id}, Çalışan: {employeeName}, İşlem Yapan: {currentUser}, Hedef Durum: {targetStatus}.");

                return BadRequest("Durum güncellenemedi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İzin durumu güncellenirken beklenmeyen bir hata oluştu. LeaveId: {LeaveId}, Status: {Status}", id, status);

                await TryLogLeaveStatusChangeAsync(
                    level: "Error",
                    message: $"İzin durumu güncellenirken hata oluştu. İzin Id: {id}, Çalışan: {employeeName}, İşlem Yapan: {currentUser}, Hedef Durum: {targetStatus}, Hata: {ex.Message}.");

                return StatusCode(500, "Durum güncellenirken beklenmeyen bir hata oluştu.");
            }
        }

        private async Task TryLogLeaveStatusChangeAsync(string level, string message)
        {
            try
            {
                await _userActionLogService.LogAsync(new UserActionLogEntryModel
                {
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    MacAddress = null,
                    User = User.Identity?.Name ?? "anonymous",
                    Timestamp = DateTime.UtcNow,
                    Message = message,
                    Level = level,
                    MethodName = UpdateLeaveStatusMethodName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı aksiyon logu yazılamadı. Method: {MethodName}", UpdateLeaveStatusMethodName);
            }
        }

        private static AdminLeaveRequestViewModel MapLeaveRequestItem(LeaveEntity leave, IReadOnlyDictionary<int, string> employeeNames)
        {
            var employeeName = employeeNames.TryGetValue(leave.EmployeeId, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : $"#{leave.EmployeeId}";

            return new AdminLeaveRequestViewModel
            {
                Id = leave.Id,
                EmployeeId = leave.EmployeeId,
                LeaveTypeId = leave.LeaveTypeId,
                EmployeeName = employeeName,
                LeaveType = GetLeaveTypeName(leave),
                RequestedDays = GetRequestedDays(leave),
                StartDate = leave.StartDate,
                EndDate = leave.EndDate,
                Reason = leave.Reason,
                Status = leave.Status,
                RemainingLeaveDays = GetRemainingLeaveDays(leave),
                CreatedDate = leave.CreatedDate,
                StatusLabel = GetLeaveStatusDisplayName(leave.Status),
                StatusTone = GetLeaveStatusTone(leave.Status),
                DecisionDisplay = "-",
                CanTakeAction = leave.Status == (int)LeaveStatus.Pending
            };
        }

        private static string BuildEmployeeName(Employee? employee, int? employeeId)
        {
            if (employee != null)
            {
                var fullName = string.Join(" ", new[] { employee.FirstName, employee.LastName }
                    .Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    return fullName;
                }
            }

            return employeeId.HasValue ? $"#{employeeId.Value}" : "Bilinmiyor";
        }

        private static string BuildPortalName(EmployeePortal portal)
        {
            var fullName = string.Join(" ", new[] { portal.FirstName, portal.LastName }
                .Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

            return !string.IsNullOrWhiteSpace(fullName)
                ? fullName
                : portal.Email;
        }

        private static string GetPortalEmployeeName(
            LeaveEntity leave,
            IReadOnlyDictionary<int, string> employeeEmailById,
            IReadOnlyDictionary<string, EmployeePortal> portalByEmail,
            IReadOnlyDictionary<int, string> employeeNames)
        {
            if (employeeEmailById.TryGetValue(leave.EmployeeId, out var email) &&
                portalByEmail.TryGetValue(email, out var portal))
            {
                return BuildPortalName(portal);
            }

            if (employeeNames.TryGetValue(leave.EmployeeId, out var employeeName) && !string.IsNullOrWhiteSpace(employeeName))
            {
                return employeeName;
            }

            return $"#{leave.EmployeeId}";
        }

        private static string GetLeaveTypeName(LeaveEntity leave)
        {
            return leave.LeaveType?.Name ?? string.Empty;
        }

        private static decimal GetRequestedDays(LeaveEntity leave)
        {
            if (leave.RequestedDays > 0)
            {
                return leave.RequestedDays;
            }

            return LeaveDurationCalculator.CalculateRequestedDays(leave.StartDate, leave.EndDate);
        }

        private static decimal GetRemainingLeaveDays(LeaveEntity leave)
        {
            return leave.RemainingLeaveDays;
        }

        private static LeaveStatus ToLeaveStatus(int status)
        {
            return Enum.IsDefined(typeof(LeaveStatus), status)
                ? (LeaveStatus)status
                : LeaveStatus.Pending;
        }

        private static string GetLeaveStatusDisplayName(int status)
        {
            return ToLeaveStatus(status) switch
            {
                LeaveStatus.Approved => "Onaylandı",
                LeaveStatus.Rejected => "Reddedildi",
                LeaveStatus.Cancelled => "İptal",
                _ => "Onay Bekliyor"
            };
        }

        private static string GetLeaveStatusTone(int status)
        {
            return ToLeaveStatus(status) switch
            {
                LeaveStatus.Approved => "approved",
                LeaveStatus.Rejected => "rejected",
                LeaveStatus.Cancelled => "cancelled",
                _ => "pending"
            };
        }

        private static DateTime? TryParseReportDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return DateTime.TryParse(value, out var parsedDate)
                ? parsedDate
                : null;
        }

        private async Task<AdminLeaveReportViewModel> BuildLeaveReportViewModelAsync(int? employeeId, int? leaveTypeId, string? startDate, string? endDate)
        {
            var leaves = await _leaveService.GetAllLeavesAsync();
            var employees = (await _employeeRepository.GetAllAsync(x => !x.IsDeleted)).ToList();
            var employeePortals = (await _employeePortalRepository.GetAllAsync())
                .OrderBy(x => x.FirstName)
                .ThenBy(x => x.LastName)
                .ToList();

            var employeeNames = employees.ToDictionary(
                x => x.Id,
                x => string.Join(" ", new[] { x.FirstName, x.LastName }.Where(y => !string.IsNullOrWhiteSpace(y))).Trim());
            var employeeEmailById = employees
                .Where(x => !string.IsNullOrWhiteSpace(x.Email))
                .GroupBy(x => x.Id)
                .ToDictionary(x => x.Key, x => x.First().Email!);
            var portalByEmail = employeePortals
                .Where(x => !string.IsNullOrWhiteSpace(x.Email))
                .GroupBy(x => x.Email, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
            var selectedPortal = employeeId.HasValue
                ? employeePortals.FirstOrDefault(x => x.Id == employeeId.Value)
                : null;

            var filteredLeaves = leaves.AsEnumerable();
            var parsedStartDate = TryParseReportDate(startDate);
            var parsedEndDate = TryParseReportDate(endDate);

            if (selectedPortal != null && !string.IsNullOrWhiteSpace(selectedPortal.Email))
            {
                filteredLeaves = filteredLeaves.Where(x =>
                    employeeEmailById.TryGetValue(x.EmployeeId, out var email) &&
                    string.Equals(email, selectedPortal.Email, StringComparison.OrdinalIgnoreCase));
            }

            if (leaveTypeId.HasValue)
            {
                filteredLeaves = filteredLeaves.Where(x => x.LeaveTypeId == leaveTypeId.Value);
            }

            if (parsedStartDate.HasValue)
            {
                filteredLeaves = filteredLeaves.Where(x => x.EndDate.Date >= parsedStartDate.Value.Date);
            }

            if (parsedEndDate.HasValue)
            {
                filteredLeaves = filteredLeaves.Where(x => x.StartDate.Date <= parsedEndDate.Value.Date);
            }

            var leaveTypeOptions = leaves
                .Where(x => x.LeaveType != null)
                .GroupBy(x => new { x.LeaveTypeId, x.LeaveType!.Name })
                .OrderBy(x => x.Key.Name)
                .Select(x => new AdminLeaveReportFilterOptionViewModel
                {
                    Id = x.Key.LeaveTypeId,
                    Label = x.Key.Name
                })
                .ToList();

            var model = new AdminLeaveReportViewModel
            {
                SelectedEmployeeId = employeeId,
                SelectedLeaveTypeId = leaveTypeId,
                StartDate = parsedStartDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                EndDate = parsedEndDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                EmployeeOptions = employeePortals
                    .Select(x => new AdminLeaveReportFilterOptionViewModel
                    {
                        Id = x.Id,
                        Label = BuildPortalName(x)
                    })
                    .ToList(),
                LeaveTypeOptions = leaveTypeOptions,
                Items = filteredLeaves
                    .OrderByDescending(x => x.CreatedDate)
                    .ThenByDescending(x => x.Id)
                    .Select(x => new AdminLeaveReportItemViewModel
                    {
                        Id = x.Id,
                        EmployeeName = GetPortalEmployeeName(x, employeeEmailById, portalByEmail, employeeNames),
                        LeaveType = GetLeaveTypeName(x),
                        StartDate = x.StartDate,
                        EndDate = x.EndDate,
                        RequestedDays = GetRequestedDays(x),
                        StatusLabel = GetLeaveStatusDisplayName(x.Status),
                        StatusTone = GetLeaveStatusTone(x.Status),
                        CreatedDate = x.CreatedDate
                    })
                    .ToList()
            };

            model.TotalCount = model.Items.Count;
            return model;
        }

        private async Task<AdminSliderViewModel> BuildSliderViewModelAsync()
        {
            var items = (await _sliderImageRepository.GetAllAsync())
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.CreatedDate)
                .Select(MapSliderItem)
                .ToList();

            return new AdminSliderViewModel
            {
                Items = items
            };
        }

        private async Task NormalizeSliderOrderAsync()
        {
            var items = (await _sliderImageRepository.GetAllAsync())
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.CreatedDate)
                .ToList();

            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                var normalizedDisplayOrder = index + 1;
                if (item.DisplayOrder == normalizedDisplayOrder)
                {
                    continue;
                }

                item.DisplayOrder = normalizedDisplayOrder;
                item.UpdateDate = DateTime.Now;
                _sliderImageRepository.Update(item);
            }
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

        private AdminSliderItemViewModel MapSliderItem(SliderImage image)
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

        private static string NormalizePortalUserStatus(string? status)
        {
            return string.Equals(status, "passive", StringComparison.OrdinalIgnoreCase)
                ? "passive"
                : "active";
        }

        private static AdminPortalUserListItemViewModel MapPortalUserListItem(EmployeePortal portalUser)
        {
            return new AdminPortalUserListItemViewModel
            {
                Id = portalUser.Id,
                FullName = BuildPortalName(portalUser),
                Email = portalUser.Email,
                PhoneNumber = portalUser.PhoneNumber,
                LeaveDays = portalUser.LeaveDays,
                HireDate = portalUser.HireDate,
                IsDeleted = portalUser.IsDeleted
            };
        }

        private static AdminPortalUserEditViewModel MapPortalUserEditModel(EmployeePortal portalUser)
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
                IsDeleted = portalUser.IsDeleted
            };
        }
    }
}
