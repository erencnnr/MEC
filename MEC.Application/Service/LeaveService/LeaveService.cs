using System.Globalization;
using System.IO;
using System.Net.Mail;
using ClosedXML.Excel;
using MEC.Application.Abstractions.Common.Models;
using MEC.Application.Abstractions.Service.ApprovalWorkflowService;
using MEC.Application.Abstractions.Service.ApprovalWorkflowService.Model;
using MEC.Application.Abstractions.Service.LeaveService;
using MEC.Application.Abstractions.Service.LeaveService.Model;
using MEC.Application.Abstractions.Service.LoggingService;
using MEC.Application.Abstractions.Service.LoggingService.Model;
using MEC.Application.Abstractions.Service.NotificationService;
using MEC.Application.Abstractions.Service.NotificationService.Model;
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
    private readonly IGenericRepository<Holiday> _holidayRepository;
    private readonly IGenericRepository<LeaveAgreement> _leaveAgreementRepository;
    private readonly IGenericRepository<LeavePolicySetting> _leavePolicyRepository;
    private readonly IGenericRepository<EmployeePortalLocation> _employeePortalLocationRepository;
    private readonly IGenericRepository<Location> _locationRepository;
    private readonly IAnnouncementService _announcementService;
    private readonly IUserActionLogService _userActionLogService;
    private readonly IWorkflowNotificationService _workflowNotificationService;
    private readonly IApprovalWorkflowService _approvalWorkflowService;

    public LeaveService(
        IGenericRepository<Leave> leaveRepository,
        IGenericRepository<EmployeePortal> employeePortalRepository,
        IGenericRepository<LeaveType> leaveTypeRepository,
        IGenericRepository<Holiday> holidayRepository,
        IGenericRepository<LeaveAgreement> leaveAgreementRepository,
        IGenericRepository<LeavePolicySetting> leavePolicyRepository,
        IGenericRepository<EmployeePortalLocation> employeePortalLocationRepository,
        IGenericRepository<Location> locationRepository,
        IAnnouncementService announcementService,
        IUserActionLogService userActionLogService,
        IWorkflowNotificationService workflowNotificationService,
        IApprovalWorkflowService approvalWorkflowService)
    {
        _leaveRepository = leaveRepository;
        _employeePortalRepository = employeePortalRepository;
        _leaveTypeRepository = leaveTypeRepository;
        _holidayRepository = holidayRepository;
        _leaveAgreementRepository = leaveAgreementRepository;
        _leavePolicyRepository = leavePolicyRepository;
        _employeePortalLocationRepository = employeePortalLocationRepository;
        _locationRepository = locationRepository;
        _announcementService = announcementService;
        _userActionLogService = userActionLogService;
        _workflowNotificationService = workflowNotificationService;
        _approvalWorkflowService = approvalWorkflowService;
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
            : await CalculateRequestedDaysWithHolidaysAsync(leave.StartDate, leave.EndDate);
        var affectsAnnualBalance = string.Equals(leave.LeaveType?.Code, LeaveTypeCodes.Annual, StringComparison.OrdinalIgnoreCase);
        var statusChanged = leave.Status != status;
        var previousStatus = leave.Status;
        var employeePortal = statusChanged && affectsAnnualBalance
            ? await _employeePortalRepository.GetByIdAsync(leave.EmployeeId)
            : null;

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

        if (statusChanged && affectsAnnualBalance && employeePortal != null)
        {
            var recalculatedFromAgreement = await RecalculateEmployeeAnnualBalanceAsync(employeePortal, DateTime.Today);
            if (!recalculatedFromAgreement)
            {
                if (previousStatus != (int)LeaveStatus.Approved && status == (int)LeaveStatus.Approved)
                {
                    employeePortal.LeaveDays -= requestedDays;
                }
                else if (previousStatus == (int)LeaveStatus.Approved && status != (int)LeaveStatus.Approved)
                {
                    employeePortal.LeaveDays += requestedDays;
                }

                _employeePortalRepository.Update(employeePortal);
            }

            leave.RemainingLeaveDays = employeePortal.LeaveDays;
            _leaveRepository.Update(leave);
        }

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

    public async Task<List<HolidayCalendarItemModel>> GetHolidayCalendarItemsAsync()
    {
        var holidays = await _holidayRepository.GetAllAsync(x => x.EndDate > x.StartDate);

        var calendarItems = holidays
            .OrderBy(x => x.StartDate)
            .ThenBy(x => x.EndDate)
            .Select(x => new HolidayCalendarItemModel
            {
                Id = x.Id,
                Name = x.Name,
                StartDate = x.StartDate,
                EndDate = x.EndDate
            })
            .ToList();

        var currentYear = DateTime.Today.Year;
        foreach (var statutoryHoliday in GetFixedStatutoryHolidays(
                     new DateTime(currentYear - 1, 1, 1),
                     new DateTime(currentYear + 2, 12, 31, 23, 59, 59)))
        {
            if (calendarItems.Any(x =>
                    x.StartDate == statutoryHoliday.Interval.StartDate &&
                    x.EndDate == statutoryHoliday.Interval.EndDate))
            {
                continue;
            }

            calendarItems.Add(new HolidayCalendarItemModel
            {
                Id = -calendarItems.Count - 1,
                Name = statutoryHoliday.Name,
                StartDate = statutoryHoliday.Interval.StartDate,
                EndDate = statutoryHoliday.Interval.EndDate
            });
        }

        return calendarItems
            .OrderBy(x => x.StartDate)
            .ThenBy(x => x.EndDate)
            .ToList();
    }

    public async Task<List<SaturdayPolicyModel>> GetSaturdayPoliciesAsync()
    {
        var policies = await _leavePolicyRepository.GetAllAsync();
        return policies
            .OrderBy(x => x.EffectiveFrom)
            .Select(x => new SaturdayPolicyModel
            {
                EffectiveFrom = x.EffectiveFrom.Date,
                CountSaturday = x.CountSaturday
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

        EmployeePortal? employeePortal = null;
        if (string.IsNullOrWhiteSpace(request.UserEmail))
        {
            AddFieldError(result, "UserEmail", "Kullanıcı kaydı bulunamadı.");
        }
        else
        {
            employeePortal = (await _employeePortalRepository.GetAllAsync(
                    x => x.Email == request.UserEmail && !x.IsDeleted))
                .FirstOrDefault();

            if (employeePortal == null)
            {
                AddFieldError(result, "UserEmail", "Kullanıcı kaydı bulunamadı.");
            }
            else
            {
                await RecalculateEmployeeAnnualBalanceAsync(employeePortal, DateTime.Today);
            }
        }

        if (!result.FieldErrors.ContainsKey("StartDate") &&
            !result.FieldErrors.ContainsKey("EndDate"))
        {
            result.RequestedDays = await CalculateRequestedDaysWithHolidaysAsync(request.StartDate, request.EndDate);
            if (result.RequestedDays <= 0)
            {
                AddFieldError(result, "EndDate", "Seçilen tarih ve saat aralığı için kullanılabilir izin günü hesaplanamadı.");
            }
        }

        if (result.SelectedLeaveType != null && result.RequestedDays > 0)
        {
            ValidateLeaveTypeSpecificRules(result, request, employeePortal);
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

        var validation = await ValidateLeaveRequestAsync(request);
        if (!validation.IsSuccess || validation.SelectedLeaveType == null)
        {
            return OperationResultModel<LeaveRequestCreateResultModel>.Fail(
                validation.Errors.FirstOrDefault() ?? "İzin talebi doğrulanamadı.");
        }

        var leaveType = (await _leaveTypeRepository.GetAllAsync(x => x.Id == request.LeaveTypeId && x.IsActive)).FirstOrDefault();
        if (leaveType == null)
        {
            return OperationResultModel<LeaveRequestCreateResultModel>.Fail("Geçerli bir izin türü seçiniz.");
        }

        var requestedDays = validation.RequestedDays;

        var approvalRoute = await _approvalWorkflowService.ResolveRouteAsync(employeePortal.Id);
        var routeValidationError = ValidateApprovalRoute(approvalRoute);
        if (!string.IsNullOrWhiteSpace(routeValidationError))
        {
            return OperationResultModel<LeaveRequestCreateResultModel>.Fail(routeValidationError);
        }

        var leaveRequest = new Leave
        {
            EmployeeId = employeePortal.Id,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            LeaveTypeId = leaveType.Id,
            RequestedDays = requestedDays,
            MinimumBlockExceptionRequested = request.MinimumBlockExceptionRequested,
            RemainingLeaveDays = employeePortal.LeaveDays,
            Reason = request.Reason.Trim(),
            Status = approvalRoute!.RequiresManagerApproval
                ? (int)LeaveStatus.Pending
                : (int)LeaveStatus.PendingFinalApproval,
            CreatedDate = DateTime.Now
        };

        await _leaveRepository.AddAsync(leaveRequest);

        return OperationResultModel<LeaveRequestCreateResultModel>.Success(
            new LeaveRequestCreateResultModel { LeaveId = leaveRequest.Id },
            "İzin talebiniz başarıyla gönderildi.");
    }

    public async Task DispatchLeaveRequestCreatedNotificationsAsync(LeaveRequestCreatedDispatchModel request)
    {
        if (request.LeaveId <= 0 || string.IsNullOrWhiteSpace(request.UserEmail))
        {
            return;
        }

        var employeePortal = (await _employeePortalRepository.GetAllAsync(x => x.Email == request.UserEmail && !x.IsDeleted)).FirstOrDefault();
        if (employeePortal == null)
        {
            return;
        }

        var leave = (await _leaveRepository.GetAllAsync(
                x => x.Id == request.LeaveId && x.EmployeeId == employeePortal.Id,
                x => x.LeaveType!))
            .FirstOrDefault();

        if (leave == null)
        {
            return;
        }

        var approvalRoute = await _approvalWorkflowService.ResolveRouteAsync(employeePortal.Id);
        if (approvalRoute == null)
        {
            return;
        }

        var isFinalApprovalStage = leave.Status == (int)LeaveStatus.PendingFinalApproval;
        var approvers = isFinalApprovalStage
            ? new List<ApprovalRecipientModel> { approvalRoute.FinalApprover }
            : approvalRoute.ManagerApprovers;

        await _workflowNotificationService.NotifyLeaveRequestCreatedAsync(new LeaveRequestCreatedNotificationModel
        {
            LeaveId = leave.Id,
            EmployeeName = BuildPortalName(employeePortal),
            EmployeeEmail = employeePortal.Email ?? string.Empty,
            StartDate = leave.StartDate,
            EndDate = leave.EndDate,
            Reason = leave.Reason,
            LocationNames = approvalRoute.LocationNames,
            ApprovalTarget = isFinalApprovalStage
                ? $"Genel Müdürlüğe ({approvalRoute.FinalApprover.DisplayName})"
                : $"{approvalRoute.LocationNames} okul müdürüne",
            Approvers = ToNotificationRecipients(approvers),
            TriggeredByUser = string.IsNullOrWhiteSpace(request.UserEmail) ? BuildPortalName(employeePortal) : request.UserEmail,
            IpAddress = string.IsNullOrWhiteSpace(request.IpAddress) ? "unknown" : request.IpAddress
        });
    }

    public async Task<OperationResultModel> CancelLeaveRequestAsync(LeaveCancelRequestModel request)
    {
        if (string.IsNullOrWhiteSpace(request.UserEmail))
        {
            return OperationResultModel.Fail("Kullanıcı kaydı bulunamadı.");
        }

        var employeePortal = (await _employeePortalRepository.GetAllAsync(x => x.Email == request.UserEmail && !x.IsDeleted)).FirstOrDefault();
        if (employeePortal == null)
        {
            return OperationResultModel.Fail("Kullanıcı kaydı bulunamadı.");
        }

        var leave = (await _leaveRepository.GetAllAsync(
                x => x.Id == request.LeaveId && x.EmployeeId == employeePortal.Id,
                x => x.LeaveType!))
            .FirstOrDefault();

        if (leave == null)
        {
            return OperationResultModel.Fail("İzin talebi bulunamadı.");
        }

        if (!IsPendingApproval(leave.Status))
        {
            return OperationResultModel.Fail("Sadece onay bekleyen izin talepleri iptal edilebilir.");
        }

        var employeeName = BuildPortalName(employeePortal);
        var cancelledBy = string.IsNullOrWhiteSpace(request.CancelledBy) ? employeeName : request.CancelledBy;
        var currentUser = string.IsNullOrWhiteSpace(request.CurrentUser) ? request.UserEmail : request.CurrentUser;
        var approvalRoute = await _approvalWorkflowService.ResolveRouteAsync(employeePortal.Id);

        try
        {
            var result = await UpdateLeaveStatusAsync(leave.Id, (int)LeaveStatus.Cancelled, cancelledBy);
            if (!result)
            {
                await LogWorkflowActionAsync(
                    request.IpAddress,
                    currentUser,
                    "Warning",
                    request.MethodName,
                    $"İzin talebi iptal edilemedi. İzin Id: {leave.Id}, Çalışan: {employeeName}.");

                return OperationResultModel.Fail("İzin talebi iptal edilemedi.", "Warning");
            }

            await LogWorkflowActionAsync(
                request.IpAddress,
                currentUser,
                "Information",
                request.MethodName,
                $"İzin talebi iptal edildi. İzin Id: {leave.Id}, Çalışan: {employeeName}, Başlangıç: {leave.StartDate:dd.MM.yyyy HH:mm}, Bitiş: {leave.EndDate:dd.MM.yyyy HH:mm}.");

            await _workflowNotificationService.NotifyLeaveRequestCancelledAsync(new LeaveRequestCancelledNotificationModel
            {
                LeaveId = leave.Id,
                EmployeeName = employeeName,
                StartDate = leave.StartDate,
                EndDate = leave.EndDate,
                Reason = leave.Reason,
                CancelledBy = cancelledBy,
                Approvers = ToNotificationRecipients(GetCurrentApprovers(leave.Status, approvalRoute)),
                TriggeredByUser = currentUser,
                IpAddress = string.IsNullOrWhiteSpace(request.IpAddress) ? "unknown" : request.IpAddress
            });

            return OperationResultModel.Success("İzin talebi iptal edildi.");
        }
        catch (Exception ex)
        {
            await LogWorkflowActionAsync(
                request.IpAddress,
                currentUser,
                "Error",
                request.MethodName,
                $"İzin talebi iptal edilirken hata oluştu. İzin Id: {leave.Id}, Çalışan: {employeeName}, Hata: {ex.Message}");

            return OperationResultModel.Fail("İzin talebi iptal edilirken beklenmeyen bir hata oluştu.", "Error");
        }
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

        var actor = await _approvalWorkflowService.GetActorAsync(query.CurrentUserEmail);
        if (actor == null)
        {
            return new PagedResultModel<AdminLeaveRequestItemModel>
            {
                CurrentPage = 1,
                TotalPages = 1,
                PageSize = pageSize
            };
        }

        var routes = await _approvalWorkflowService.ResolveRoutesAsync(leaves.Select(x => x.EmployeeId));
        leaves = leaves
            .Where(x => routes.TryGetValue(x.EmployeeId, out var route) && CanViewRequest(actor, route))
            .ToList();

        var portalUserNames = routes.Values
            .ToDictionary(x => x.EmployeePortalId, x => x.EmployeeName);
        var mappedItems = leaves
            .Select(x => MapAdminLeaveRequestItem(
                x,
                portalUserNames,
                routes.GetValueOrDefault(x.EmployeeId),
                CanTakeAction(x.Status, actor, routes.GetValueOrDefault(x.EmployeeId))))
            .ToList();

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

    public async Task<AdminLeaveRequestItemModel?> GetAdminLeaveRequestDetailAsync(int id, string currentUserEmail)
    {
        var leave = (await _leaveRepository.GetAllAsync(x => x.Id == id, x => x.LeaveType)).FirstOrDefault();
        if (leave == null)
        {
            return null;
        }

        var employeePortal = await _employeePortalRepository.GetByIdAsync(leave.EmployeeId);
        var actor = await _approvalWorkflowService.GetActorAsync(currentUserEmail);
        var route = await _approvalWorkflowService.ResolveRouteAsync(leave.EmployeeId);
        if (actor == null || !CanViewRequest(actor, route))
        {
            return null;
        }

        return MapAdminLeaveRequestItem(
            leave,
            new Dictionary<int, string> { [leave.EmployeeId] = BuildPortalName(employeePortal, leave.EmployeeId) },
            route,
            CanTakeAction(leave.Status, actor, route));
    }

    public async Task<PagedResultModel<AdminLeaveAgreementItemModel>> GetAdminLeaveAgreementsAsync(AdminLeaveAgreementListQueryModel query)
    {
        var currentPage = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
        var selectedSort = string.Equals(query.SortOrder, "CreatedDate_Asc", StringComparison.OrdinalIgnoreCase)
            ? "CreatedDate_Asc"
            : "CreatedDate_Desc";
        var normalizedSearch = query.SearchText?.Trim();

        var agreements = (await _leaveAgreementRepository.GetAllAsync(null, x => x.EmployeePortal))
            .Where(x => x.EmployeePortal != null && !x.EmployeePortal.IsDeleted)
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            agreements = agreements.Where(x => LeaveAgreementMatchesSearch(x, normalizedSearch));
        }

        if (query.IsSigned.HasValue)
        {
            agreements = agreements.Where(x => x.IsSigned == query.IsSigned.Value);
        }

        agreements = selectedSort == "CreatedDate_Asc"
            ? agreements.OrderBy(x => x.CreatedDate ?? DateTime.MinValue).ThenBy(x => BuildPortalName(x.EmployeePortal!, x.EmployeePortalId))
            : agreements.OrderByDescending(x => x.CreatedDate ?? DateTime.MinValue).ThenBy(x => BuildPortalName(x.EmployeePortal!, x.EmployeePortalId));

        var items = agreements
            .Select(MapAdminLeaveAgreementItem)
            .ToList();

        var totalCount = items.Count;
        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
        currentPage = Math.Min(currentPage, totalPages);

        return new PagedResultModel<AdminLeaveAgreementItemModel>
        {
            Items = items.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList(),
            CurrentPage = currentPage,
            TotalPages = totalPages,
            TotalCount = totalCount,
            PageSize = pageSize
        };
    }

    public async Task<AdminLeaveAgreementItemModel?> GetAdminLeaveAgreementAsync(int id)
    {
        var agreement = (await _leaveAgreementRepository.GetAllAsync(x => x.Id == id, x => x.EmployeePortal)).FirstOrDefault();
        return agreement == null ? null : MapAdminLeaveAgreementItem(agreement);
    }

    public async Task<OperationResultModel> SyncLeaveAgreementsAsync()
    {
        var activePortalUsers = (await _employeePortalRepository.GetAllAsync(x => !x.IsDeleted)).ToList();
        var existingAgreements = (await _leaveAgreementRepository.GetAllAsync()).ToList();
        var existingEmployeeIds = existingAgreements
            .Select(x => x.EmployeePortalId)
            .ToHashSet();
        var createdCount = 0;

        foreach (var portalUser in activePortalUsers)
        {
            if (existingEmployeeIds.Contains(portalUser.Id))
            {
                continue;
            }

            await _leaveAgreementRepository.AddAsync(new LeaveAgreement
            {
                EmployeePortalId = portalUser.Id,
                AgreedLeaveDays = 0,
                IsSigned = false,
                CreatedDate = DateTime.Now,
                UpdateDate = DateTime.Now
            });

            existingEmployeeIds.Add(portalUser.Id);
            createdCount++;
        }

        return createdCount > 0
            ? OperationResultModel.Success($"{createdCount} mutabakat kaydı oluşturuldu.")
            : OperationResultModel.Success("Eksik mutabakat kaydı bulunamadı.");
    }

    public async Task<OperationResultModel> UploadLeaveAgreementsAsync(LeaveAgreementUploadRequestModel request)
    {
        List<LeaveAgreementImportRow> importRows;
        try
        {
            importRows = ReadLeaveAgreementRows(request.ExcelStream);
        }
        catch
        {
            return OperationResultModel.Fail("Excel dosyası okunamadı. Dosya formatını kontrol ediniz.");
        }

        if (importRows.Count == 0)
        {
            return OperationResultModel.Fail("Excel dosyasında işlenecek veri satırı bulunamadı.");
        }

        var activePortalUsers = (await _employeePortalRepository.GetAllAsync(x => !x.IsDeleted)).ToList();
        var portalUsersByEmail = activePortalUsers
            .Where(x => !string.IsNullOrWhiteSpace(x.Email))
            .GroupBy(x => x.Email.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        var existingAgreements = (await _leaveAgreementRepository.GetAllAsync()).ToList();
        var agreementsByEmployeeId = existingAgreements.ToDictionary(x => x.EmployeePortalId, x => x);
        var processedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var updatedCount = 0;
        var errorMessages = new List<string>();

        foreach (var row in importRows)
        {
            if (string.IsNullOrWhiteSpace(row.Email))
            {
                errorMessages.Add($"{row.RowNumber}. satır: E-posta alanı boş olamaz.");
                continue;
            }

            if (!IsValidEmail(row.Email))
            {
                errorMessages.Add($"{row.RowNumber}. satır: Geçerli bir e-posta adresi girilmelidir.");
                continue;
            }

            if (!row.AgreedLeaveDays.HasValue)
            {
                errorMessages.Add($"{row.RowNumber}. satır: Mutabık kalınan izin gün değeri okunamadı.");
                continue;
            }

            if (row.AgreedLeaveDays.Value < 0)
            {
                errorMessages.Add($"{row.RowNumber}. satır: Mutabık kalınan izin gün değeri negatif olamaz.");
                continue;
            }

            if (!row.BalanceAsOfDate.HasValue || row.BalanceAsOfDate.Value.Date > DateTime.Today)
            {
                errorMessages.Add($"{row.RowNumber}. satır: Mutabakat tarihi boş bırakılamaz ve bugünden ileri olamaz.");
                continue;
            }

            if (!row.CurrentYearEarnedDays.HasValue || !row.CurrentYearUsedDays.HasValue ||
                row.CurrentYearEarnedDays.Value < 0 || row.CurrentYearUsedDays.Value < 0)
            {
                errorMessages.Add($"{row.RowNumber}. satır: Yıl içinde hak edilen ve kullanılan izin değerleri geçerli olmalıdır.");
                continue;
            }

            if (!processedEmails.Add(row.Email))
            {
                errorMessages.Add($"{row.RowNumber}. satır: Aynı Excel içinde bu e-posta adresi daha önce işlendi.");
                continue;
            }

            if (!portalUsersByEmail.TryGetValue(row.Email, out var portalUser))
            {
                errorMessages.Add($"{row.RowNumber}. satır: E-posta ile eşleşen aktif portal kullanıcısı bulunamadı.");
                continue;
            }

            if (agreementsByEmployeeId.TryGetValue(portalUser.Id, out var agreement))
            {
                agreement.AgreedLeaveDays = row.AgreedLeaveDays.Value;
                agreement.BalanceAsOfDate = row.BalanceAsOfDate.Value.Date;
                agreement.CurrentYearEarnedDays = row.CurrentYearEarnedDays.Value;
                agreement.CurrentYearUsedDays = row.CurrentYearUsedDays.Value;
                agreement.IsSigned = false;
                agreement.UpdateDate = DateTime.Now;
                _leaveAgreementRepository.Update(agreement);
            }
            else
            {
                agreement = new LeaveAgreement
                {
                    EmployeePortalId = portalUser.Id,
                    AgreedLeaveDays = row.AgreedLeaveDays.Value,
                    BalanceAsOfDate = row.BalanceAsOfDate.Value.Date,
                    CurrentYearEarnedDays = row.CurrentYearEarnedDays.Value,
                    CurrentYearUsedDays = row.CurrentYearUsedDays.Value,
                    IsSigned = false,
                    CreatedDate = DateTime.Now,
                    UpdateDate = DateTime.Now
                };

                await _leaveAgreementRepository.AddAsync(agreement);
                agreementsByEmployeeId[portalUser.Id] = agreement;
            }

            await RecalculateEmployeeAnnualBalanceAsync(portalUser, DateTime.Today);

            updatedCount++;
        }

        if (updatedCount == 0)
        {
            return OperationResultModel.Fail(errorMessages.FirstOrDefault() ?? "Excel verisi işlenemedi.");
        }

        if (errorMessages.Count > 0)
        {
            return OperationResultModel.Success(
                $"{updatedCount} mutabakat kaydı işlendi. {errorMessages.Count} satır atlandı. İlk hata: {errorMessages[0]}",
                "warning");
        }

        return OperationResultModel.Success($"{updatedCount} mutabakat kaydı başarıyla işlendi.");
    }

    public async Task<OperationResultModel> UpdateLeaveAgreementAsync(AdminLeaveAgreementUpdateModel model)
    {
        if (model.AgreedLeaveDays < 0)
        {
            return OperationResultModel.Fail("Mutabık kalınan izin gün değeri negatif olamaz.");
        }

        if (!model.BalanceAsOfDate.HasValue || model.BalanceAsOfDate.Value.Date > DateTime.Today)
        {
            return OperationResultModel.Fail("Mutabakat tarihi boş bırakılamaz ve bugünden ileri olamaz.");
        }

        if (model.CurrentYearEarnedDays < 0 || model.CurrentYearUsedDays < 0)
        {
            return OperationResultModel.Fail("Yıl içinde hak edilen ve kullanılan izin günleri negatif olamaz.");
        }

        var agreement = (await _leaveAgreementRepository.GetAllAsync(x => x.Id == model.Id, x => x.EmployeePortal)).FirstOrDefault();
        if (agreement == null)
        {
            return OperationResultModel.Fail("Mutabakat kaydı bulunamadı.");
        }

        agreement.AgreedLeaveDays = model.AgreedLeaveDays;
        agreement.BalanceAsOfDate = model.BalanceAsOfDate.Value.Date;
        agreement.CurrentYearEarnedDays = model.CurrentYearEarnedDays;
        agreement.CurrentYearUsedDays = model.CurrentYearUsedDays;
        agreement.IsSigned = model.IsSigned;
        agreement.UpdateDate = DateTime.Now;

        _leaveAgreementRepository.Update(agreement);

        if (agreement.EmployeePortal != null)
        {
            await RecalculateEmployeeAnnualBalanceAsync(agreement.EmployeePortal, DateTime.Today);
        }

        return OperationResultModel.Success($"{BuildPortalName(agreement.EmployeePortal, agreement.EmployeePortalId)} için izin mutabakat kaydı güncellendi.");
    }

    public async Task<OperationResultModel> UpdateLeaveAgreementPdfAsync(AdminLeaveAgreementPdfUpdateModel model)
    {
        if (string.IsNullOrWhiteSpace(model.FileName))
        {
            return OperationResultModel.Fail("Yüklenecek PDF dosyası bulunamadı.");
        }

        var agreement = (await _leaveAgreementRepository.GetAllAsync(x => x.Id == model.Id, x => x.EmployeePortal)).FirstOrDefault();
        if (agreement == null)
        {
            return OperationResultModel.Fail("Mutabakat kaydı bulunamadı.");
        }

        agreement.AgreementPdfFileName = model.FileName;
        agreement.AgreementPdfOriginalFileName = model.OriginalFileName;
        agreement.AgreementPdfContentType = string.IsNullOrWhiteSpace(model.ContentType) ? "application/pdf" : model.ContentType;
        agreement.AgreementPdfSizeBytes = model.SizeBytes;
        agreement.AgreementPdfUploadedAt = DateTime.Now;
        agreement.UpdateDate = DateTime.Now;

        _leaveAgreementRepository.Update(agreement);

        return OperationResultModel.Success($"{BuildPortalName(agreement.EmployeePortal, agreement.EmployeePortalId)} için mutabakat PDF'i yüklendi.");
    }

    public async Task<AdminLeavePolicyModel> GetAdminLeavePolicyAsync()
    {
        var policies = (await _leavePolicyRepository.GetAllAsync())
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.Id)
            .ToList();
        var currentPolicy = policies
            .Where(x => x.EffectiveFrom.Date <= DateTime.Today)
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefault();

        return new AdminLeavePolicyModel
        {
            CurrentCountSaturday = currentPolicy?.CountSaturday ?? false,
            SaturdayPolicies = policies.Select(x => new AdminSaturdayPolicyItemModel
            {
                Id = x.Id,
                EffectiveFrom = x.EffectiveFrom.Date,
                CountSaturday = x.CountSaturday,
                CreatedDate = x.CreatedDate
            }).ToList()
        };
    }

    public async Task<AdminLeaveBalanceResultModel> GetAdminLeaveBalancesAsync(AdminLeaveBalanceQueryModel query)
    {
        var actor = await _approvalWorkflowService.GetActorAsync(query.CurrentUserEmail);
        var canViewAllLocations = actor?.IsAdministrator == true || actor?.IsFinalApprover == true;
        var isAuthorized = actor != null && (canViewAllLocations || actor.IsLocationManager);
        var currentPage = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
        var today = DateTime.Today;

        if (!isAuthorized)
        {
            return new AdminLeaveBalanceResultModel
            {
                IsAuthorized = false,
                CurrentPage = 1,
                TotalPages = 1,
                PageSize = pageSize
            };
        }

        var employees = (await _employeePortalRepository.GetAllAsync(
                x => !x.IsDeleted && (!x.TerminationDate.HasValue || x.TerminationDate.Value >= today)))
            .ToList();
        var employeeIds = employees.Select(x => x.Id).ToHashSet();
        var assignments = (await _employeePortalLocationRepository.GetAllAsync(
                x => employeeIds.Contains(x.EmployeePortalId),
                x => x.Location!))
            .Where(x => x.Location != null)
            .ToList();
        var assignmentsByEmployee = assignments
            .GroupBy(x => x.EmployeePortalId)
            .ToDictionary(x => x.Key, x => x.ToList());

        if (!canViewAllLocations)
        {
            var managerLocationIds = actor!.LocationIds.ToHashSet();
            employees = employees
                .Where(x => assignmentsByEmployee.TryGetValue(x.Id, out var employeeAssignments) &&
                            employeeAssignments.Any(a => managerLocationIds.Contains(a.LocationId)))
                .ToList();
        }

        var visibleEmployeeIds = employees.Select(x => x.Id).ToHashSet();
        var visibleAssignments = assignments
            .Where(x => visibleEmployeeIds.Contains(x.EmployeePortalId))
            .ToList();
        var permittedLocationIds = canViewAllLocations
            ? visibleAssignments.Select(x => x.LocationId).Distinct().ToHashSet()
            : actor!.LocationIds.ToHashSet();
        var locations = (await _locationRepository.GetAllAsync(x => permittedLocationIds.Contains(x.Id)))
            .OrderBy(x => x.Name)
            .ToList();

        if (query.LocationId.HasValue)
        {
            employees = permittedLocationIds.Contains(query.LocationId.Value)
                ? employees.Where(x => assignmentsByEmployee.TryGetValue(x.Id, out var employeeAssignments) &&
                                       employeeAssignments.Any(a => a.LocationId == query.LocationId.Value)).ToList()
                : new List<EmployeePortal>();
        }

        var normalizedSearch = query.SearchText?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            employees = employees.Where(x =>
            {
                var employeeName = BuildPortalName(x);
                var locationNames = BuildLocationNames(assignmentsByEmployee.GetValueOrDefault(x.Id));
                return employeeName.Contains(normalizedSearch, StringComparison.CurrentCultureIgnoreCase) ||
                       (!string.IsNullOrWhiteSpace(x.Email) && x.Email.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)) ||
                       (!string.IsNullOrWhiteSpace(x.Title) && x.Title.Contains(normalizedSearch, StringComparison.CurrentCultureIgnoreCase)) ||
                       locationNames.Contains(normalizedSearch, StringComparison.CurrentCultureIgnoreCase);
            }).ToList();
        }

        employees = employees
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .ThenBy(x => x.Id)
            .ToList();

        var totalCount = employees.Count;
        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
        currentPage = Math.Min(currentPage, totalPages);
        var pageEmployees = employees
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        var pageEmployeeIds = pageEmployees.Select(x => x.Id).ToHashSet();
        var agreements = (await _leaveAgreementRepository.GetAllAsync(
                x => pageEmployeeIds.Contains(x.EmployeePortalId)))
            .ToDictionary(x => x.EmployeePortalId, x => x);
        var items = new List<AdminLeaveBalanceItemModel>();

        foreach (var employee in pageEmployees)
        {
            await RecalculateEmployeeAnnualBalanceAsync(employee, today);
            agreements.TryGetValue(employee.Id, out var agreement);
            var completedServiceYears = GetCompletedServiceYears(employee.HireDate, today);

            items.Add(new AdminLeaveBalanceItemModel
            {
                EmployeePortalId = employee.Id,
                EmployeeName = BuildPortalName(employee),
                Email = employee.Email,
                Title = employee.Title,
                LocationNames = BuildLocationNames(assignmentsByEmployee.GetValueOrDefault(employee.Id)),
                HireDate = employee.HireDate,
                CompletedServiceYears = completedServiceYears,
                AnnualEntitlementDays = employee.HireDate.HasValue && employee.HireDate.Value.Year > 1900
                    ? AnnualLeaveEntitlementCalculator.CalculateEntitlement(employee.HireDate.Value, employee.BirthDate, today)
                    : 0m,
                CurrentBalance = employee.LeaveDays,
                NextEntitlementDate = employee.HireDate.HasValue && employee.HireDate.Value.Year > 1900
                    ? employee.HireDate.Value.Date.AddYears(Math.Max(1, completedServiceYears + 1))
                    : null,
                HasReconciliation = agreement?.BalanceAsOfDate != null,
                BalanceAsOfDate = agreement?.BalanceAsOfDate,
                ReconciledOpeningBalance = agreement?.AgreedLeaveDays ?? 0m,
                CurrentYearEarnedDays = agreement?.CurrentYearEarnedDays ?? 0m,
                CurrentYearUsedDays = agreement?.CurrentYearUsedDays ?? 0m
            });
        }

        return new AdminLeaveBalanceResultModel
        {
            IsAuthorized = true,
            CanViewAllLocations = canViewAllLocations,
            SearchText = normalizedSearch ?? string.Empty,
            SelectedLocationId = query.LocationId,
            LocationOptions = locations.Select(x => new AdminLeaveBalanceLocationOptionModel
            {
                Id = x.Id,
                Name = x.Name
            }).ToList(),
            Items = items,
            CurrentPage = currentPage,
            TotalPages = totalPages,
            TotalCount = totalCount,
            PageSize = pageSize
        };
    }

    public async Task<OperationResultModel> UpdateSaturdayPolicyAsync(AdminSaturdayPolicyUpdateModel model)
    {
        if (model.EffectiveFrom.Year < 2000)
        {
            return OperationResultModel.Fail("Geçerli bir başlangıç tarihi giriniz.");
        }

        var effectiveDate = model.EffectiveFrom.Date;
        var existingPolicy = (await _leavePolicyRepository.GetAllAsync(
                x => x.EffectiveFrom == effectiveDate))
            .FirstOrDefault();

        if (existingPolicy == null)
        {
            await _leavePolicyRepository.AddAsync(new LeavePolicySetting
            {
                EffectiveFrom = effectiveDate,
                CountSaturday = model.CountSaturday,
                CreatedDate = DateTime.Now,
                UpdateDate = DateTime.Now
            });
        }
        else
        {
            existingPolicy.CountSaturday = model.CountSaturday;
            existingPolicy.UpdateDate = DateTime.Now;
            _leavePolicyRepository.Update(existingPolicy);
        }

        var affectedLeaves = (await _leaveRepository.GetAllAsync(
                x => x.EndDate >= effectiveDate,
                x => x.LeaveType!))
            .ToList();

        foreach (var leave in affectedLeaves)
        {
            leave.RequestedDays = await CalculateRequestedDaysWithHolidaysAsync(leave.StartDate, leave.EndDate);
            leave.UpdateDate = DateTime.Now;
            _leaveRepository.Update(leave);
        }

        await RecalculateAllAnnualLeaveBalancesAsync();

        return OperationResultModel.Success(
            $"Cumartesi kuralı {effectiveDate:dd.MM.yyyy} tarihinden itibaren güncellendi. {affectedLeaves.Count} izin kaydı yeniden hesaplandı.");
    }

    public async Task RecalculateAllAnnualLeaveBalancesAsync()
    {
        var employees = (await _employeePortalRepository.GetAllAsync(x => !x.IsDeleted)).ToList();
        foreach (var employee in employees)
        {
            await RecalculateEmployeeAnnualBalanceAsync(employee, DateTime.Today);
        }
    }

    private async Task<bool> RecalculateEmployeeAnnualBalanceAsync(EmployeePortal employeePortal, DateTime asOfDate)
    {
        var agreement = (await _leaveAgreementRepository.GetAllAsync(
                x => x.EmployeePortalId == employeePortal.Id))
            .FirstOrDefault();

        if (agreement?.BalanceAsOfDate == null)
        {
            return false;
        }

        var cutoffDate = agreement.BalanceAsOfDate.Value.Date;
        var entitlementThrough = employeePortal.TerminationDate.HasValue && employeePortal.TerminationDate.Value.Date < asOfDate.Date
            ? employeePortal.TerminationDate.Value.Date
            : asOfDate.Date;
        var earnedAfterCutoff = 0m;

        if (employeePortal.HireDate.HasValue && employeePortal.HireDate.Value.Year > 1900)
        {
            earnedAfterCutoff = AnnualLeaveEntitlementCalculator.GetEntitlements(
                    employeePortal.HireDate.Value,
                    employeePortal.BirthDate,
                    cutoffDate,
                    entitlementThrough)
                .Sum(x => x.Days);
        }

        var approvedAnnualLeaves = await _leaveRepository.GetAllAsync(
            x => x.EmployeeId == employeePortal.Id &&
                 x.Status == (int)LeaveStatus.Approved &&
                 x.StartDate >= cutoffDate.AddDays(1),
            x => x.LeaveType!);
        var usedAfterCutoff = approvedAnnualLeaves
            .Where(x => string.Equals(x.LeaveType?.Code, LeaveTypeCodes.Annual, StringComparison.OrdinalIgnoreCase))
            .Sum(x => x.RequestedDays > 0
                ? x.RequestedDays
                : LeaveDurationCalculator.CalculateRequestedDays(x.StartDate, x.EndDate));

        employeePortal.LeaveDays = agreement.AgreedLeaveDays + earnedAfterCutoff - usedAfterCutoff;
        employeePortal.UpdateDate = DateTime.Now;
        _employeePortalRepository.Update(employeePortal);
        return true;
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
        var leave = (await _leaveRepository.GetAllAsync(x => x.Id == request.LeaveId, x => x.LeaveType)).FirstOrDefault();
        var employeePortal = leave != null ? await _employeePortalRepository.GetByIdAsync(leave.EmployeeId) : null;
        var employeeName = BuildPortalName(employeePortal, leave?.EmployeeId);
        var currentUser = string.IsNullOrWhiteSpace(request.CurrentUser) ? "anonymous" : request.CurrentUser;
        var targetStatus = GetLeaveStatusDisplayName(request.Status);

        try
        {
            if (leave == null || employeePortal == null)
            {
                return OperationResultModel.Fail("İzin talebi bulunamadı.");
            }

            if (request.Status != (int)LeaveStatus.Approved && request.Status != (int)LeaveStatus.Rejected)
            {
                return OperationResultModel.Fail("Bu işlem için yalnızca onay veya ret kararı verilebilir.");
            }

            var actor = await _approvalWorkflowService.GetActorAsync(currentUser);
            var approvalRoute = await _approvalWorkflowService.ResolveRouteAsync(leave.EmployeeId);
            if (actor == null || approvalRoute == null)
            {
                return OperationResultModel.Fail("Onay yetkiniz doğrulanamadı.");
            }

            var decisionBy = string.IsNullOrWhiteSpace(request.DecisionBy) ? currentUser : request.DecisionBy;
            var isManagerStage = leave.Status == (int)LeaveStatus.Pending;
            var isFinalStage = leave.Status == (int)LeaveStatus.PendingFinalApproval;

            if (isManagerStage && !CanTakeManagerAction(actor, approvalRoute))
            {
                return OperationResultModel.Fail("Bu izin talebi için okul müdürü onay yetkiniz bulunmuyor.");
            }

            if (isFinalStage && !actor.IsFinalApprover)
            {
                return OperationResultModel.Fail("Bu izin talebi Genel Müdürlük onayı bekliyor.");
            }

            if (!isManagerStage && !isFinalStage)
            {
                return OperationResultModel.Fail("Bu izin talebi daha önce sonuçlandırılmış.");
            }

            bool result;
            if (isManagerStage)
            {
                leave.ManagerDecisionBy = decisionBy;
                leave.ManagerDecisionDate = DateTime.UtcNow;
                leave.Status = request.Status == (int)LeaveStatus.Approved
                    ? (int)LeaveStatus.PendingFinalApproval
                    : (int)LeaveStatus.Rejected;
                leave.UpdateDate = DateTime.Now;

                if (request.Status == (int)LeaveStatus.Rejected)
                {
                    leave.DecisionBy = decisionBy;
                    leave.DecisionDate = DateTime.UtcNow;
                }

                _leaveRepository.Update(leave);
                result = true;
            }
            else
            {
                result = await UpdateLeaveStatusAsync(request.LeaveId, request.Status, decisionBy);
            }

            if (result)
            {
                await TryLogLeaveStatusChangeAsync(
                    request,
                    "Information",
                    $"İzin durumu güncellendi. İzin Id: {request.LeaveId}, Çalışan: {employeeName}, İşlem Yapan: {currentUser}, Yeni Durum: {targetStatus}.");

                if ((request.Status == (int)LeaveStatus.Approved || request.Status == (int)LeaveStatus.Rejected) && leave != null && employeePortal != null)
                {
                    await _workflowNotificationService.NotifyLeaveRequestDecisionAsync(new LeaveRequestDecisionNotificationModel
                    {
                        LeaveId = leave.Id,
                        EmployeeName = employeeName,
                        EmployeeEmail = employeePortal.Email ?? string.Empty,
                        StartDate = leave.StartDate,
                        EndDate = leave.EndDate,
                        Reason = leave.Reason,
                        DecisionBy = decisionBy,
                        DecisionLabel = targetStatus,
                        LocationNames = approvalRoute.LocationNames,
                        IsManagerDecision = isManagerStage,
                        RegionalManagers = ToNotificationRecipients(approvalRoute.ManagerApprovers),
                        NextApprovers = isManagerStage && request.Status == (int)LeaveStatus.Approved
                            ? ToNotificationRecipients(new[] { approvalRoute.FinalApprover })
                            : new List<WorkflowNotificationRecipientModel>(),
                        TriggeredByUser = currentUser,
                        IpAddress = string.IsNullOrWhiteSpace(request.IpAddress) ? "unknown" : request.IpAddress
                    });
                }

                var successMessage = isManagerStage && request.Status == (int)LeaveStatus.Approved
                    ? "Okul müdürü onayı tamamlandı; izin talebi Genel Müdürlüğe iletildi."
                    : request.Status == (int)LeaveStatus.Approved
                        ? "İzin talebi onaylandı."
                        : "İzin talebi reddedildi.";

                return OperationResultModel.Success(successMessage);
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
            PendingLeaveCount = leaves.Count(x => IsPendingApproval(x.Status)),
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

    private async Task<decimal> CalculateRequestedDaysWithHolidaysAsync(DateTime startDate, DateTime endDate)
    {
        var holidayIntervals = await GetHolidayIntervalsAsync(startDate, endDate);
        var saturdayPolicies = (await GetSaturdayPoliciesAsync())
            .Select(x => new SaturdayLeavePolicy(x.EffectiveFrom, x.CountSaturday))
            .ToList();

        return LeaveDurationCalculator.CalculateRequestedDays(
            startDate,
            endDate,
            holidayIntervals,
            saturdayPolicies);
    }

    private async Task<List<HolidayInterval>> GetHolidayIntervalsAsync(DateTime startDate, DateTime endDate)
    {
        if (endDate < startDate)
        {
            return new List<HolidayInterval>();
        }

        var holidays = await _holidayRepository.GetAllAsync(x => x.StartDate <= endDate && x.EndDate >= startDate);

        var intervals = holidays
            .Where(x => x.EndDate > x.StartDate)
            .Select(x => new HolidayInterval(x.StartDate, x.EndDate))
            .ToList();

        intervals.AddRange(GetFixedStatutoryHolidays(startDate, endDate)
            .Select(x => x.Interval));

        return intervals;
    }

    private static IEnumerable<(string Name, HolidayInterval Interval)> GetFixedStatutoryHolidays(
        DateTime startDate,
        DateTime endDate)
    {
        for (var year = startDate.Year; year <= endDate.Year; year++)
        {
            yield return FullDay("Yılbaşı", new DateTime(year, 1, 1));
            yield return FullDay("Ulusal Egemenlik ve Çocuk Bayramı", new DateTime(year, 4, 23));
            yield return FullDay("Emek ve Dayanışma Günü", new DateTime(year, 5, 1));
            yield return FullDay("Atatürk'ü Anma, Gençlik ve Spor Bayramı", new DateTime(year, 5, 19));
            yield return FullDay("Demokrasi ve Millî Birlik Günü", new DateTime(year, 7, 15));
            yield return FullDay("Zafer Bayramı", new DateTime(year, 8, 30));
            yield return HalfDay("Cumhuriyet Bayramı Arifesi", new DateTime(year, 10, 28));
            yield return FullDay("Cumhuriyet Bayramı", new DateTime(year, 10, 29));
        }

        static (string Name, HolidayInterval Interval) FullDay(string name, DateTime day)
            => (name, new HolidayInterval(day.Date, day.Date.AddDays(1)));

        static (string Name, HolidayInterval Interval) HalfDay(string name, DateTime day)
            => (name, new HolidayInterval(day.Date.AddHours(13), day.Date.AddDays(1)));
    }

    private static void ValidateLeaveTypeSpecificRules(
        LeaveRequestValidationModel result,
        LeaveRequestCreateModel request,
        EmployeePortal? employeePortal)
    {
        var code = result.SelectedLeaveType?.Code ?? string.Empty;
        var requestedDays = result.RequestedDays;

        if (string.Equals(code, LeaveTypeCodes.Annual, StringComparison.OrdinalIgnoreCase))
        {
            if (employeePortal?.HireDate == null || employeePortal.HireDate.Value.Year <= 1900)
            {
                AddFieldError(result, "LeaveTypeId", "Yıllık izin hak edişini doğrulamak için işe giriş tarihi tanımlanmalıdır.");
            }
            else if (request.StartDate.Date < employeePortal.HireDate.Value.Date.AddYears(1))
            {
                AddFieldError(result, "StartDate", "Yıllık izin hakkı ilk çalışma yılı tamamlandıktan sonra kullanılabilir.");
            }

            if (requestedDays != 0.5m)
            {
                if (requestedDays < 5m)
                {
                    AddFieldError(
                        result,
                        "EndDate",
                        "Yıllık izin en az 10 gün blok kullanılmalıdır. Karşılıklı onayla alt sınır 5 gündür; özel durumlarda yarım gün kullanılabilir.");
                }
                else if (requestedDays < 10m && !request.MinimumBlockExceptionRequested)
                {
                    AddFieldError(
                        result,
                        "MinimumBlockExceptionRequested",
                        "5 ile 9,5 gün arasındaki yıllık izin için karşılıklı onay seçeneğini işaretleyiniz.");
                }
            }

            if (employeePortal != null && requestedDays > employeePortal.LeaveDays)
            {
                AddFieldError(result, "EndDate", "Talep edilen yıllık izin mevcut izin bakiyesini aşıyor.");
            }

            return;
        }

        var maximumDays = code.ToUpperInvariant() switch
        {
            LeaveTypeCodes.Marriage => 3m,
            LeaveTypeCodes.Bereavement => 3m,
            LeaveTypeCodes.Paternity => 5m,
            _ => 0m
        };

        if (maximumDays > 0m && requestedDays > maximumDays)
        {
            AddFieldError(
                result,
                "EndDate",
                $"{result.SelectedLeaveType?.Name} en fazla {maximumDays:0.##} gün kullanılabilir.");
        }
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
            new() { Value = (int)LeaveStatus.Pending, Label = "Okul Müdürü Onayı Bekliyor" },
            new() { Value = (int)LeaveStatus.PendingFinalApproval, Label = "Genel Müdürlük Onayı Bekliyor" },
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
            DecisionDisplay = GetDecisionDisplay(leave),
            CanCancel = IsPendingApproval(leave.Status)
        };
    }

    private static AdminLeaveRequestItemModel MapAdminLeaveRequestItem(
        Leave leave,
        IReadOnlyDictionary<int, string> employeeNames,
        ApprovalRouteModel? approvalRoute,
        bool canTakeAction)
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
            LocationNames = approvalRoute?.LocationNames ?? "-",
            ManagerDecisionDisplay = GetManagerDecisionDisplay(leave),
            MinimumBlockExceptionRequested = leave.MinimumBlockExceptionRequested,
            CanTakeAction = canTakeAction
        };
    }

    private static AdminLeaveAgreementItemModel MapAdminLeaveAgreementItem(LeaveAgreement agreement)
    {
        return new AdminLeaveAgreementItemModel
        {
            Id = agreement.Id,
            EmployeePortalId = agreement.EmployeePortalId,
            FirstName = agreement.EmployeePortal?.FirstName ?? string.Empty,
            LastName = agreement.EmployeePortal?.LastName ?? string.Empty,
            EmployeeName = BuildPortalName(agreement.EmployeePortal, agreement.EmployeePortalId),
            Email = agreement.EmployeePortal?.Email ?? string.Empty,
            PhoneNumber = agreement.EmployeePortal?.PhoneNumber ?? string.Empty,
            AgreedLeaveDays = agreement.AgreedLeaveDays,
            BalanceAsOfDate = agreement.BalanceAsOfDate,
            CurrentYearEarnedDays = agreement.CurrentYearEarnedDays,
            CurrentYearUsedDays = agreement.CurrentYearUsedDays,
            CurrentBalance = agreement.EmployeePortal?.LeaveDays ?? agreement.AgreedLeaveDays,
            IsSigned = agreement.IsSigned,
            HasAgreementPdf = !string.IsNullOrWhiteSpace(agreement.AgreementPdfFileName),
            AgreementPdfFileName = agreement.AgreementPdfFileName ?? string.Empty,
            AgreementPdfOriginalFileName = agreement.AgreementPdfOriginalFileName ?? string.Empty,
            AgreementPdfContentType = agreement.AgreementPdfContentType ?? string.Empty,
            CreatedDate = agreement.CreatedDate
        };
    }

    private static bool LeaveAgreementMatchesSearch(LeaveAgreement agreement, string searchText)
    {
        var employee = agreement.EmployeePortal;
        if (employee == null)
        {
            return false;
        }

        var normalizedSearch = searchText.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSearch))
        {
            return true;
        }

        var fullName = BuildPortalName(employee);

        return (!string.IsNullOrWhiteSpace(employee.FirstName) && employee.FirstName.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)) ||
               (!string.IsNullOrWhiteSpace(employee.LastName) && employee.LastName.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)) ||
               (!string.IsNullOrWhiteSpace(fullName) && fullName.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)) ||
               (!string.IsNullOrWhiteSpace(employee.PhoneNumber) && employee.PhoneNumber.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)) ||
               (!string.IsNullOrWhiteSpace(employee.Email) && employee.Email.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase));
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

    private static string BuildLocationNames(IEnumerable<EmployeePortalLocation>? assignments)
    {
        var names = (assignments ?? Enumerable.Empty<EmployeePortalLocation>())
            .Select(x => x.Location?.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        return names.Count == 0 ? "Atanmamış" : string.Join(", ", names);
    }

    private static int GetCompletedServiceYears(DateTime? hireDate, DateTime asOfDate)
    {
        if (!hireDate.HasValue || hireDate.Value.Year <= 1900 || hireDate.Value.Date > asOfDate.Date)
        {
            return 0;
        }

        var years = asOfDate.Year - hireDate.Value.Year;
        if (hireDate.Value.Date.AddYears(years) > asOfDate.Date)
        {
            years--;
        }

        return Math.Max(0, years);
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
            LeaveStatus.Pending => "Okul Müdürü Onayı Bekliyor",
            LeaveStatus.PendingFinalApproval => "Genel Müdürlük Onayı Bekliyor",
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
        if (IsPendingApproval(leave.Status))
        {
            return "-";
        }

        return string.IsNullOrWhiteSpace(leave.DecisionBy)
            ? "Belirtilmedi"
            : leave.DecisionBy;
    }

    private static string GetManagerDecisionDisplay(Leave leave)
    {
        if (leave.Status == (int)LeaveStatus.Pending)
        {
            return "Bekleniyor";
        }

        return string.IsNullOrWhiteSpace(leave.ManagerDecisionBy)
            ? "Doğrudan Genel Müdürlük onayına iletildi"
            : leave.ManagerDecisionBy;
    }

    private static bool IsPendingApproval(int status)
    {
        return status == (int)LeaveStatus.Pending ||
               status == (int)LeaveStatus.PendingFinalApproval;
    }

    private static string? ValidateApprovalRoute(ApprovalRouteModel? route)
    {
        if (route == null)
        {
            return "Çalışan için onay akışı oluşturulamadı.";
        }

        if (!route.HasLocation)
        {
            return "İzin talebi oluşturabilmek için kullanıcıya bir lokasyon atanmalıdır.";
        }

        if (string.IsNullOrWhiteSpace(route.FinalApprover.Email))
        {
            return "Genel Müdürlük onaylayıcısı yapılandırılmamış.";
        }

        if (!route.EmployeeIsLocationManager && route.ManagerApprovers.Count == 0)
        {
            return "Kullanıcının lokasyonu için okul müdürü tanımlanmamış.";
        }

        return null;
    }

    private static bool CanViewRequest(ApprovalActorModel actor, ApprovalRouteModel? route)
    {
        return route != null &&
               (actor.IsAdministrator || actor.IsFinalApprover || CanTakeManagerAction(actor, route));
    }

    private static bool CanTakeAction(int status, ApprovalActorModel actor, ApprovalRouteModel? route)
    {
        return route != null &&
               ((status == (int)LeaveStatus.Pending && CanTakeManagerAction(actor, route)) ||
                (status == (int)LeaveStatus.PendingFinalApproval && actor.IsFinalApprover));
    }

    private static bool CanTakeManagerAction(ApprovalActorModel actor, ApprovalRouteModel route)
    {
        return actor.IsLocationManager && route.ManagerApprovers.Any(x => EmailsEqual(x.Email, actor.Email));
    }

    private static bool EmailsEqual(string? left, string? right)
    {
        return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<ApprovalRecipientModel> GetCurrentApprovers(int status, ApprovalRouteModel? route)
    {
        if (route == null)
        {
            return Array.Empty<ApprovalRecipientModel>();
        }

        return status == (int)LeaveStatus.PendingFinalApproval
            ? new[] { route.FinalApprover }
            : route.ManagerApprovers;
    }

    private static List<WorkflowNotificationRecipientModel> ToNotificationRecipients(IEnumerable<ApprovalRecipientModel> recipients)
    {
        return recipients
            .Where(x => !string.IsNullOrWhiteSpace(x.Email))
            .GroupBy(x => x.Email.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(x => new WorkflowNotificationRecipientModel
            {
                Email = x.Key,
                DisplayName = x.First().DisplayName
            })
            .ToList();
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

    private Task LogWorkflowActionAsync(string? ipAddress, string? user, string level, string methodName, string message)
    {
        return _userActionLogService.LogAsync(new UserActionLogEntryModel
        {
            IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? "unknown" : ipAddress,
            MacAddress = null,
            User = string.IsNullOrWhiteSpace(user) ? "anonymous" : user,
            Timestamp = DateTime.UtcNow,
            Message = message,
            Level = string.IsNullOrWhiteSpace(level) ? "Information" : level,
            MethodName = string.IsNullOrWhiteSpace(methodName) ? "LeaveWorkflow" : methodName
        });
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

    private static List<LeaveAgreementImportRow> ReadLeaveAgreementRows(Stream excelStream)
    {
        using var workbook = new XLWorkbook(excelStream);
        var worksheet = workbook.Worksheets.First();
        var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var rows = new List<LeaveAgreementImportRow>();

        for (var rowNumber = 2; rowNumber <= lastRowNumber; rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            var firstName = row.Cell(1).GetString().Trim();
            var lastName = row.Cell(2).GetString().Trim();
            var email = row.Cell(3).GetString().Trim();
            var phoneNumber = row.Cell(4).GetString().Trim();
            var rawAgreedLeaveDays = row.Cell(5).GetString().Trim();
            var rawBalanceAsOfDate = row.Cell(6).GetString().Trim();
            var rawCurrentYearEarnedDays = row.Cell(7).GetString().Trim();
            var rawCurrentYearUsedDays = row.Cell(8).GetString().Trim();

            if (string.IsNullOrWhiteSpace(firstName) &&
                string.IsNullOrWhiteSpace(lastName) &&
                string.IsNullOrWhiteSpace(email) &&
                string.IsNullOrWhiteSpace(phoneNumber) &&
                string.IsNullOrWhiteSpace(rawAgreedLeaveDays) &&
                string.IsNullOrWhiteSpace(rawBalanceAsOfDate) &&
                string.IsNullOrWhiteSpace(rawCurrentYearEarnedDays) &&
                string.IsNullOrWhiteSpace(rawCurrentYearUsedDays))
            {
                continue;
            }

            rows.Add(new LeaveAgreementImportRow
            {
                RowNumber = rowNumber,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = phoneNumber,
                RawAgreedLeaveDays = rawAgreedLeaveDays,
                AgreedLeaveDays = TryReadBulkLeaveDays(row.Cell(5), out var agreedLeaveDays) ? agreedLeaveDays : null,
                BalanceAsOfDate = TryReadLeaveAgreementDate(row.Cell(6), out var balanceAsOfDate) ? balanceAsOfDate : null,
                CurrentYearEarnedDays = TryReadBulkLeaveDays(row.Cell(7), out var currentYearEarnedDays) ? currentYearEarnedDays : null,
                CurrentYearUsedDays = TryReadBulkLeaveDays(row.Cell(8), out var currentYearUsedDays) ? currentYearUsedDays : null
            });
        }

        return rows;
    }

    private static bool TryReadLeaveAgreementDate(IXLCell cell, out DateTime date)
    {
        if (cell.TryGetValue<DateTime>(out date))
        {
            date = date.Date;
            return true;
        }

        var rawValue = cell.GetString().Trim();
        return DateTime.TryParse(rawValue, CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.AllowWhiteSpaces, out date) ||
               DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out date);
    }

    private sealed class LeaveAgreementImportRow
    {
        public int RowNumber { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string RawAgreedLeaveDays { get; set; } = string.Empty;
        public decimal? AgreedLeaveDays { get; set; }
        public DateTime? BalanceAsOfDate { get; set; }
        public decimal? CurrentYearEarnedDays { get; set; }
        public decimal? CurrentYearUsedDays { get; set; }
    }
}
