using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEC.Application.Abstractions.Service.LoanService.Model
{
    public class LoanFilterRequestModel
    {
        public string? SearchText { get; set; }

        // Çoklu Personel Filtreleri
        public List<int>? AssignedToIds { get; set; } // Zimmet Alanlar
        public List<int>? AssignedByIds { get; set; } // Zimmet Verenler (CreatedBy)

        // Sıralama (Örn: "LoanDate_Desc", "ReturnDate_Asc")
        public string? SortOrder { get; set; } = "LoanDate_Desc";
        public string ActiveTab { get; set; } = "active";
    }
}
