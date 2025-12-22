using MEC.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEC.Domain.Entity.Invoice
{
    public class Invoice : BaseEntity
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public string? Supplier { get; set; }
        public DateTime? InvoiceDate { get; set; }
    }
}
