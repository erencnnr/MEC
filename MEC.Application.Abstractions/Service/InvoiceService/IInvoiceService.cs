using MEC.Application.Abstractions.Application;
using MEC.Domain.Entity.Invoice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEC.Application.Abstractions.Service.InvoiceService
{
    public interface IInvoiceService : IApplicationService
    {
        Task<List<Invoice>> GetInvoiceListAsync();
    }
}
