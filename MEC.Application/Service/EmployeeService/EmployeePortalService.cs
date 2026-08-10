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
        private readonly IGenericRepository<EmployeePortalLocation> _employeePortalLocationRepository;
        private readonly IGenericRepository<Location> _locationRepository;
        private readonly IGenericRepository<Leave> _leaveRepository;

        public EmployeePortalService(
            IGenericRepository<EmployeePortal> repository,
            IGenericRepository<EmployeePortalChild> childRepository,
            IGenericRepository<EmployeePortalLocation> employeePortalLocationRepository,
            IGenericRepository<Location> locationRepository,
            IGenericRepository<Leave> leaveRepository)
        {
            _repository = repository;
            _childRepository = childRepository;
            _employeePortalLocationRepository = employeePortalLocationRepository;
            _locationRepository = locationRepository;
            _leaveRepository = leaveRepository;
        }

        public async Task<EmployeePortal> GetProfileByEmailAsync(string email)
        {
            var results = (await _repository.GetAllAsync(
                x => x.Email == email && !x.IsDeleted,
                x => x.Children))
                .ToList();

            await HydrateLocationAssignmentsAsync(results);
            return results.FirstOrDefault();
        }

        public async Task<List<EmployeePortal>> GetActivePortalUsersAsync()
        {
            var portalUsers = (await _repository.GetAllAsync(x => !x.IsDeleted))
                .OrderBy(x => x.FirstName)
                .ThenBy(x => x.LastName)
                .ThenBy(x => x.Email)
                .ToList();

            await HydrateLocationAssignmentsAsync(portalUsers);
            return portalUsers;
        }

        public async Task<EmployeePortal?> GetActivePortalUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var portalUsers = (await _repository.GetAllAsync(x => !x.IsDeleted && x.Email == email)).ToList();
            await HydrateLocationAssignmentsAsync(portalUsers);
            return portalUsers.FirstOrDefault();
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
            var normalizedSearchTerm = NormalizeOptionalText(query.SearchTerm) ?? string.Empty;
            var currentPage = query.Page < 1 ? 1 : query.Page;
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

            var portalUsers = (await _repository.GetAllAsync())
                .Where(x => normalizedStatus == "passive" ? x.IsDeleted : !x.IsDeleted)
                .Where(x => string.IsNullOrEmpty(normalizedSearchTerm) || MatchesPortalUserSearch(x, normalizedSearchTerm))
                .OrderBy(x => x.FirstName)
                .ThenBy(x => x.LastName)
                .ThenBy(x => x.Email)
                .ToList();

            await HydrateLocationAssignmentsAsync(portalUsers);

            var totalCount = portalUsers.Count;
            var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
            currentPage = Math.Min(currentPage, totalPages);

            return new PortalUserListResultModel
            {
                Status = normalizedStatus,
                SearchTerm = normalizedSearchTerm,
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
                x => x.Children))
                .FirstOrDefault();

            if (portalUser != null)
            {
                await HydrateLocationAssignmentsAsync(new[] { portalUser });
            }

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
                x => x.Children))
                .FirstOrDefault();

            if (portalUser != null)
            {
                await HydrateLocationAssignmentsAsync(new[] { portalUser });
            }

            return portalUser == null ? null : MapSelfProfileEditModel(portalUser);
        }

        public async Task<OperationResultModel> UpdatePortalUserAsync(PortalUserEditModel model)
        {
            if (string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                return OperationResultModel.Fail("Telefon alanÄ± zorunludur.");
            }

            var childValidation = NormalizeChildren(model.Children);
            if (!childValidation.IsSuccess)
            {
                return OperationResultModel.Fail(childValidation.Message);
            }

            var normalizedLocationIds = NormalizeLocationIds(model.LocationIds);

            var portalUser = await _repository.GetByIdAsync(model.Id);
            if (portalUser == null)
            {
                return OperationResultModel.Fail("Portal kullanÄ±cÄ±sÄ± bulunamadÄ±.");
            }

            portalUser.FirstName = model.FirstName.Trim();
            portalUser.LastName = model.LastName.Trim();
            portalUser.Email = model.Email.Trim();
            portalUser.PhoneNumber = NormalizePhoneNumber(model.PhoneNumber);
            portalUser.Title = model.Title.Trim();
            portalUser.HireDate = NormalizeOptionalDate(model.HireDate);
            portalUser.BirthDate = NormalizeOptionalDate(model.BirthDate);
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
            await ReplaceLocationsAsync(model.Id, normalizedLocationIds);
            await ReplaceChildrenAsync(model.Id, childValidation.Children);
            return OperationResultModel.Success("Portal kullanÄ±cÄ±sÄ± gÃ¼ncellendi.");
        }

        public async Task<OperationResultModel> UpdateSelfProfileAsync(string email, PortalSelfEditModel model)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return OperationResultModel.Fail("Aktif portal kullanÄ±cÄ±sÄ± bulunamadÄ±.");
            }

            if (string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                return OperationResultModel.Fail("Telefon alanÄ± zorunludur.");
            }

            var childValidation = NormalizeChildren(model.Children);
            if (!childValidation.IsSuccess)
            {
                return OperationResultModel.Fail(childValidation.Message);
            }

            var portalUser = await GetActivePortalUserByEmailAsync(email);
            if (portalUser == null)
            {
                return OperationResultModel.Fail("Aktif portal kullanÄ±cÄ±sÄ± bulunamadÄ±.");
            }

            portalUser.PhoneNumber = NormalizePhoneNumber(model.PhoneNumber);
            portalUser.AddressText = NormalizeOptionalText(model.AddressText);
            portalUser.MaritalStatus = model.MaritalStatus;
            portalUser.EducationUniversity = NormalizeOptionalText(model.EducationUniversity);
            portalUser.EducationFaculty = NormalizeOptionalText(model.EducationFaculty);
            portalUser.EducationDepartment = NormalizeOptionalText(model.EducationDepartment);
            portalUser.UpdateDate = DateTime.Now;

            _repository.Update(portalUser);
            await ReplaceChildrenAsync(portalUser.Id, childValidation.Children);
            return OperationResultModel.Success("Profil bilgileriniz gÃ¼ncellendi.");
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
                    EducationStatus = child.EducationStatus,
                    CreatedDate = DateTime.Now,
                    UpdateDate = DateTime.Now
                });
            }
        }

        private async Task ReplaceLocationsAsync(int employeePortalId, List<int> locationIds)
        {
            var existingLocations = (await _employeePortalLocationRepository.GetAllAsync(x => x.EmployeePortalId == employeePortalId)).ToList();
            foreach (var existingLocation in existingLocations)
            {
                _employeePortalLocationRepository.Delete(existingLocation);
            }

            foreach (var locationId in locationIds)
            {
                await _employeePortalLocationRepository.AddAsync(new EmployeePortalLocation
                {
                    EmployeePortalId = employeePortalId,
                    LocationId = locationId,
                    CreatedDate = DateTime.Now,
                    UpdateDate = DateTime.Now
                });
            }
        }

        private async Task HydrateLocationAssignmentsAsync(IEnumerable<EmployeePortal> portalUsers)
        {
            var portalUserList = portalUsers
                .Where(x => x != null)
                .ToList();

            if (!portalUserList.Any())
            {
                return;
            }

            var portalUserIds = portalUserList
                .Select(x => x.Id)
                .Distinct()
                .ToList();

            var locationAssignments = (await _employeePortalLocationRepository.GetAllAsync(
                x => portalUserIds.Contains(x.EmployeePortalId),
                x => x.Location!))
                .GroupBy(x => x.EmployeePortalId)
                .ToDictionary(
                    x => x.Key,
                    x => (ICollection<EmployeePortalLocation>)x
                        .OrderBy(item => item.Location!.Name)
                        .ThenBy(item => item.LocationId)
                        .ToList());

            foreach (var portalUser in portalUserList)
            {
                portalUser.EmployeePortalLocations = locationAssignments.TryGetValue(portalUser.Id, out var assignments)
                    ? assignments
                    : new List<EmployeePortalLocation>();
            }
        }

        private static ChildNormalizationResult NormalizeChildren(IEnumerable<PortalUserChildEditModel>? children)
        {
            var normalizedChildren = new List<PortalUserChildEditModel>();
            foreach (var child in children ?? Enumerable.Empty<PortalUserChildEditModel>())
            {
                var hasGender = child.Gender.HasValue;
                var hasBirthDate = child.BirthDate.HasValue;
                var hasEducationStatus = child.EducationStatus.HasValue;

                if (!hasGender && !hasBirthDate && !hasEducationStatus)
                {
                    continue;
                }

                if (!hasGender || !hasBirthDate || !hasEducationStatus)
                {
                    return ChildNormalizationResult.Fail("Her çocuk kaydında cinsiyet, doğum tarihi ve eğitim durumu birlikte girilmelidir.");
                }

                normalizedChildren.Add(new PortalUserChildEditModel
                {
                    Gender = child.Gender,
                    BirthDate = child.BirthDate.Value.Date,
                    EducationStatus = child.EducationStatus
                });
            }

            return ChildNormalizationResult.Success(normalizedChildren);
        }

        private static string? NormalizeOptionalText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string NormalizePhoneNumber(string value)
        {
            return new string(value.Where(char.IsDigit).ToArray());
        }

        private static DateTime? NormalizeOptionalDate(DateTime? value)
        {
            return value.HasValue && value.Value.Year > 1000
                ? value.Value.Date
                : null;
        }

        private static List<int> NormalizeLocationIds(IEnumerable<int>? locationIds)
        {
            return (locationIds ?? Enumerable.Empty<int>())
                .Where(x => x > 0)
                .Distinct()
                .ToList();
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

        private static bool MatchesPortalUserSearch(EmployeePortal portalUser, string searchTerm)
        {
            var fullName = BuildPortalName(portalUser);
            if (fullName.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase) ||
                ContainsSearchTerm(portalUser.FirstName, searchTerm, StringComparison.CurrentCultureIgnoreCase) ||
                ContainsSearchTerm(portalUser.LastName, searchTerm, StringComparison.CurrentCultureIgnoreCase) ||
                ContainsSearchTerm(portalUser.Email, searchTerm, StringComparison.OrdinalIgnoreCase) ||
                ContainsSearchTerm(portalUser.PhoneNumber, searchTerm, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var searchDigits = new string(searchTerm.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(searchDigits))
            {
                return false;
            }

            var phoneDigits = new string((portalUser.PhoneNumber ?? string.Empty).Where(char.IsDigit).ToArray());
            return phoneDigits.Contains(searchDigits, StringComparison.Ordinal);
        }

        private static bool ContainsSearchTerm(string? value, string searchTerm, StringComparison comparison)
        {
            return !string.IsNullOrWhiteSpace(value) && value.Contains(searchTerm, comparison);
        }

        private static string BuildPortalName(EmployeePortal portal)
        {
            var fullName = string.Join(" ", new[] { portal.FirstName, portal.LastName }
                .Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

            return !string.IsNullOrWhiteSpace(fullName)
                ? fullName
                : portal.Email;
        }

        private static string BuildLocationNames(EmployeePortal portalUser)
        {
            var names = portalUser.EmployeePortalLocations
                .Select(x => x.Location?.Name)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            return names.Any()
                ? string.Join(", ", names)
                : string.Empty;
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
                LocationIds = portalUser.EmployeePortalLocations
                    .Select(x => x.LocationId)
                    .Distinct()
                    .ToList(),
                LocationNames = BuildLocationNames(portalUser),
                LeaveDays = portalUser.LeaveDays,
                HireDate = NormalizeOptionalDate(portalUser.HireDate),
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
                HireDate = NormalizeOptionalDate(portalUser.HireDate),
                BirthDate = NormalizeOptionalDate(portalUser.BirthDate),
                LocationIds = portalUser.EmployeePortalLocations
                    .Select(x => x.LocationId)
                    .Distinct()
                    .ToList(),
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
                HireDate = NormalizeOptionalDate(portalUser.HireDate),
                BirthDate = NormalizeOptionalDate(portalUser.BirthDate),
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
                BirthDate = child.BirthDate,
                EducationStatus = child.EducationStatus
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
