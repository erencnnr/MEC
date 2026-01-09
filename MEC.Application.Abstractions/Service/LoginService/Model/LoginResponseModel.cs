using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEC.Application.Abstractions.Service.LoginService.Model
{
    public class LoginResponseModel
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public string UserName { get; set; }  // AD'deki samAccountName
        public string DisplayName { get; set; }
        public string Email { get; set; }

        public List<string> Roles { get; set; } = new List<string>();
    }
}
