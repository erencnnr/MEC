using MEC.Application.Abstractions.Application;
using MEC.Domain.Entity.School;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEC.Application.Abstractions.Service.SchoolService
{
    public interface ISchoolClassService : IApplicationService
    {
        Task<List<SchoolClass>> GetSchoolClassListAsync();
    }
}
