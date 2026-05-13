using MEC.Application.Abstractions.Common.Models;
using MEC.Application.Abstractions.Service.LoggingService;
using MEC.Application.Abstractions.Service.LoggingService.Model;
using MEC.Application.Abstractions.Service.OvertimeService;
using MEC.Application.Abstractions.Service.OvertimeService.Model;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Common.Enum;
using MEC.Domain.Entity.Employee;
using MEC.Domain.Entity.Overtime;

namespace MEC.Application.Service.OvertimeService
{
    public class OvertimeService : IOvertimeService
    {
        private readonly IGenericRepository<OvertimeRequest> _overtimeRequestRepository;
        private readonly IGenericRepository<EmployeePortal> _employeePortalRepository;
        private readonly IUserActionLogService _userActionLogService;

        public OvertimeService(
            IGenericRepository<OvertimeRequest> overtimeRequestRepository,
            IGenericRepository<EmployeePortal> employeePortalRepository,
            IUserActionLogService userActionLogService)
        {
            _overtimeRequestRepository = overtimeRequestRepository;
            _employeePortalRepository = employeePortalRepository;
            _userActionLogService = userActionLogService;
        }

        public async Task<OvertimeRequestValidationModel> ValidateOvertimeRequestAsync(OvertimeRequestCreateModel request)
        {
            var result = new OvertimeRequestValidationModel
            {
                IsSuccess = true,
                Level = "success"
            };

            if (request.StartDate == default)
            {
                AddFieldError(result, "Date", "Mesai tarihi zorunludur.");
            }

            if (request.EndDate <= request.StartDate)
            {
                AddFieldError(result, "EndTime", "Bitiş saati başlangıç saatinden sonra olmalıdır.");
            }

            if (request.StartDate.Date != request.EndDate.Date)
            {
                AddFieldError(result, "EndTime", "Mesai talebi yalnızca aynı gün içinde oluşturulabilir.");
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                AddFieldError(result, "Reason", "Mesai açıklaması zorunludur.");
            }

            if (result.FieldErrors.Count == 0)
            {
                result.RequestedHours = CalculateRequestedHours(request.StartDate, request.EndDate);

                if (result.RequestedHours <= 0)
                {
                    AddFieldError(result, "EndTime", "Geçerli bir mesai süresi hesaplanamadı.");
                }
            }

            result.IsSuccess = result.FieldErrors.Count == 0;
            result.Level = result.IsSuccess ? "success" : "danger";
            result.Message = result.IsSuccess ? string.Empty : "Mesai talebi doğrulanamadı.";
            result.Errors = result.FieldErrors.Values.ToList();

            return result;
        }

        public async Task<OperationResultModel<OvertimeRequestCreateResultModel>> CreateOvertimeRequestAsync(OvertimeRequestCreateModel request)
        {
            var employeePortal = await GetActiveEmployeePortalByEmailAsync(request.UserEmail);
            if (employeePortal == null)
            {
                return OperationResultModel<OvertimeRequestCreateResultModel>.Fail("Kullanıcı kaydı bulunamadı.");
            }

            var validation = await ValidateOvertimeRequestAsync(request);
            if (!validation.IsSuccess)
            {
                return OperationResultModel<OvertimeRequestCreateResultModel>.Fail(
                    validation.FieldErrors.Values.FirstOrDefault() ?? "Mesai talebi doğrulanamadı.");
            }

            var overtimeRequest = new OvertimeRequest
            {
                EmployeePortalId = employeePortal.Id,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                RequestedHours = validation.RequestedHours,
                Reason = request.Reason.Trim(),
                Status = (int)OvertimeStatus.Pending,
                CreatedDate = DateTime.Now
            };

            await _overtimeRequestRepository.AddAsync(overtimeRequest);

            return OperationResultModel<OvertimeRequestCreateResultModel>.Success(
                new OvertimeRequestCreateResultModel
                {
                    OvertimeRequestId = overtimeRequest.Id
                },
                "Mesai talebiniz başarıyla gönderildi.");
        }

