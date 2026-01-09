using MEC.Application.Abstractions.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEC.Application.Abstractions.Service.LdapService
{
    public interface ILdapService : IApplicationService
    {
        Task<int> SyncUsersFromLdapAsync();
    }
}
