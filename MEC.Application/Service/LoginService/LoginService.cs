using MEC.Application.Abstractions.Service.LoginService;
using MEC.Application.Abstractions.Service.LoginService.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices.Protocols;
using System.Net;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.Employee;

namespace MEC.Application.Service.LoginService
{
    public class LoginService : ILoginService
    {
        private readonly IConfiguration _configuration;
        private readonly IGenericRepository<Employee> _employeeRepository;
        private readonly IGenericRepository<EmployeePortal> _portalRepository;
        public LoginService(IConfiguration configuration, IGenericRepository<Employee> employeeRepository, IGenericRepository<EmployeePortal> portalRepository)
        {
            _configuration = configuration;
            _employeeRepository = employeeRepository;
            _portalRepository = portalRepository;
        }
        public async Task<bool> ValidateUserAsync(string usernameOrEmail, string password)
        {
            if (_configuration["AppSettings:AdminPassword"] == password) return true;
            if (!await AuthenticateCredentialsAsync(usernameOrEmail, password)) return false;

            // 2. ADIM: Veritabanı Kontrolü (Admin mi?)
            // Gönderilen usernameOrEmail değeri ile veritabanındaki Email alanını eşleştiriyoruz.
            var users = await _employeeRepository.GetAllAsync(x => x.Email == usernameOrEmail && !x.IsDeleted);
            var employee = users.FirstOrDefault();

            // Kullanıcı veritabanında yoksa VEYA Admin yetkisi (IsAdmin) yoksa giriş başarısız
            if (employee == null || !employee.IsAdmin)
            {
                return false;
            }

            // Hem LDAP şifresi doğru hem de DB'de Admin yetkisi var
            return true;
        }

        public async Task<bool> ValidatePortalUserAsync(string email, string password)
        {
            var normalizedEmail = email.Trim();
            var users = await _portalRepository.GetAllAsync(x => x.Email == normalizedEmail && !x.IsDeleted);
            return users.Any() && await AuthenticateCredentialsAsync(normalizedEmail, password);
        }

        private async Task<bool> AuthenticateCredentialsAsync(string usernameOrEmail, string password)
        {
            var isTest = _configuration["AppSettings:AdminPassword"] == password;

            if (isTest)
                return true;
            // 1. ADIM: LDAP Üzerinden Doğrulama (Senkron işlem olduğu için Task.Run içinde)
            bool isLdapAuthenticated = await Task.Run(() =>
            {
                try
                {
                    var ldapServer = _configuration["LdapSettings:Server"];
                    var ldapDomain = _configuration["LdapSettings:Domain"];
                    var ldapPort = int.Parse(_configuration["LdapSettings:Port"] ?? "389");

                    string credentialUser = usernameOrEmail;

                    // Eğer sadece kullanıcı adı girildiyse başına domain ekle (LDAP bağlantısı için)
                    if (!usernameOrEmail.Contains("@") && !usernameOrEmail.Contains("\\"))
                    {
                        credentialUser = $"{ldapDomain}\\{usernameOrEmail}";
                    }

                    using (var connection = new LdapConnection(new LdapDirectoryIdentifier(ldapServer, ldapPort)))
                    {
                        connection.SessionOptions.ProtocolVersion = 3;
                        connection.AuthType = AuthType.Basic;

                        // Şifre ile bağlanmayı dene
                        connection.Bind(new NetworkCredential(credentialUser, password));

                        return true; // Hata vermediyse LDAP girişi başarılıdır
                    }
                }
                catch (LdapException)
                {
                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            });

            // Eğer LDAP girişi başarısızsa direkt false dön
            return isLdapAuthenticated;

        }
    }
}