        public async Task<OvertimeHistoryResultModel> GetOvertimeHistoryAsync(OvertimeHistoryQueryModel query)
        {
            var currentYear = DateTime.Today.Year;
            var selectedSort = NormalizeSort(query.Sort);

            if (string.IsNullOrWhiteSpace(query.UserEmail))
            {
                return CreateEmptyHistoryResult(currentYear, query, selectedSort, "Kullanıcı kaydı bulunamadı.");
            }

            var employeePortal = await GetActiveEmployeePortalByEmailAsync(query.UserEmail);
            if (employeePortal == null)
            {
                return CreateEmptyHistoryResult(currentYear, query, selectedSort, "Kullanıcı kaydı bulunamadı.");
            }

            var allRequests = (await _overtimeRequestRepository.GetAllAsync(
                    x => x.EmployeePortalId == employeePortal.Id,
                    x => x.EmployeePortal!))
                .ToList();

            var filteredRequests = allRequests.AsEnumerable();

            if (query.Year.HasValue)
            {
                filteredRequests = filteredRequests.Where(x => x.StartDate.Year == query.Year.Value);
            }

            if (query.Status.HasValue && Enum.IsDefined(typeof(OvertimeStatus), query.Status.Value))
            {
                filteredRequests = filteredRequests.Where(x => x.Status == query.Status.Value);
            }

            filteredRequests = selectedSort switch
            {
                "created_asc" => filteredRequests.OrderBy(x => x.CreatedDate ?? x.StartDate).ThenBy(x => x.Id),
                _ => filteredRequests.OrderByDescending(x => x.CreatedDate ?? x.StartDate).ThenByDescending(x => x.Id)
            };

            var items = filteredRequests
                .Select(MapHistoryItem)
                .ToList();

            var totalCount = items.Count;
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
            var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (decimal)pageSize);
            var currentPage = Math.Min(Math.Max(query.Page, 1), totalPages);
            var pagedItems = items
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new OvertimeHistoryResultModel
            {
                Items = pagedItems,
                YearOptions = CreateYearOptions(allRequests, currentYear),
                StatusOptions = CreateStatusOptions(),
                SelectedYear = query.Year,
                SelectedStatus = query.Status,
                SelectedSort = selectedSort,
                CurrentPage = currentPage,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = pageSize
            };
        }

        public async Task<OvertimeHistoryItemModel?> GetOvertimeHistoryDetailAsync(string userEmail, int overtimeRequestId)
        {
            var employeePortal = await GetActiveEmployeePortalByEmailAsync(userEmail);
            if (employeePortal == null)
            {
                return null;
            }

            var overtimeRequest = (await _overtimeRequestRepository.GetAllAsync(
                    x => x.Id == overtimeRequestId && x.EmployeePortalId == employeePortal.Id,
                    x => x.EmployeePortal!))
                .FirstOrDefault();

            return overtimeRequest == null ? null : MapHistoryItem(overtimeRequest);
        }

