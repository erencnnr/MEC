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
        private readonly IGenericRepository<EmployeePortalChild> _childRepository;
        private readonly IGenericRepository<Location> _locationRepository;
        private readonly IGenericRepository<Leave> _leaveRepository;

        public EmployeePortalService(
            IGenericRepository<EmployeePortal> repository,
            IGenericRepository<EmployeePortalChild> childRepository,
            IGenericRepository<Location> locationRepository,
            IGenericRepository<Leave> leaveRepository)
        {
            _repository = repository;
            _childRepository = childRepository;
            _locationRepository = locationRepository;
            _leaveRepository = leaveRepository;
        }

        public async Task<EmployeePortal> GetProfileByEmailAsync(string email)
        {
            var results = await _repository.GetAllAsync(
                x => x.Email == email && !x.IsDeleted,
                x => x.Location!,
                x => x.Children);

            return results.FirstOrDefault();
        }

        public async Task<List<EmployeePortal>> GetActivePortalUsersAsync()
        {
            return (await _repository.GetAllAsync(x => !x.IsDeleted, x => x.Location!))
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

            return (await _repository.GetAllAsync(x => !x.IsDeleted && x.Email == email, x => x.Location!)).FirstOrDefault();
        }

        public async Task<ProfileSummaryModel> GetProfileSummaryByEmailAsync(string email)
        {
            var profile = await GetProfileByEmailAsync(email);
            var model = new ProfileSummaryModel
            {
                Profile = profile
            };

            if (string.IsNullOrWhiteSpace(email) || profile == null)
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

            var portalUsers = (await _repository.GetAllAsync(null, x => x.Location!))
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
            var portalUser = (await _repository.GetAllAsync(
                x => x.Id == id,
                x => x.Location!,
                x => x.Children)).FirstOrDefault();

            return portalUser == null ? null : MapPortalUserEditModel(portalUser);
        }

        public async Task<PortalSelfEditModel?> GetSelfProfileEditAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var portalUser = (await _repository.GetAllAsync(
                x => !x.IsDeleted && x.Email == email,
                x => x.Location!,
                x => x.Children)).FirstOrDefault();

            return portalUser == null ? null : MapSelfProfileEditModel(portalUser);
        }

        public async Task<OperationResultModel> UpdatePortalUserAsync(PortalUserEditModel model)
        {
            if (string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                return OperationResultModel.Fail("Telefon alanı zorunludur.");
            }

            var childValidation = NormalizeChildren(model.Children);
            if (!childValidation.IsSuccess)
            {
                return OperationResultModel.Fail(childValidation.Message);
            }

            var portalUser = await _repository.GetByIdAsync(model.Id);
            if (portalUser == null)
            {
                return OperationResultModel.Fail("Portal kullanıcısı bulunamadı.");
            }

            portalUser.FirstName = model.FirstName.Trim();
            portalUser.LastName = model.LastName.Trim();
            portalUser.Email = model.Email.Trim();
            portalUser.PhoneNumber = model.PhoneNumber.Trim();
            portalUser.Title = model.Title.Trim();
            portalUser.HireDate = model.HireDate;
            portalUser.BirthDate = model.BirthDate;
            portalUser.LocationId = model.LocationId;
            portalUser.LeaveDays = model.LeaveDays;
            portalUser.IsAdmin = model.IsAdmin;
            portalUser.IsDeleted = model.IsDeleted;
            portalUser.AddressText = NormalizeOptionalText(model.AddressText);
            portalUser.MaritalStatus = model.MaritalStatus;
            portalUser.EducationUniversity = NormalizeOptionalText(model.EducationUniversity);
            portalUser.EducationFaculty = NormalizeOptionalText(model.EducationFaculty);
            portalUser.EducationDepartment = NormalizeOptionalText(model.EducationDepartment);
            portalUser.UpdateDate = DateTime.Now;

            _repository.Update(portalUser);
            await ReplaceChildrenAsync(model.Id, childValidation.Children);
            return OperationResultModel.Success("Portal kullanıcısı güncellendi.");
        }

        public async Task<OperationResultModel> UpdateSelfProfileAsync(string email, PortalSelfEditModel model)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return OperationResultModel.Fail("Aktif portal kullanıcısı bulunamadı.");
            }

            if (string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                return OperationResultModel.Fail("Telefon alanı zorunludur.");
            }

            var childValidation = NormalizeChildren(model.Children);
            if (!childValidation.IsSuccess)
            {
                return OperationResultModel.Fail(childValidation.Message);
            }

            var portalUser = await GetActivePortalUserByEmailAsync(email);
            if (portalUser == null)
            {
                return OperationResultModel.Fail("Aktif portal kullanıcısı bulunamadı.");
            }

            portalUser.PhoneNumber = model.PhoneNumber.Trim();
            portalUser.AddressText = NormalizeOptionalText(model.AddressText);
            portalUser.MaritalStatus = model.MaritalStatus;
            portalUser.EducationUniversity = NormalizeOptionalText(model.EducationUniversity);
            portalUser.EducationFaculty = NormalizeOptionalText(model.EducationFaculty);
            portalUser.EducationDepartment = NormalizeOptionalText(model.EducationDepartment);
            portalUser.UpdateDate = DateTime.Now;

            _repository.Update(portalUser);
            await ReplaceChildrenAsync(portalUser.Id, childValidation.Children);
            return OperationResultModel.Success("Profil bilgileriniz güncellendi.");
        }

        public async Task<bool> RequiresProfileCompletionAsync(string email)
        {
            var portalUser = await GetActivePortalUserByEmailAsync(email);
            return portalUser != null && string.IsNullOrWhiteSpace(portalUser.PhoneNumber);
        }

        public async Task<List<LocationOptionModel>> GetLocationOptionsAsync()
        {
            return (await _locationRepository.GetAllAsync())
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .Select(x => new LocationOptionModel
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();
        }

        private async Task ReplaceChildrenAsync(int employeePortalId, List<PortalUserChildEditModel> children)
        {
            var existingChildren = (await _childRepository.GetAllAsync(x => x.EmployeePortalId == employeePortalId)).ToList();
            foreach (var child in existingChildren)
            {
                _childRepository.Delete(child);
            }

            foreach (var child in children)
            {
                await _childRepository.AddAsync(new EmployeePortalChild
                {
                    EmployeePortalId = employeePortalId,
                    Gender = child.Gender!.Value,
                    BirthDate = child.BirthDate!.Value.Date,
                    CreatedDate = DateTime.Now,
                    UpdateDate = DateTime.Now
                });
            }
        }

        private static ChildNormalizationResult NormalizeChildren(IEnumerable<PortalUserChildEditModel>? children)
        {
            var normalizedChildren = new List<PortalUserChildEditModel>();
            foreach (var child in children ?? Enumerable.Empty<PortalUserChildEditModel>())
            {
                var hasGender = child.Gender.HasValue;
                var hasBirthDate = child.BirthDate.HasValue;

                if (!hasGender && !hasBirthDate)
                {
                    continue;
                }

                if (!hasGender || !hasBirthDate)
                {
                    return ChildNormalizationResult.Fail("Her çocuk kaydında cinsiyet ve doğum tarihi birlikte girilmelidir.");
                }

                normalizedChildren.Add(new PortalUserChildEditModel
                {
                    Gender = child.Gender,
                    BirthDate = child.BirthDate.Value.Date
                });
            }

            return ChildNormalizationResult.Success(normalizedChildren);
        }

        private static string? NormalizeOptionalText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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
                Title = portalUser.Title,
                LocationId = portalUser.LocationId,
                LocationName = portalUser.Location?.Name ?? string.Empty,
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
                Title = portalUser.Title,
                HireDate = portalUser.HireDate,
                BirthDate = portalUser.BirthDate,
                LocationId = portalUser.LocationId,
                LeaveDays = portalUser.LeaveDays,
                IsAdmin = portalUser.IsAdmin,
                IsDeleted = portalUser.IsDeleted,
                AddressText = portalUser.AddressText,
                MaritalStatus = portalUser.MaritalStatus,
                EducationUniversity = portalUser.EducationUniversity,
                EducationFaculty = portalUser.EducationFaculty,
                EducationDepartment = portalUser.EducationDepartment,
                Children = portalUser.Children
                    .OrderBy(x => x.BirthDate)
                    .Select(MapChildModel)
                    .ToList()
            };
        }

        private static PortalSelfEditModel MapSelfProfileEditModel(EmployeePortal portalUser)
        {
            return new PortalSelfEditModel
            {
                Id = portalUser.Id,
                FirstName = portalUser.FirstName,
                LastName = portalUser.LastName,
                Email = portalUser.Email,
                Title = portalUser.Title,
                HireDate = portalUser.HireDate,
                BirthDate = portalUser.BirthDate,
                PhoneNumber = portalUser.PhoneNumber,
                AddressText = portalUser.AddressText,
                MaritalStatus = portalUser.MaritalStatus,
                EducationUniversity = portalUser.EducationUniversity,
                EducationFaculty = portalUser.EducationFaculty,
                EducationDepartment = portalUser.EducationDepartment,
                Children = portalUser.Children
                    .OrderBy(x => x.BirthDate)
                    .Select(MapChildModel)
                    .ToList()
            };
        }

        private static PortalUserChildEditModel MapChildModel(EmployeePortalChild child)
        {
            return new PortalUserChildEditModel
            {
                Gender = child.Gender,
                BirthDate = child.BirthDate
            };
        }

        private sealed class ChildNormalizationResult
        {
            public bool IsSuccess { get; private init; }
            public string Message { get; private init; } = string.Empty;
            public List<PortalUserChildEditModel> Children { get; private init; } = new();

            public static ChildNormalizationResult Success(List<PortalUserChildEditModel> children)
            {
                return new ChildNormalizationResult
                {
                    IsSuccess = true,
                    Children = children
                };
            }

            public static ChildNormalizationResult Fail(string message)
            {
                return new ChildNormalizationResult
                {
                    IsSuccess = false,
                    Message = message
                };
            }
        }
    }
}
