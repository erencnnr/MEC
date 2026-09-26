using MEC.Application.Abstractions.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEC.Application.Abstractions.Service.LoginService
{
    public interface ILoginService : IApplicationService
    {
        Task<bool> ValidatePortalUserAsync(string email, string password);
        Task<bool> ValidateUserAsync(string usernameOrEmail, string password);
    }
}
