using System.Globalization;
using System.Net.Mail;
using ClosedXML.Excel;
using MEC.Application.Abstractions.Common.Models;
using MEC.Application.Abstractions.Service.LeaveService;
using MEC.Application.Abstractions.Service.LeaveService.Model;
using MEC.Application.Abstractions.Service.LoggingService;
using MEC.Application.Abstractions.Service.LoggingService.Model;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Common;
using MEC.Domain.Common.Enum;
using MEC.Domain.Entity.Employee;
using MEC.Domain.Entity.Leave;

public class LeaveService : ILeaveService
{
    private readonly IGenericRepository<Leave> _leaveRepository;
    private readonly IGenericRepository<EmployeePortal> _employeePortalRepository;
    private readonly IGenericRepository<LeaveType> _leaveTypeRepository;
    private readonly IAnnouncementService _announcementService;
    private readonly IUserActionLogService _userActionLogService;

    public LeaveService(
        IGenericRepository<Leave> leaveRepository,
        IGenericRepository<EmployeePortal> employeePortalRepository,
        IGenericRepository<LeaveType> leaveTypeRepository,
        IAnnouncementService announcementService,
        IUserActionLogService userActionLogService)
    {
        _leaveRepository = leaveRepository;
        _employeePortalRepository = employeePortalRepository;
        _leaveTypeRepository = leaveTypeRepository;
        _announcementService = announcementService;
        _userActionLogService = userActionLogService;
    }

    public async Task<List<Leave>> GetAllLeavesAsync()
    {
        var leaves = await _leaveRepository.GetAllAsync(null, x => x.LeaveType);

        return leaves
            .OrderByDescending(x => x.CreatedDate)
            .ToList();
    }

    public async Task<bool> UpdateLeaveStatusAsync(int leaveId, int status, string? decisionBy = null)
    {
        var leave = (await _leaveRepository.GetAllAsync(x => x.Id == leaveId, x => x.LeaveType)).FirstOrDefault();

        if (leave == null)
        {
            return false;
        }

        var requestedDays = leave.RequestedDays > 0
            ? leave.RequestedDays
            : LeaveDurationCalculator.CalculateRequestedDays(leave.StartDate, leave.EndDate);
        var affectsAnnualBalance = string.Equals(leave.LeaveType?.Code, LeaveTypeCodes.Annual, StringComparison.OrdinalIgnoreCase);
        var statusChanged = leave.Status != status;

        if (statusChanged)
        {
            var employeePortal = await _employeePortalRepository.GetByIdAsync(leave.EmployeeId);
            if (employeePortal != null)
            {
                if (affectsAnnualBalance && leave.Status != (int)LeaveStatus.Approved && status == (int)LeaveStatus.Approved)
                {
                    employeePortal.LeaveDays -= requestedDays;
                }
                else if (affectsAnnualBalance && leave.Status == (int)LeaveStatus.Approved && status != (int)LeaveStatus.Approved)
                {
                    employeePortal.LeaveDays += requestedDays;
                }

                leave.RemainingLeaveDays = employeePortal.LeaveDays;
                _employeePortalRepository.Update(employeePortal);
            }
        }

        leave.Status = status;
        leave.RequestedDays = requestedDays;
        leave.UpdateDate = DateTime.Now;

        if (statusChanged)
        {
            if (status == (int)LeaveStatus.Pending)
            {
                leave.DecisionBy = null;
                leave.DecisionDate = null;
            }
            else
            {
                leave.DecisionBy = string.IsNullOrWhiteSpace(decisionBy) ? "anonymous" : decisionBy;
                leave.DecisionDate = DateTime.UtcNow;
            }
        }

        _leaveRepository.Update(leave);

        return true;
    }

