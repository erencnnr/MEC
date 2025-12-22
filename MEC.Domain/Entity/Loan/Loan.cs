using MEC.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEC.Domain.Entity.Loan
{
    public class Loan : BaseEntity
    {
        public int Id { get; set; }
        public string? Notes { get; set; }
        public DateTime LoanDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public int AssetId { get; set; }
        public Asset.Asset Asset { get; set; }
        public int AssignedToId { get; set; }

        [ForeignKey("AssignedToId")]
        public virtual Employee.Employee AssignedTo { get; set; }
        public int? AssignedById { get; set; } 

        [ForeignKey("AssignedById")]
        public virtual Employee.Employee? AssignedBy { get; set; }
        public int LoanStatusId { get; set; }
        public LoanStatus LoanStatus { get; set; }
    }
}
