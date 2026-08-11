using MEC.Application.Abstractions.Service.LdapService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Common;
using MEC.Domain.Entity.Employee;
using Microsoft.Extensions.Configuration;
using System.DirectoryServices.Protocols;
using System.Net;

namespace MEC.Application.Service.LdapService
{
    public class LdapService : ILdapService
    {
        private readonly IGenericRepository<Employee> _employeeRepository;
        private readonly IGenericRepository<EmployeePortal> _employeePortalRepository;
        private readonly IConfiguration _configuration;

        public LdapService(
            IGenericRepository<Employee> employeeRepository,
            IGenericRepository<EmployeePortal> employeePortalRepository,
            IConfiguration configuration)
        {
            _employeeRepository = employeeRepository;
            _employeePortalRepository = employeePortalRepository;
            _configuration = configuration;
        }

        public async Task<int> SyncUsersFromLdapAsync()
        {
            try
            {
                var ldapUsers = GetActiveLdapUsers();
                var processedCount = 0;

                foreach (var ldapUser in ldapUsers)
                {
                    var users = await _employeeRepository.GetAllAsync(x => x.Email == ldapUser.EffectiveEmail);
                    var existingUser = users.FirstOrDefault();

                    if (existingUser != null)
                    {
                        existingUser.FirstName = ldapUser.FirstName;
                        existingUser.LastName = ldapUser.LastName;
                        existingUser.Phone = ldapUser.Phone;
                        existingUser.IsDeleted = false;
                        existingUser.UpdateDate = DateTime.Now;

                        _employeeRepository.Update(existingUser);
                    }
                    else
                    {
                        var newEmployee = new Employee
                        {
                            FirstName = ldapUser.FirstName,
                            LastName = ldapUser.LastName,
                            Email = ldapUser.EffectiveEmail,
                            Phone = ldapUser.Phone,
                            CreatedDate = DateTime.Now,
                            IsDeleted = false
                        };

                        await _employeeRepository.AddAsync(newEmployee);
                    }

                    processedCount++;
                }

                return processedCount;
            }
            catch (Exception ex)
            {
                throw new Exception($"LDAP Senkronizasyon hatası: {ex.Message}");
            }
        }

        public async Task<int> SyncPortalUsersFromLdapAsync()
        {
            try
            {
                var ldapUsers = GetActiveLdapUsers();
                var processedCount = 0;

                foreach (var ldapUser in ldapUsers)
                {
                    var portalUsers = await _employeePortalRepository.GetAllAsync(x => x.Email == ldapUser.EffectiveEmail);
                    var existingPortalUser = portalUsers.FirstOrDefault();

                    if (existingPortalUser != null)
                    {
                        existingPortalUser.FirstName = ldapUser.FirstName;
                        existingPortalUser.LastName = ldapUser.LastName;
                        existingPortalUser.PhoneNumber = ldapUser.Phone;
                        existingPortalUser.Email = ldapUser.EffectiveEmail;
                        existingPortalUser.Title ??= string.Empty;
                        existingPortalUser.IsDeleted = false;
                        existingPortalUser.TerminationDate = null;
                        existingPortalUser.UpdateDate = DateTime.Now;

                        _employeePortalRepository.Update(existingPortalUser);
                    }
                    else
                    {
                        var newPortalUser = new EmployeePortal
                        {
                            FirstName = ldapUser.FirstName,
                            LastName = ldapUser.LastName,
                            PhoneNumber = ldapUser.Phone,
                            Email = ldapUser.EffectiveEmail,
                            Title = string.Empty,
                            HireDate = null,
                            TerminationDate = null,
                            BirthDate = null,
                            LeaveDays = 0,
                            CreatedDate = DateTime.Now,
                            IsDeleted = false
                        };

                        await _employeePortalRepository.AddAsync(newPortalUser);
                    }

                    processedCount++;
                }

                return processedCount;
            }
            catch (Exception ex)
            {
                throw new Exception($"LDAP portal kullanıcı senkronizasyon hatası: {ex.Message}");
            }
        }

        private List<LdapUserModel> GetActiveLdapUsers()
        {
            var server = _configuration["LdapSettings:Server"];
            var port = int.Parse(_configuration["LdapSettings:Port"] ?? "389");
            var searchBase = _configuration["LdapSettings:SearchBase"];
            var bindUser = _configuration["LdapSettings:BindUser"];
            var bindPass = _configuration["LdapSettings:BindPass"];

            using var connection = new LdapConnection(new LdapDirectoryIdentifier(server, port));
            connection.SessionOptions.ProtocolVersion = 3;
            connection.AuthType = AuthType.Basic;

            if (!string.IsNullOrEmpty(bindUser) && !string.IsNullOrEmpty(bindPass))
            {
                connection.Bind(new NetworkCredential(bindUser, bindPass));
            }
            else
            {
                connection.Bind();
            }

            const string filter = "(&(objectClass=user)(objectCategory=person)(!userAccountControl:1.2.840.113556.1.4.803:=2))";
            string[] attributes = { "sAMAccountName", "mail", "givenName", "sn", "displayName", "telephoneNumber" };

            var searchRequest = new SearchRequest(
                searchBase,
                filter,
                SearchScope.Subtree,
                attributes);

            var response = (SearchResponse)connection.SendRequest(searchRequest);
            var users = new List<LdapUserModel>();

            foreach (SearchResultEntry entry in response.Entries)
            {
                var username = GetAttributeValue(entry, "sAMAccountName");
                var email = GetAttributeValue(entry, "mail");
                var firstName = GetAttributeValue(entry, "givenName");
                var lastName = GetAttributeValue(entry, "sn");
                var displayName = GetAttributeValue(entry, "displayName");
                var phone = GetAttributeValue(entry, "telephoneNumber");
                var effectiveEmail = !string.IsNullOrWhiteSpace(email) ? email : username;

                if (string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(displayName))
                {
                    var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        firstName = parts[0];
                        lastName = parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : string.Empty;
                    }
                }

                if (string.IsNullOrWhiteSpace(firstName))
                {
                    firstName = username;
                }

                if (string.IsNullOrWhiteSpace(lastName))
                {
                    lastName = "-";
                }

                if (string.IsNullOrWhiteSpace(effectiveEmail))
                {
                    continue;
                }

                users.Add(new LdapUserModel
                {
                    Username = username ?? string.Empty,
                    EffectiveEmail = effectiveEmail,
                    FirstName = TurkishNameFormatter.Format(firstName),
                    LastName = TurkishNameFormatter.Format(lastName),
                    Phone = phone ?? string.Empty
                });
            }

            return users;
        }

        private static string? GetAttributeValue(SearchResultEntry entry, string attributeName)
        {
            if (entry.Attributes.Contains(attributeName) && entry.Attributes[attributeName].Count > 0)
            {
                var value = entry.Attributes[attributeName][0];
                if (value is string stringValue)
                {
                    return stringValue;
                }

                if (value is byte[] bytes)
                {
                    return System.Text.Encoding.UTF8.GetString(bytes);
                }

                return value.ToString();
            }

            return null;
        }

        private sealed class LdapUserModel
        {
            public string Username { get; set; } = string.Empty;
            public string EffectiveEmail { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
        }
    }
}