    public async Task<List<LeaveTypeOptionModel>> GetActiveLeaveTypeOptionsAsync()
    {
        var leaveTypes = await _leaveTypeRepository.GetAllAsync(x => x.IsActive);

        return leaveTypes
            .OrderBy(x => x.Id)
            .Select(x => new LeaveTypeOptionModel
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code
            })
            .ToList();
    }

    public async Task<LeaveRequestValidationModel> ValidateLeaveRequestAsync(LeaveRequestCreateModel request)
    {
        var result = new LeaveRequestValidationModel
        {
            IsSuccess = true,
            Level = "success"
        };

        if (request.EndDate < request.StartDate)
        {
            AddFieldError(result, "EndDate", "Bitiş tarihi başlangıç tarihinden önce olamaz.");
        }

        if (!LeaveDurationCalculator.IsWithinWorkingHours(request.StartDate))
        {
            AddFieldError(result, "StartDate", "Başlangıç saati 09:00 ile 18:00 arasında olmalıdır.");
        }

        if (!LeaveDurationCalculator.IsWithinWorkingHours(request.EndDate))
        {
            AddFieldError(result, "EndDate", "Bitiş saati 09:00 ile 18:00 arasında olmalıdır.");
        }

        if (result.FieldErrors.Count == 0)
        {
            result.RequestedDays = LeaveDurationCalculator.CalculateRequestedDays(request.StartDate, request.EndDate);
            if (result.RequestedDays <= 0)
            {
                AddFieldError(result, "EndDate", "Seçilen tarih ve saat aralığı için kullanılabilir izin günü hesaplanamadı.");
            }
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            AddFieldError(result, "Reason", "İzin nedeni zorunludur.");
        }

        if (request.LeaveTypeId <= 0)
        {
            AddFieldError(result, "LeaveTypeId", "Geçerli bir izin türü seçiniz.");
        }
        else
        {
            result.SelectedLeaveType = (await GetActiveLeaveTypeOptionsAsync()).FirstOrDefault(x => x.Id == request.LeaveTypeId);
            if (result.SelectedLeaveType == null)
            {
                AddFieldError(result, "LeaveTypeId", "Geçerli bir izin türü seçiniz.");
            }
        }

        result.IsSuccess = result.FieldErrors.Count == 0;
        result.Level = result.IsSuccess ? "success" : "danger";
        result.Message = result.IsSuccess ? string.Empty : "İzin talebi doğrulanamadı.";
        result.Errors = result.FieldErrors.Values.ToList();
        return result;
    }

    public async Task<OperationResultModel<LeaveRequestCreateResultModel>> CreateLeaveRequestAsync(LeaveRequestCreateModel request)
    {
        var employeePortal = (await _employeePortalRepository.GetAllAsync(x => x.Email == request.UserEmail && !x.IsDeleted)).FirstOrDefault();
        if (employeePortal == null)
        {
            return OperationResultModel<LeaveRequestCreateResultModel>.Fail("Kullanıcı kaydı bulunamadı.");
        }

        var leaveType = (await _leaveTypeRepository.GetAllAsync(x => x.Id == request.LeaveTypeId && x.IsActive)).FirstOrDefault();
        if (leaveType == null)
        {
            return OperationResultModel<LeaveRequestCreateResultModel>.Fail("Geçerli bir izin türü seçiniz.");
        }

        var leaveRequest = new Leave
        {
            EmployeeId = employeePortal.Id,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            LeaveTypeId = leaveType.Id,
            RequestedDays = request.RequestedDays,
            RemainingLeaveDays = employeePortal.LeaveDays,
            Reason = request.Reason.Trim(),
            Status = (int)LeaveStatus.Pending,
            CreatedDate = DateTime.Now
        };

        await _leaveRepository.AddAsync(leaveRequest);

        return OperationResultModel<LeaveRequestCreateResultModel>.Success(
            new LeaveRequestCreateResultModel { LeaveId = leaveRequest.Id },
            "İzin talebiniz başarıyla gönderildi.");
    }

    public async Task DeleteLeaveAsync(int id)
    {
        var leave = await _leaveRepository.GetByIdAsync(id);
        if (leave != null)
        {
            _leaveRepository.Delete(leave);
        }
    }

    public async Task<LeaveHistoryResultModel> GetLeaveHistoryAsync(LeaveHistoryQueryModel query)
    {
        var currentYear = DateTime.Today.Year;
        var selectedSort = string.Equals(query.Sort, "created_asc", StringComparison.OrdinalIgnoreCase)
            ? "created_asc"
            : "created_desc";
        var leaveTypeOptions = await GetActiveLeaveTypeOptionsAsync();

        if (string.IsNullOrWhiteSpace(query.UserEmail))
        {
            return CreateEmptyHistoryResult(currentYear, query, selectedSort, leaveTypeOptions, "Kullanıcı kaydı bulunamadı.");
        }

        var employeePortal = (await _employeePortalRepository.GetAllAsync(x => x.Email == query.UserEmail && !x.IsDeleted)).FirstOrDefault();
        if (employeePortal == null)
        {
            return CreateEmptyHistoryResult(currentYear, query, selectedSort, leaveTypeOptions, "Kullanıcı kaydı bulunamadı.");
        }

        var employeeLeaves = (await _leaveRepository.GetAllAsync(x => x.EmployeeId == employeePortal.Id, x => x.LeaveType))
            .OrderByDescending(x => x.CreatedDate)
            .ThenByDescending(x => x.Id)
            .ToList();

        var filteredItems = employeeLeaves.Select(MapLeaveHistoryItem).AsEnumerable();

        if (query.Year.HasValue)
        {
            filteredItems = filteredItems.Where(x => x.StartDate.Year == query.Year.Value || x.EndDate.Year == query.Year.Value);
        }

        if (query.Status.HasValue)
        {
            filteredItems = filteredItems.Where(x => x.Status == query.Status.Value);
        }

        if (query.LeaveTypeId.HasValue)
        {
            filteredItems = filteredItems.Where(x => x.LeaveTypeId == query.LeaveTypeId.Value);
        }

        filteredItems = selectedSort == "created_asc"
            ? filteredItems.OrderBy(x => x.CreatedDate ?? DateTime.MinValue).ThenBy(x => x.Id)
            : filteredItems.OrderByDescending(x => x.CreatedDate ?? DateTime.MinValue).ThenByDescending(x => x.Id);

        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
        var totalCount = filteredItems.Count();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        var currentPage = Math.Min(Math.Max(query.Page, 1), totalPages);

        return new LeaveHistoryResultModel
        {
            Items = filteredItems.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList(),
            YearOptions = CreateYearOptions(employeeLeaves, currentYear),
            LeaveTypeOptions = leaveTypeOptions,
            StatusOptions = CreateStatusOptions(),
            SelectedYear = query.Year,
            SelectedStatus = query.Status,
            SelectedLeaveTypeId = query.LeaveTypeId,
            SelectedSort = selectedSort,
            CurrentPage = currentPage,
            TotalPages = totalPages,
            TotalCount = totalCount,
            PageSize = pageSize
        };
    }

    public async Task<LeaveHistoryItemModel?> GetLeaveHistoryDetailAsync(string userEmail, int leaveId)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return null;
        }

        var employeePortal = (await _employeePortalRepository.GetAllAsync(x => x.Email == userEmail && !x.IsDeleted)).FirstOrDefault();
        if (employeePortal == null)
        {
            return null;
        }

        var leave = (await _leaveRepository.GetAllAsync(x => x.EmployeeId == employeePortal.Id && x.Id == leaveId, x => x.LeaveType)).FirstOrDefault();
        return leave == null ? null : MapLeaveHistoryItem(leave);
    }

    public async Task<PagedResultModel<AdminLeaveRequestItemModel>> GetAdminLeaveRequestsAsync(AdminLeaveRequestListQueryModel query)
    {
        var currentPage = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
        var leaves = await GetAllLeavesAsync();
        if (query.Status.HasValue && Enum.IsDefined(typeof(LeaveStatus), query.Status.Value))
        {
            leaves = leaves.Where(x => x.Status == query.Status.Value).ToList();
        }

        var portalUserNames = await GetPortalUserNamesAsync();
        var mappedItems = leaves.Select(x => MapAdminLeaveRequestItem(x, portalUserNames)).ToList();

        var totalCount = mappedItems.Count;
        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
        currentPage = Math.Min(currentPage, totalPages);

        return new PagedResultModel<AdminLeaveRequestItemModel>
        {
            Items = mappedItems.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList(),
            CurrentPage = currentPage,
            TotalPages = totalPages,
            TotalCount = totalCount,
            PageSize = pageSize
        };
    }

    public async Task<AdminLeaveRequestItemModel?> GetAdminLeaveRequestDetailAsync(int id)
    {
        var leave = (await _leaveRepository.GetAllAsync(x => x.Id == id, x => x.LeaveType)).FirstOrDefault();
        if (leave == null)
        {
            return null;
        }

        var employeePortal = await _employeePortalRepository.GetByIdAsync(leave.EmployeeId);
        return MapAdminLeaveRequestItem(
            leave,
            new Dictionary<int, string> { [leave.EmployeeId] = BuildPortalName(employeePortal, leave.EmployeeId) });
    }

    public async Task<AdminLeaveReportResultModel> GetAdminLeaveReportAsync(AdminLeaveReportQueryModel query)
    {
        var leaves = await GetAllLeavesAsync();
        var employeePortals = (await _employeePortalRepository.GetAllAsync())
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .ToList();
        var portalUserNames = employeePortals.ToDictionary(x => x.Id, x => BuildPortalName(x));
        var selectedPortal = query.EmployeeId.HasValue
            ? employeePortals.FirstOrDefault(x => x.Id == query.EmployeeId.Value)
            : null;

        var filteredLeaves = leaves.AsEnumerable();
        var parsedStartDate = TryParseReportDate(query.StartDate);
        var parsedEndDate = TryParseReportDate(query.EndDate);

        if (selectedPortal != null)
        {
            filteredLeaves = filteredLeaves.Where(x => x.EmployeeId == selectedPortal.Id);
        }

        if (query.LeaveTypeId.HasValue)
        {
            filteredLeaves = filteredLeaves.Where(x => x.LeaveTypeId == query.LeaveTypeId.Value);
        }

        if (parsedStartDate.HasValue)
        {
            filteredLeaves = filteredLeaves.Where(x => x.EndDate.Date >= parsedStartDate.Value.Date);
        }

        if (parsedEndDate.HasValue)
        {
            filteredLeaves = filteredLeaves.Where(x => x.StartDate.Date <= parsedEndDate.Value.Date);
        }

        var model = new AdminLeaveReportResultModel
        {
            SelectedEmployeeId = query.EmployeeId,
            SelectedLeaveTypeId = query.LeaveTypeId,
            StartDate = parsedStartDate?.ToString("yyyy-MM-dd") ?? string.Empty,
            EndDate = parsedEndDate?.ToString("yyyy-MM-dd") ?? string.Empty,
            EmployeeOptions = employeePortals.Select(x => new AdminLeaveReportFilterOptionModel
            {
                Id = x.Id,
                Label = BuildPortalName(x)
            }).ToList(),
            LeaveTypeOptions = leaves
                .Where(x => x.LeaveType != null)
                .GroupBy(x => new { x.LeaveTypeId, x.LeaveType!.Name })
                .OrderBy(x => x.Key.Name)
                .Select(x => new AdminLeaveReportFilterOptionModel
                {
                    Id = x.Key.LeaveTypeId,
                    Label = x.Key.Name
                })
                .ToList(),
            Items = filteredLeaves
                .OrderByDescending(x => x.CreatedDate)
                .ThenByDescending(x => x.Id)
                .Select(x => new AdminLeaveReportItemModel
                {
                    Id = x.Id,
                    EmployeeName = portalUserNames.TryGetValue(x.EmployeeId, out var employeeName) && !string.IsNullOrWhiteSpace(employeeName)
                        ? employeeName
                        : $"#{x.EmployeeId}",
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

    public async Task<LeaveReportExportModel> ExportAdminLeaveReportAsync(AdminLeaveReportQueryModel query)
    {
        var model = await GetAdminLeaveReportAsync(query);

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

        return new LeaveReportExportModel
        {
            Content = stream.ToArray(),
            FileName = $"izin-raporu-{DateTime.Now:yyyyMMdd-HHmm}.xlsx"
        };
    }

    public async Task<OperationResultModel> UpdateLeaveStatusWithLogAsync(LeaveStatusUpdateRequestModel request)
    {
        var leave = await _leaveRepository.GetByIdAsync(request.LeaveId);
        var employeePortal = leave != null ? await _employeePortalRepository.GetByIdAsync(leave.EmployeeId) : null;
        var employeeName = BuildPortalName(employeePortal, leave?.EmployeeId);
        var currentUser = string.IsNullOrWhiteSpace(request.CurrentUser) ? "anonymous" : request.CurrentUser;
        var targetStatus = GetLeaveStatusDisplayName(request.Status);

        try
        {
            var result = await UpdateLeaveStatusAsync(request.LeaveId, request.Status, currentUser);
            if (result)
            {
                await TryLogLeaveStatusChangeAsync(
                    request,
                    "Information",
                    $"İzin durumu güncellendi. İzin Id: {request.LeaveId}, Çalışan: {employeeName}, İşlem Yapan: {currentUser}, Yeni Durum: {targetStatus}.");

                return OperationResultModel.Success("Durum güncellendi.");
            }

            await TryLogLeaveStatusChangeAsync(
                request,
                "Warning",
                $"İzin durumu güncellenemedi. İzin Id: {request.LeaveId}, Çalışan: {employeeName}, İşlem Yapan: {currentUser}, Hedef Durum: {targetStatus}.");

            return OperationResultModel.Fail("Durum güncellenemedi.", "Warning");
        }
        catch (Exception ex)
        {
            await TryLogLeaveStatusChangeAsync(
                request,
                "Error",
                $"İzin durumu güncellenirken hata oluştu. İzin Id: {request.LeaveId}, Çalışan: {employeeName}, İşlem Yapan: {currentUser}, Hedef Durum: {targetStatus}, Hata: {ex.Message}.");

            return OperationResultModel.Fail("Durum güncellenirken beklenmeyen bir hata oluştu.", "Error");
        }
    }

    public async Task<AdminDashboardModel> GetAdminDashboardAsync(string adminName)
    {
        var leaves = await GetAllLeavesAsync();
        var announcements = (await _announcementService.GetAllAnnouncementsAsync())
            .OrderByDescending(x => x.CreatedDate)
            .ToList();
        var employeePortals = await _employeePortalRepository.GetAllAsync();
        var today = DateTime.Today;

        var portalUserNames = employeePortals.ToDictionary(x => x.Id, x => BuildPortalName(x));

        return new AdminDashboardModel
        {
            AdminName = string.IsNullOrWhiteSpace(adminName) ? "Admin" : adminName,
            GeneratedAt = DateTime.Now,
            PendingLeaveCount = leaves.Count(x => x.Status == (int)LeaveStatus.Pending),
            TotalAnnouncementCount = announcements.Count,
            TodayAnnouncementCount = announcements.Count(x => x.CreatedDate.HasValue && x.CreatedDate.Value.Date == today),
            NegativeLeaveBalanceCount = employeePortals.Count(x => x.LeaveDays < 0),
            RecentLeaveRequests = leaves.Take(5).Select(x => new AdminRecentLeaveItemModel
            {
                EmployeeName = portalUserNames.TryGetValue(x.EmployeeId, out var employeeName) && !string.IsNullOrWhiteSpace(employeeName)
                    ? employeeName
                    : $"#{x.EmployeeId}",
                LeaveType = GetLeaveTypeName(x),
                RequestedDays = GetRequestedDays(x),
                Status = x.Status,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                CreatedDate = x.CreatedDate
            }).ToList(),
            RecentAnnouncements = announcements.Take(5).Select(x => new AdminRecentAnnouncementItemModel
            {
                Title = x.Title,
                IsActive = x.IsActive,
                CreatedDate = x.CreatedDate
            }).ToList()
        };
    }

    public async Task<BulkLeaveUploadResultModel> BulkUploadLeaveDaysAsync(BulkLeaveUploadRequestModel request)
    {
        List<BulkLeaveImportRow> importRows;
        try
        {
            importRows = ReadBulkLeaveRows(request.ExcelStream);
        }
        catch
        {
            return CreateBulkLeaveResult(false, "danger", "Excel dosyası okunamadı. Dosya formatını kontrol ediniz.", new List<BulkLeaveUpdatedUserModel>(), new List<BulkLeaveUploadErrorRowModel>());
        }

        if (importRows.Count == 0)
        {
            return CreateBulkLeaveResult(false, "danger", "Excel dosyasında işlenecek veri satırı bulunamadı.", new List<BulkLeaveUpdatedUserModel>(), new List<BulkLeaveUploadErrorRowModel>());
        }

        var activePortalUsers = (await _employeePortalRepository.GetAllAsync(x => !x.IsDeleted)).ToList();
        var portalUsersByEmail = activePortalUsers
            .Where(x => !string.IsNullOrWhiteSpace(x.Email))
            .GroupBy(x => x.Email.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        var processedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var errorRows = new List<BulkLeaveUploadErrorRowModel>();
        var updatedUsers = new List<BulkLeaveUpdatedUserModel>();

        foreach (var row in importRows)
        {
            if (string.IsNullOrWhiteSpace(row.Email))
            {
                errorRows.Add(CreateBulkLeaveError(row, "E-posta alanı boş olamaz."));
                continue;
            }

            if (!IsValidEmail(row.Email))
            {
                errorRows.Add(CreateBulkLeaveError(row, "Geçerli bir e-posta adresi girilmelidir."));
                continue;
            }

            if (!row.Days.HasValue)
            {
                errorRows.Add(CreateBulkLeaveError(row, "Eklenecek izin gün sayısı okunamadı."));
                continue;
            }

            if (row.Days.Value <= 0)
            {
                errorRows.Add(CreateBulkLeaveError(row, "Eklenecek izin gün sayısı sıfırdan büyük olmalıdır."));
                continue;
            }

            if (processedEmails.Contains(row.Email))
            {
                errorRows.Add(CreateBulkLeaveError(row, "Aynı Excel içinde bu e-posta adresi daha önce işlendi."));
                continue;
            }

            if (!portalUsersByEmail.TryGetValue(row.Email, out var portalUser))
            {
                errorRows.Add(CreateBulkLeaveError(row, "E-posta ile eşleşen aktif portal kullanıcısı bulunamadı."));
                continue;
            }

            var oldLeaveDays = portalUser.LeaveDays;
            portalUser.LeaveDays += row.Days.Value;
            portalUser.UpdateDate = DateTime.Now;

            _employeePortalRepository.Update(portalUser);
            processedEmails.Add(row.Email);

            updatedUsers.Add(new BulkLeaveUpdatedUserModel
            {
                Email = portalUser.Email,
                NewLeaveDays = portalUser.LeaveDays
            });

            await LogBulkLeaveUploadAsync(portalUser, oldLeaveDays, row.Days.Value, portalUser.LeaveDays, row.Description, request);
        }

        var level = errorRows.Count == 0 ? "success" : updatedUsers.Count > 0 ? "warning" : "danger";
        var success = updatedUsers.Count > 0;
        var message = level switch
        {
            "success" => $"Toplu izin yükleme işlemi başarılı! {updatedUsers.Count} kullanıcıya izin eklendi",
            "warning" => $"{updatedUsers.Count} kullanıcıya izin eklendi. Hatalı satırları kontrol ediniz.",
            _ => "Toplu izin yükleme işlemi tamamlanamadı. Hatalı satırları kontrol ediniz."
        };

        return CreateBulkLeaveResult(success, level, message, updatedUsers, errorRows);
    }

    private static void AddFieldError(LeaveRequestValidationModel result, string fieldName, string message)
    {
        result.FieldErrors[fieldName] = message;
    }

    private static LeaveHistoryResultModel CreateEmptyHistoryResult(
        int currentYear,
        LeaveHistoryQueryModel query,
        string selectedSort,
        List<LeaveTypeOptionModel> leaveTypeOptions,
        string? errorMessage)
    {
        return new LeaveHistoryResultModel
        {
            YearOptions = Enumerable.Range(1970, currentYear - 1969).Reverse().ToList(),
            LeaveTypeOptions = leaveTypeOptions,
            StatusOptions = CreateStatusOptions(),
            SelectedStatus = query.Status,
            SelectedLeaveTypeId = query.LeaveTypeId,
            SelectedSort = selectedSort,
            CurrentPage = 1,
            TotalPages = 1,
            TotalCount = 0,
            PageSize = query.PageSize <= 0 ? 10 : query.PageSize,
            ErrorMessage = errorMessage
        };
    }

    private static List<int> CreateYearOptions(List<Leave> leaves, int currentYear)
    {
        if (!leaves.Any())
        {
            return Enumerable.Range(1970, currentYear - 1969).Reverse().ToList();
        }

        var oldestYear = leaves.Min(x => Math.Min(x.StartDate.Year, x.EndDate.Year));
        return Enumerable.Range(oldestYear, currentYear - oldestYear + 1).ToList();
    }

    private static List<LeaveStatusOptionModel> CreateStatusOptions()
    {
        return new List<LeaveStatusOptionModel>
        {
            new() { Value = (int)LeaveStatus.Pending, Label = "Onay Bekliyor" },
            new() { Value = (int)LeaveStatus.Approved, Label = "Onaylandı" },
            new() { Value = (int)LeaveStatus.Rejected, Label = "Reddedildi" },
            new() { Value = (int)LeaveStatus.Cancelled, Label = "İptal" }
        };
    }

    private static LeaveHistoryItemModel MapLeaveHistoryItem(Leave leave)
    {
        return new LeaveHistoryItemModel
        {
            Id = leave.Id,
            LeaveTypeId = leave.LeaveTypeId,
            LeaveType = GetLeaveTypeName(leave),
            StartDate = leave.StartDate,
            EndDate = leave.EndDate,
            RequestedDays = GetRequestedDays(leave),
            Reason = leave.Reason,
            Status = leave.Status,
            RemainingLeaveDays = GetRemainingLeaveDays(leave),
            CreatedDate = leave.CreatedDate,
            StatusLabel = GetLeaveStatusDisplayName(leave.Status),
            StatusTone = GetLeaveStatusTone(leave.Status),
            DecisionDisplay = GetDecisionDisplay(leave)
        };
    }

    private static AdminLeaveRequestItemModel MapAdminLeaveRequestItem(Leave leave, IReadOnlyDictionary<int, string> employeeNames)
    {
        var employeeName = employeeNames.TryGetValue(leave.EmployeeId, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : $"#{leave.EmployeeId}";

        return new AdminLeaveRequestItemModel
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
            DecisionDisplay = GetDecisionDisplay(leave),
            CanTakeAction = leave.Status == (int)LeaveStatus.Pending
        };
    }

    private async Task<Dictionary<int, string>> GetPortalUserNamesAsync()
    {
        var portalUsers = await _employeePortalRepository.GetAllAsync(x => !x.IsDeleted);
        return portalUsers.ToDictionary(x => x.Id, x => BuildPortalName(x));
    }

    private static string BuildPortalName(EmployeePortal portal)
    {
        var fullName = string.Join(" ", new[] { portal.FirstName, portal.LastName }
            .Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

        return !string.IsNullOrWhiteSpace(fullName)
            ? fullName
            : portal.Email;
    }

    private static string BuildPortalName(EmployeePortal? portal, int? portalUserId)
    {
        if (portal != null)
        {
            return BuildPortalName(portal);
        }

        return portalUserId.HasValue ? $"#{portalUserId.Value}" : "Bilinmiyor";
    }

    private static string GetLeaveTypeName(Leave leave)
    {
        return leave.LeaveType?.Name ?? string.Empty;
    }

    private static decimal GetRequestedDays(Leave leave)
    {
        return leave.RequestedDays > 0
            ? leave.RequestedDays
            : LeaveDurationCalculator.CalculateRequestedDays(leave.StartDate, leave.EndDate);
    }

    private static decimal GetRemainingLeaveDays(Leave leave)
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

    private static string GetDecisionDisplay(Leave leave)
    {
        if (leave.Status == (int)LeaveStatus.Pending)
        {
            return "-";
        }

        return string.IsNullOrWhiteSpace(leave.DecisionBy)
            ? "Belirtilmedi"
            : leave.DecisionBy;
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

    private async Task TryLogLeaveStatusChangeAsync(LeaveStatusUpdateRequestModel request, string level, string message)
    {
        try
        {
            await _userActionLogService.LogAsync(new UserActionLogEntryModel
            {
                IpAddress = string.IsNullOrWhiteSpace(request.IpAddress) ? "unknown" : request.IpAddress,
                MacAddress = null,
                User = string.IsNullOrWhiteSpace(request.CurrentUser) ? "anonymous" : request.CurrentUser,
                Timestamp = DateTime.UtcNow,
                Message = message,
                Level = level,
                MethodName = string.IsNullOrWhiteSpace(request.MethodName) ? "UpdateLeaveStatus" : request.MethodName
            });
        }
        catch
        {
            // Logging must not block the status update flow.
        }
    }

    private static List<BulkLeaveImportRow> ReadBulkLeaveRows(Stream excelStream)
    {
        using var workbook = new XLWorkbook(excelStream);
        var worksheet = workbook.Worksheets.First();
        var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var rows = new List<BulkLeaveImportRow>();

        for (var rowNumber = 2; rowNumber <= lastRowNumber; rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            var email = row.Cell(1).GetString().Trim();
            var rawDays = row.Cell(2).GetString().Trim();
            var description = row.Cell(3).GetString().Trim();

            if (string.IsNullOrWhiteSpace(email) &&
                string.IsNullOrWhiteSpace(rawDays) &&
                string.IsNullOrWhiteSpace(description))
            {
                continue;
            }

            rows.Add(new BulkLeaveImportRow
            {
                RowNumber = rowNumber,
                Email = email,
                RawDays = rawDays,
                Days = TryReadBulkLeaveDays(row.Cell(2), out var days) ? days : null,
                Description = description
            });
        }

        return rows;
    }

    private static bool TryReadBulkLeaveDays(IXLCell cell, out decimal days)
    {
        days = 0;

        if (cell.TryGetValue<double>(out var numericValue))
        {
            days = Convert.ToDecimal(numericValue, CultureInfo.InvariantCulture);
            return true;
        }

        var rawValue = cell.GetString().Trim();
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        return decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"), out days) ||
               decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out days);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new MailAddress(email);
            return string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static BulkLeaveUploadErrorRowModel CreateBulkLeaveError(BulkLeaveImportRow row, string errorMessage)
    {
        return new BulkLeaveUploadErrorRowModel
        {
            RowNumber = row.RowNumber,
            Email = row.Email,
            RawDays = row.RawDays,
            Description = row.Description,
            ErrorMessage = errorMessage
        };
    }

    private async Task LogBulkLeaveUploadAsync(
        EmployeePortal portalUser,
        decimal oldLeaveDays,
        decimal addedDays,
        decimal newLeaveDays,
        string description,
        BulkLeaveUploadRequestModel request)
    {
        try
        {
            var employeeName = string.Join(" ", new[] { portalUser.FirstName, portalUser.LastName }
                .Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

            if (string.IsNullOrWhiteSpace(employeeName))
            {
                employeeName = portalUser.Email;
            }

            await _userActionLogService.LogAsync(new UserActionLogEntryModel
            {
                IpAddress = string.IsNullOrWhiteSpace(request.IpAddress) ? "unknown" : request.IpAddress,
                MacAddress = null,
                User = string.IsNullOrWhiteSpace(request.CurrentUser) ? "anonymous" : request.CurrentUser,
                Timestamp = DateTime.UtcNow,
                Message = $"Toplu izin yükleme ile izin hakkı eklendi. Kullanıcı: {employeeName}, Email: {portalUser.Email}, Eski İzin Hakkı: {oldLeaveDays:0.##}, Eklenen Gün: {addedDays:0.##}, Yeni İzin Hakkı: {newLeaveDays:0.##}, Açıklama: {(string.IsNullOrWhiteSpace(description) ? "-" : description)}, İşlem Yapan: {(string.IsNullOrWhiteSpace(request.CurrentUser) ? "anonymous" : request.CurrentUser)}.",
                Level = "Information",
                MethodName = string.IsNullOrWhiteSpace(request.MethodName) ? "BulkLeaveUpload" : request.MethodName
            });
        }
        catch
        {
            // Logging must not block the balance update that has already succeeded.
        }
    }

    private static BulkLeaveUploadResultModel CreateBulkLeaveResult(
        bool success,
        string level,
        string message,
        List<BulkLeaveUpdatedUserModel> updatedUsers,
        List<BulkLeaveUploadErrorRowModel> errorRows)
    {
        return new BulkLeaveUploadResultModel
        {
            Success = success,
            Level = level,
            Message = message,
            UpdatedCount = updatedUsers.Count,
            FailedCount = errorRows.Count,
            UpdatedUsers = updatedUsers,
            FailedRows = errorRows
        };
    }

    private sealed class BulkLeaveImportRow
    {
        public int RowNumber { get; set; }
        public string Email { get; set; } = string.Empty;
        public string RawDays { get; set; } = string.Empty;
        public decimal? Days { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
