using MEC.Application.Abstractions.Service.InvoiceService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.Invoice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEC.Application.Service.InvoiceService
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IGenericRepository<Invoice> _repository;

        public InvoiceService(IGenericRepository<Invoice> repository)
        {
            _repository = repository;
        }

        public async Task<List<Invoice>> GetInvoiceListAsync()
        {
            var invoices = await _repository.GetAllAsync();
            return invoices.ToList();
        }
    }
}
