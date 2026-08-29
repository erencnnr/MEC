using MEC.Application.Abstractions.Service.ApprovalWorkflowService;
using MEC.Application.Abstractions.Service.ApprovalWorkflowService.Model;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.Employee;

namespace MEC.Application.Service.ApprovalWorkflowService
{
    public class ApprovalWorkflowService : IApprovalWorkflowService
    {
        private readonly IGenericRepository<EmployeePortal> _employeePortalRepository;
        private readonly IGenericRepository<EmployeePortalLocation> _employeePortalLocationRepository;
        private readonly ApprovalWorkflowSettings _settings;

        public ApprovalWorkflowService(
            IGenericRepository<EmployeePortal> employeePortalRepository,
            IGenericRepository<EmployeePortalLocation> employeePortalLocationRepository,
            ApprovalWorkflowSettings settings)
        {
            _employeePortalRepository = employeePortalRepository;
            _employeePortalLocationRepository = employeePortalLocationRepository;
            _settings = settings;
        }

        public async Task<ApprovalActorModel?> GetActorAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var normalizedEmail = email.Trim();
            var employee = (await _employeePortalRepository.GetAllAsync(
                    x => !x.IsDeleted && x.Email == normalizedEmail))
                .FirstOrDefault();

            if (employee == null)
            {
                return null;
            }

            var locationIds = (await _employeePortalLocationRepository.GetAllAsync(
                    x => x.EmployeePortalId == employee.Id))
                .Select(x => x.LocationId)
                .Distinct()
                .ToList();

            return new ApprovalActorModel
            {
                EmployeePortalId = employee.Id,
                Email = employee.Email,
                DisplayName = BuildDisplayName(employee),
                IsAdministrator = employee.IsAdmin,
                IsLocationManager = employee.IsManager,
                IsFinalApprover = EmailsEqual(employee.Email, _settings.FinalApproverEmail),
                LocationIds = locationIds
            };
        }

        public async Task<ApprovalRouteModel?> ResolveRouteAsync(int employeePortalId)
        {
            var routes = await ResolveRoutesAsync(new[] { employeePortalId });
            return routes.GetValueOrDefault(employeePortalId);
        }

        public async Task<Dictionary<int, ApprovalRouteModel>> ResolveRoutesAsync(IEnumerable<int> employeePortalIds)
        {
            var requestedEmployeeIds = employeePortalIds
                .Distinct()
                .ToList();

            if (requestedEmployeeIds.Count == 0)
            {
                return new Dictionary<int, ApprovalRouteModel>();
            }

            var employees = (await _employeePortalRepository.GetAllAsync(
                    x => requestedEmployeeIds.Contains(x.Id) && !x.IsDeleted))
                .ToList();

            if (employees.Count == 0)
            {
                return new Dictionary<int, ApprovalRouteModel>();
            }

            var activeEmployeeIds = employees
                .Select(x => x.Id)
                .ToList();
            var employeeAssignments = (await _employeePortalLocationRepository.GetAllAsync(
                    x => activeEmployeeIds.Contains(x.EmployeePortalId),
                    x => x.Location!))
                .ToList();
            var allLocationIds = employeeAssignments
                .Select(x => x.LocationId)
                .Distinct()
                .ToList();

            var managerAssignments = allLocationIds.Count == 0
                ? new List<EmployeePortalLocation>()
                : (await _employeePortalLocationRepository.GetAllAsync(
                        x => allLocationIds.Contains(x.LocationId),
                        x => x.EmployeePortal!))
                    .ToList();

            var finalApprover = await ResolveFinalApproverAsync();
            var assignmentsByEmployee = employeeAssignments.ToLookup(x => x.EmployeePortalId);
            var managersByLocation = managerAssignments
                .Where(x => x.EmployeePortal != null &&
                            !x.EmployeePortal.IsDeleted &&
                            x.EmployeePortal.IsManager &&
                            !string.IsNullOrWhiteSpace(x.EmployeePortal.Email))
                .ToLookup(x => x.LocationId, x => x.EmployeePortal!);
            var routes = new Dictionary<int, ApprovalRouteModel>();

            foreach (var employee in employees)
            {
                var assignments = assignmentsByEmployee[employee.Id].ToList();
                var locationIds = assignments
                    .Select(x => x.LocationId)
                    .Distinct()
                    .ToList();
                var managerApprovers = locationIds
                    .SelectMany(locationId => managersByLocation[locationId])
                    .Where(x => !EmailsEqual(x.Email, employee.Email))
                    .GroupBy(x => x.Email.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.First())
                    .OrderBy(BuildDisplayName)
                    .Select(x => new ApprovalRecipientModel
                    {
                        Email = x.Email.Trim(),
                        DisplayName = BuildDisplayName(x)
                    })
                    .ToList();

                routes[employee.Id] = new ApprovalRouteModel
                {
                    EmployeePortalId = employee.Id,
                    EmployeeEmail = employee.Email,
                    EmployeeName = BuildDisplayName(employee),
                    EmployeeIsLocationManager = employee.IsManager,
                    LocationIds = locationIds,
                    LocationNames = string.Join(", ", assignments
                    .Select(x => x.Location?.Name)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)),
                    ManagerApprovers = managerApprovers,
                    FinalApprover = finalApprover
                };
            }

            return routes;
        }

        private async Task<ApprovalRecipientModel> ResolveFinalApproverAsync()
        {
            var configuredEmail = (_settings.FinalApproverEmail ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(configuredEmail))
            {
                return new ApprovalRecipientModel();
            }

            var employee = (await _employeePortalRepository.GetAllAsync(
                    x => !x.IsDeleted && x.Email == configuredEmail))
                .FirstOrDefault();

            return new ApprovalRecipientModel
            {
                Email = employee?.Email?.Trim() ?? configuredEmail,
                DisplayName = employee == null ? "Mustafa Meral" : BuildDisplayName(employee)
            };
        }

        private static string BuildDisplayName(EmployeePortal employee)
        {
            var fullName = string.Join(" ", new[] { employee.FirstName, employee.LastName }
                .Where(x => !string.IsNullOrWhiteSpace(x)))
                .Trim();

            return string.IsNullOrWhiteSpace(fullName) ? employee.Email : fullName;
        }

        private static bool EmailsEqual(string? left, string? right)
        {
            return !string.IsNullOrWhiteSpace(left)
                && !string.IsNullOrWhiteSpace(right)
                && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