        public async Task<PagedResultModel<AdminOvertimeRequestItemModel>> GetAdminOvertimeRequestsAsync(AdminOvertimeRequestListQueryModel query)
        {
            var overtimeRequests = (await _overtimeRequestRepository.GetAllAsync(x => true, x => x.EmployeePortal!))
                .ToList();

            var filteredRequests = overtimeRequests.AsEnumerable();

            if (query.Status.HasValue && Enum.IsDefined(typeof(OvertimeStatus), query.Status.Value))
            {
                filteredRequests = filteredRequests.Where(x => x.Status == query.Status.Value);
            }

            var items = filteredRequests
                .OrderByDescending(x => x.CreatedDate ?? x.StartDate)
                .ThenByDescending(x => x.Id)
                .Select(MapAdminItem)
                .ToList();

            var totalCount = items.Count;
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
            var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (decimal)pageSize);
            var currentPage = Math.Min(Math.Max(query.Page, 1), totalPages);
            var pagedItems = items
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResultModel<AdminOvertimeRequestItemModel>
            {
                Items = pagedItems,
                CurrentPage = currentPage,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = pageSize
            };
        }

        public async Task<AdminOvertimeRequestItemModel?> GetAdminOvertimeRequestDetailAsync(int id)
        {
            var overtimeRequest = (await _overtimeRequestRepository.GetAllAsync(x => x.Id == id, x => x.EmployeePortal!))
                .FirstOrDefault();

            return overtimeRequest == null ? null : MapAdminItem(overtimeRequest);
        }

        public async Task<OperationResultModel> UpdateOvertimeStatusAsync(OvertimeStatusUpdateRequestModel request)
        {
            if (!Enum.IsDefined(typeof(OvertimeStatus), request.Status))
            {
                return OperationResultModel.Fail("Geçersiz mesai durumu.");
            }

            var overtimeRequest = (await _overtimeRequestRepository.GetAllAsync(x => x.Id == request.OvertimeRequestId, x => x.EmployeePortal!))
                .FirstOrDefault();

            if (overtimeRequest == null)
            {
                return OperationResultModel.Fail("Mesai talebi bulunamadı.");
            }

            overtimeRequest.Status = request.Status;
            overtimeRequest.UpdateDate = DateTime.Now;

            if (request.Status == (int)OvertimeStatus.Pending)
            {
                overtimeRequest.DecisionBy = null;
                overtimeRequest.DecisionDate = null;
            }
            else
            {
                overtimeRequest.DecisionBy = string.IsNullOrWhiteSpace(request.DecisionBy)
                    ? request.CurrentUser
                    : request.DecisionBy;
                overtimeRequest.DecisionDate = DateTime.UtcNow;
            }

            _overtimeRequestRepository.Update(overtimeRequest);

            var employeeName = BuildEmployeeName(overtimeRequest.EmployeePortal);
            var statusLabel = GetStatusLabel(request.Status);
            await _userActionLogService.LogAsync(new UserActionLogEntryModel
            {
                IpAddress = request.IpAddress,
                MacAddress = null,
                User = request.CurrentUser,
                Timestamp = DateTime.UtcNow,
                Level = "Information",
                MethodName = request.MethodName,
                Message = $"Mesai talebi güncellendi. TalepId: {overtimeRequest.Id}, Personel: {employeeName}, YeniDurum: {statusLabel}, Baslangic: {overtimeRequest.StartDate:dd.MM.yyyy HH:mm}, Bitis: {overtimeRequest.EndDate:dd.MM.yyyy HH:mm}, Sure: {overtimeRequest.RequestedHours:0.##} saat, Aciklama: {overtimeRequest.Reason}"
            });

            return OperationResultModel.Success("Mesai talebi güncellendi.");
        }

        private async Task<EmployeePortal?> GetActiveEmployeePortalByEmailAsync(string email)
        {
            return (await _employeePortalRepository.GetAllAsync(x => x.Email == email && !x.IsDeleted)).FirstOrDefault();
        }

        private static decimal CalculateRequestedHours(DateTime startDate, DateTime endDate)
        {
            if (endDate <= startDate)
            {
                return 0;
            }

            return Math.Round((decimal)(endDate - startDate).TotalMinutes / 60m, 2, MidpointRounding.AwayFromZero);
        }

        private static void AddFieldError(OvertimeRequestValidationModel result, string fieldName, string errorMessage)
        {
            if (!result.FieldErrors.ContainsKey(fieldName))
            {
                result.FieldErrors[fieldName] = errorMessage;
            }
        }

        private static string NormalizeSort(string? sort)
        {
            return string.Equals(sort, "created_asc", StringComparison.OrdinalIgnoreCase)
                ? "created_asc"
                : "created_desc";
        }

        private static List<int> CreateYearOptions(List<OvertimeRequest> overtimeRequests, int currentYear)
        {
            if (!overtimeRequests.Any())
            {
                return Enumerable.Range(currentYear - 5, 6).Reverse().ToList();
            }

            var oldestYear = overtimeRequests.Min(x => x.StartDate.Year);
            return Enumerable.Range(oldestYear, currentYear - oldestYear + 1)
                .Reverse()
                .ToList();
        }

        private static OvertimeHistoryResultModel CreateEmptyHistoryResult(
            int currentYear,
            OvertimeHistoryQueryModel query,
            string selectedSort,
            string errorMessage)
        {
            return new OvertimeHistoryResultModel
            {
                YearOptions = Enumerable.Range(currentYear - 5, 6).Reverse().ToList(),
                StatusOptions = CreateStatusOptions(),
                SelectedYear = query.Year,
                SelectedStatus = query.Status,
                SelectedSort = selectedSort,
                CurrentPage = 1,
                TotalPages = 1,
                TotalCount = 0,
                PageSize = query.PageSize <= 0 ? 10 : query.PageSize,
                ErrorMessage = errorMessage
            };
        }

        private static OvertimeHistoryItemModel MapHistoryItem(OvertimeRequest overtimeRequest)
        {
            return new OvertimeHistoryItemModel
            {
                Id = overtimeRequest.Id,
                EmployeePortalId = overtimeRequest.EmployeePortalId,
                EmployeeName = BuildEmployeeName(overtimeRequest.EmployeePortal),
                StartDate = overtimeRequest.StartDate,
                EndDate = overtimeRequest.EndDate,
                RequestedHours = overtimeRequest.RequestedHours,
                Reason = overtimeRequest.Reason,
                Status = overtimeRequest.Status,
                CreatedDate = overtimeRequest.CreatedDate,
                StatusLabel = GetStatusLabel(overtimeRequest.Status),
                StatusTone = GetStatusTone(overtimeRequest.Status),
                DecisionDisplay = GetDecisionDisplay(overtimeRequest)
            };
        }

        private static AdminOvertimeRequestItemModel MapAdminItem(OvertimeRequest overtimeRequest)
        {
            var item = MapHistoryItem(overtimeRequest);

            return new AdminOvertimeRequestItemModel
            {
                Id = item.Id,
                EmployeePortalId = item.EmployeePortalId,
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
                CanTakeAction = overtimeRequest.Status == (int)OvertimeStatus.Pending
            };
        }

        private static string BuildEmployeeName(EmployeePortal? employeePortal)
        {
            if (employeePortal == null)
            {
                return "Bilinmeyen Kullanıcı";
            }

            var fullName = string.Join(" ", new[] { employeePortal.FirstName, employeePortal.LastName }
                .Where(x => !string.IsNullOrWhiteSpace(x)))
                .Trim();

            return string.IsNullOrWhiteSpace(fullName)
                ? employeePortal.Email
                : fullName;
        }

        private static string GetDecisionDisplay(OvertimeRequest overtimeRequest)
        {
            if (!string.IsNullOrWhiteSpace(overtimeRequest.DecisionBy))
            {
                return overtimeRequest.DecisionBy;
            }

            return overtimeRequest.Status == (int)OvertimeStatus.Pending ? "-" : "Belirtilmedi";
        }

        private static string GetStatusLabel(int status)
        {
            return ToOvertimeStatus(status) switch
            {
                OvertimeStatus.Approved => "Onaylandı",
                OvertimeStatus.Rejected => "Reddedildi",
                OvertimeStatus.Cancelled => "İptal",
                _ => "Onay Bekliyor"
            };
        }

        private static string GetStatusTone(int status)
        {
            return ToOvertimeStatus(status) switch
            {
                OvertimeStatus.Approved => "approved",
                OvertimeStatus.Rejected => "rejected",
                OvertimeStatus.Cancelled => "cancelled",
                _ => "pending"
            };
        }

        private static OvertimeStatus ToOvertimeStatus(int status)
        {
            return Enum.IsDefined(typeof(OvertimeStatus), status)
                ? (OvertimeStatus)status
                : OvertimeStatus.Pending;
        }

        private static List<OvertimeStatusOptionModel> CreateStatusOptions()
        {
            return new List<OvertimeStatusOptionModel>
            {
                new() { Value = (int)OvertimeStatus.Pending, Label = "Onay Bekliyor" },
                new() { Value = (int)OvertimeStatus.Approved, Label = "Onaylandı" },
                new() { Value = (int)OvertimeStatus.Rejected, Label = "Reddedildi" },
                new() { Value = (int)OvertimeStatus.Cancelled, Label = "İptal" }
            };
        }
    }
}
