using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEC.Domain.Common
{
    public class PagedResult<T>
    {
        public List<T> Results { get; set; }
        public int CurrentPage { get; set; }
        public int PageCount { get; set; }
        public int PageSize { get; set; }
        public int RowCount { get; set; }

        public PagedResult()
        {
            Results = new List<T>();
        }
    }
}
