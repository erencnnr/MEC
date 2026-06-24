using MEC.PDKS.Data;
using MEC.PDKS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MEC.PDKS.Controllers
{
    public class MovementsController : Controller
    {
        private const int DefaultTakeCount = 200;

        private readonly PdksDbContext _context;

        public MovementsController(PdksDbContext context)
        {
            _context = context;
        }

        [HttpGet("/Movements")]
        public async Task<IActionResult> Index(PdksMovementFilterViewModel filter)
        {
            var users = await _context.PdksUsers
                .AsNoTracking()
                .OrderBy(x => x.AdSoyad)
                .ThenBy(x => x.SicilNo)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.AdSoyad + " (" + x.SicilNo + ")"
                })
                .ToListAsync();

            var model = new PdksMovementListViewModel
            {
                Filter = filter,
                UserOptions = users
            };

            if (filter.BaslangicTarihi.HasValue && filter.BitisTarihi.HasValue &&
                filter.BaslangicTarihi.Value.Date > filter.BitisTarihi.Value.Date)
            {
                model.ErrorMessage = "Başlangıç tarihi bitiş tarihinden büyük olamaz.";
                return View(model);
            }

            var query = _context.PdksMovements
                .AsNoTracking()
                .Include(x => x.PdksUser)
                .AsQueryable();

            if (filter.KullaniciId.HasValue)
            {
                query = query.Where(x => x.PdksUserId == filter.KullaniciId.Value);
            }

            if (filter.Gun.HasValue)
            {
                var start = filter.Gun.Value.Date;
                var end = start.AddDays(1);
                query = query.Where(x => x.HareketZamani >= start && x.HareketZamani < end);
            }
            else
            {
                if (filter.BaslangicTarihi.HasValue)
                {
                    var start = filter.BaslangicTarihi.Value.Date;
                    query = query.Where(x => x.HareketZamani >= start);
                }

                if (filter.BitisTarihi.HasValue)
                {
                    var end = filter.BitisTarihi.Value.Date.AddDays(1);
                    query = query.Where(x => x.HareketZamani < end);
                }
            }

            var hasFilter = filter.KullaniciId.HasValue ||
                            filter.Gun.HasValue ||
                            filter.BaslangicTarihi.HasValue ||
                            filter.BitisTarihi.HasValue;

            query = query.OrderByDescending(x => x.HareketZamani);

            if (!hasFilter)
            {
                query = query.Take(DefaultTakeCount);
            }

            model.Items = await query
                .Select(x => new PdksMovementListItemViewModel
                {
                    Id = x.Id,
                    KullaniciId = x.PdksUserId,
                    KullaniciAdi = x.PdksUser != null ? x.PdksUser.AdSoyad : "-",
                    SicilNo = x.PdksUser != null ? x.PdksUser.SicilNo : "-",
                    HareketZamani = x.HareketZamani,
                    HareketTipi = x.HareketTipi.ToString(),
                    CihazAdi = x.CihazAdi,
                    Not = x.Not
                })
                .ToListAsync();

            return View(model);
        }
    }
}
