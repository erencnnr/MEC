using MEC.Application.Abstractions.Service.LdapService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.Employee;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MEC.Application.Service.LdapService
{
    public class LdapService : ILdapService
    {
        private readonly IGenericRepository<Employee> _employeeRepository;
        private readonly IConfiguration _configuration;
        public LdapService(IGenericRepository<Employee> employeeRepository, IConfiguration configuration)
        {
            _employeeRepository = employeeRepository;
            _configuration = configuration;
        }
        public async Task<int> SyncUsersFromLdapAsync()
        {
            var server = _configuration["LdapSettings:Server"];
            var port = int.Parse(_configuration["LdapSettings:Port"] ?? "389");
            var searchBase = _configuration["LdapSettings:SearchBase"]; // Örn: DC=domain,DC=com
            var bindUser = _configuration["LdapSettings:BindUser"]; // domain\user
            var bindPass = _configuration["LdapSettings:BindPass"];

            int processedCount = 0;

            try
            {
                using (var connection = new LdapConnection(new LdapDirectoryIdentifier(server, port)))
                {
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

                    // Arama Filtresi: Aktif Kullanıcılar
                    // objectClass=user: Kullanıcılar
                    // !userAccountControl:2: Pasif (Disabled) olmayanlar
                    string filter = "(&(objectClass=user)(objectCategory=person)(!userAccountControl:1.2.840.113556.1.4.803:=2))";

                    string[] attributes = { "sAMAccountName", "mail", "givenName", "sn", "displayName", "telephoneNumber" };

                    var searchRequest = new SearchRequest(
                        searchBase,
                        filter,
                        SearchScope.Subtree,
                        attributes
                    );

                    // Paging (Sayfalama) Kontrolü - Çok fazla kullanıcı varsa hepsini çekmek için
                    // Basit olması adına şu an standart Search yapıyoruz. 
                    // Eğer 1000'den fazla kullanıcı varsa PageResultRequestControl eklenmelidir.

                    var response = (SearchResponse)connection.SendRequest(searchRequest);

                    foreach (SearchResultEntry entry in response.Entries)
                    {
                        // 1. Verileri LDAP'tan Oku
                        string username = GetAttributeValue(entry, "sAMAccountName");
                        string email = GetAttributeValue(entry, "mail");
                        string firstName = GetAttributeValue(entry, "givenName");
                        string lastName = GetAttributeValue(entry, "sn");
                        string displayName = GetAttributeValue(entry, "displayName");
                        string phone = GetAttributeValue(entry, "telephoneNumber");

                        // Eğer Email boşsa Username kullan
                        string effectiveEmail = !string.IsNullOrEmpty(email) ? email : username;

                        // Ad Soyad Ayrıştırma Mantığı
                        if (string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(displayName))
                        {
                            var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 0)
                            {
                                firstName = parts[0];
                                lastName = parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : "";
                            }
                        }

                        // Son kontrol: İsim hala yoksa username ver
                        if (string.IsNullOrEmpty(firstName)) firstName = username;
                        if (string.IsNullOrEmpty(lastName)) lastName = "-";

                        // 2. Veritabanı İşlemleri
                        // Email (veya username) ile eşleşen var mı?
                        var users = await _employeeRepository.GetAllAsync(x => x.Email == effectiveEmail);
                        var existingUser = users.FirstOrDefault();

                        if (existingUser != null)
                        {
                            // --- UPDATE ---
                            existingUser.FirstName = firstName;
                            existingUser.LastName = lastName;
                            existingUser.Phone = phone;
                            existingUser.IsDeleted = false; // Varsa aktif et
                            existingUser.UpdateDate = DateTime.Now;

                            _employeeRepository.Update(existingUser);
                        }
                        else
                        {
                            // --- INSERT ---
                            var newEmployee = new Employee
                            {
                                FirstName = firstName,
                                LastName = lastName,
                                Email = effectiveEmail,
                                Phone = phone,
                                CreatedDate = DateTime.Now,
                                IsDeleted = false,
                                // EmployeeTypeId null kalabilir veya varsayılan bir tip atanabilir
                            };

                            await _employeeRepository.AddAsync(newEmployee);
                        }

                        processedCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                // Loglama yapılabilir
                throw new Exception($"LDAP Senkronizasyon hatası: {ex.Message}");
            }

            return processedCount;
        }

        // Yardımcı Metot: Attribute değerini güvenli çekmek için
        private string GetAttributeValue(SearchResultEntry entry, string attributeName)
        {
            if (entry.Attributes.Contains(attributeName) && entry.Attributes[attributeName].Count > 0)
            {
                var value = entry.Attributes[attributeName][0];
                if (value is string strVal) return strVal;
                if (value is byte[] bytes) return System.Text.Encoding.UTF8.GetString(bytes);
                return value.ToString();
            }
            return null;
        }
    }
}
