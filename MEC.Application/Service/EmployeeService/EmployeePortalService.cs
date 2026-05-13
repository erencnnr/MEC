using MEC.Application.Abstractions.Common.Models;
using MEC.Application.Abstractions.Service.EmployeeService;
using MEC.Application.Abstractions.Service.EmployeeService.Model;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Common;
using MEC.Domain.Common.Enum;
using MEC.Domain.Entity.Employee;
using MEC.Domain.Entity.Leave;

namespace MEC.Application.Service.EmployeeService
{
    public class EmployeePortalService : IEmployeePortalService
    {
        private readonly IGenericRepository<EmployeePortal> _repository;
        private readonly IGenericRepository<Leave> _leaveRepository;

        public EmployeePortalService(
            IGenericRepository<EmployeePortal> repository,
            IGenericRepository<Leave> leaveRepository)
        {
            _repository = repository;
            _leaveRepository = leaveRepository;
        }

        public async Task<EmployeePortal> GetProfileByEmailAsync(string email)
        {
            var results = await _repository.GetAllAsync(x => x.Email == email && !x.IsDeleted);
            return results.FirstOrDefault();
        }

        public async Task<List<EmployeePortal>> GetActivePortalUsersAsync()
        {
            return (await _repository.GetAllAsync(x => !x.IsDeleted))
                .OrderBy(x => x.FirstName)
                .ThenBy(x => x.LastName)
                .ThenBy(x => x.Email)
                .ToList();
        }

        public async Task<EmployeePortal?> GetActivePortalUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            return (await _repository.GetAllAsync(x => !x.IsDeleted && x.Email == email)).FirstOrDefault();
        }

        public async Task<ProfileSummaryModel> GetProfileSummaryByEmailAsync(string email)
        {
            var profile = await GetProfileByEmailAsync(email);
            var model = new ProfileSummaryModel
            {
                Profile = profile
            };

            if (string.IsNullOrWhiteSpace(email))
            {
                return model;
            }

            if (profile == null)
            {
                return model;
            }

            var employeeLeaves = (await _leaveRepository.GetAllAsync(x => x.EmployeeId == profile.Id, x => x.LeaveType))
                .OrderByDescending(x => x.CreatedDate)
                .ThenByDescending(x => x.Id)
                .ToList();

            var pendingAnnualLeaves = employeeLeaves
                .Where(x => x.Status == (int)LeaveStatus.Pending)
                .Where(IsAnnualLeave)
                .ToList();

            model.PendingAnnualLeaveCount = pendingAnnualLeaves.Count;
            model.PendingAnnualLeaveDays = pendingAnnualLeaves.Sum(GetRequestedDays);
            return model;
        }

        public async Task<PortalUserListResultModel> GetPortalUsersAsync(PortalUserListQueryModel query)
        {
            var normalizedStatus = NormalizePortalUserStatus(query.Status);
            var currentPage = query.Page < 1 ? 1 : query.Page;
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

            var portalUsers = (await _repository.GetAllAsync())
                .Where(x => normalizedStatus == "passive" ? x.IsDeleted : !x.IsDeleted)
                .OrderBy(x => x.FirstName)
                .ThenBy(x => x.LastName)
                .ThenBy(x => x.Email)
                .ToList();

            var totalCount = portalUsers.Count;
            var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
            currentPage = Math.Min(currentPage, totalPages);

            return new PortalUserListResultModel
            {
                Status = normalizedStatus,
                CurrentPage = currentPage,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = pageSize,
                Items = portalUsers
                    .Skip((currentPage - 1) * pageSize)
                    .Take(pageSize)
                    .Select(MapPortalUserListItem)
                    .ToList()
            };
        }

        public async Task<PortalUserEditModel?> GetPortalUserEditAsync(int id)
        {
            var portalUser = await _repository.GetByIdAsync(id);
            return portalUser == null ? null : MapPortalUserEditModel(portalUser);
        }

        public async Task<OperationResultModel> UpdatePortalUserAsync(PortalUserEditModel model)
        {
            var portalUser = await _repository.GetByIdAsync(model.Id);
            if (portalUser == null)
            {
                return OperationResultModel.Fail("Portal kullanıcısı bulunamadı.");
            }

            portalUser.FirstName = model.FirstName.Trim();
            portalUser.LastName = model.LastName.Trim();
            portalUser.Email = model.Email.Trim();
            portalUser.PhoneNumber = model.PhoneNumber.Trim();
            portalUser.HireDate = model.HireDate;
            portalUser.BirthDate = model.BirthDate;
            portalUser.LeaveDays = model.LeaveDays;
            portalUser.IsAdmin = model.IsAdmin;
            portalUser.IsDeleted = model.IsDeleted;
            portalUser.UpdateDate = DateTime.Now;

            _repository.Update(portalUser);
            return OperationResultModel.Success("Portal kullanıcısı güncellendi.");
        }

        private static bool IsAnnualLeave(Leave leave)
        {
            return string.Equals(leave.LeaveType?.Code, LeaveTypeCodes.Annual, StringComparison.OrdinalIgnoreCase);
        }

        private static decimal GetRequestedDays(Leave leave)
        {
            return leave.RequestedDays > 0
                ? leave.RequestedDays
                : LeaveDurationCalculator.CalculateRequestedDays(leave.StartDate, leave.EndDate);
        }

        private static string NormalizePortalUserStatus(string? status)
        {
            return string.Equals(status, "passive", StringComparison.OrdinalIgnoreCase)
                ? "passive"
                : "active";
        }

        private static string BuildPortalName(EmployeePortal portal)
        {
            var fullName = string.Join(" ", new[] { portal.FirstName, portal.LastName }
                .Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

            return !string.IsNullOrWhiteSpace(fullName)
                ? fullName
                : portal.Email;
        }

        private static PortalUserListItemModel MapPortalUserListItem(EmployeePortal portalUser)
        {
            return new PortalUserListItemModel
            {
                Id = portalUser.Id,
                FullName = BuildPortalName(portalUser),
                Email = portalUser.Email,
                PhoneNumber = portalUser.PhoneNumber,
                LeaveDays = portalUser.LeaveDays,
                HireDate = portalUser.HireDate,
                IsAdmin = portalUser.IsAdmin,
                IsDeleted = portalUser.IsDeleted
            };
        }

        private static PortalUserEditModel MapPortalUserEditModel(EmployeePortal portalUser)
        {
            return new PortalUserEditModel
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
    }
}
