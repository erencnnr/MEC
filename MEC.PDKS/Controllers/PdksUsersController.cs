using MEC.PDKS.Data;
using MEC.PDKS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MEC.PDKS.Controllers
{
    public class PdksUsersController : Controller
    {
        private readonly PdksDbContext _context;

        public PdksUsersController(PdksDbContext context)
        {
            _context = context;
        }

        [HttpGet("/PdksUsers")]
        public async Task<IActionResult> Index()
        {
            var users = await _context.PdksUsers
                .AsNoTracking()
                .OrderBy(x => x.AdSoyad)
                .ThenBy(x => x.SicilNo)
                .Select(x => new PdksUserListItemViewModel
                {
                    Id = x.Id,
                    SicilNo = x.SicilNo,
                    AdSoyad = x.AdSoyad,
                    AktifMi = x.AktifMi
                })
                .ToListAsync();

            return View(new PdksUserListViewModel
            {
                Items = users
            });
        }
    }
}
