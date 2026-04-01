using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Domain.Entity.School;
using MEC.Portal.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MEC.Portal.Controllers
{
    [Route("Admin/Announcements")]
    public class AnnouncementController : Controller
    {
        private const int PageSize = 6;
        private readonly IAnnouncementService _announcementService;

        public AnnouncementController(IAnnouncementService announcementService)
        {
            _announcementService = announcementService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? status = "active", int page = 1)
        {
            var normalizedStatus = NormalizeStatus(status);
            var announcements = await _announcementService.GetAllAnnouncementsAsync();

            var filteredAnnouncements = announcements
                .Where(x => normalizedStatus == "passive" ? !x.IsActive : x.IsActive)
                .OrderByDescending(x => x.CreatedDate ?? DateTime.MinValue)
                .ToList();

            var totalCount = filteredAnnouncements.Count;
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
            var currentPage = Math.Min(Math.Max(page, 1), totalPages);

            var items = filteredAnnouncements
                .Skip((currentPage - 1) * PageSize)
                .Take(PageSize)
                .Select(x => new AnnouncementCardViewModel
                {
                    Id = x.Id,
                    Title = x.Title,
                    Summary = BuildSummary(x.Content),
                    CreatedDate = x.CreatedDate,
                    IsActive = x.IsActive
                })
                .ToList();

            var model = new AnnouncementListViewModel
            {
                Items = items,
                Status = normalizedStatus,
                CurrentPage = currentPage,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = PageSize
            };

            return View(model);
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View(new Announcement { IsActive = true });
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Announcement announcement)
        {
            if (!ModelState.IsValid)
            {
                return View(announcement);
            }

            await _announcementService.AddAnnouncementAsync(announcement);
            TempData["AnnouncementSuccess"] = "Duyuru kaydedildi.";
            return RedirectToAction(nameof(Index), new { status = announcement.IsActive ? "active" : "passive" });
        }

        [HttpGet("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var announcement = await _announcementService.GetAnnouncementByIdAsync(id);
            if (announcement == null)
            {
                return NotFound();
            }

            return View(announcement);
        }

        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Announcement announcement)
        {
            if (id != announcement.Id)
            {
                announcement.Id = id;
            }

            if (!ModelState.IsValid)
            {
                return View(announcement);
            }

            var currentAnnouncement = await _announcementService.GetAnnouncementByIdAsync(id);
            if (currentAnnouncement == null)
            {
                return NotFound();
            }

            currentAnnouncement.Title = announcement.Title;
            currentAnnouncement.Content = announcement.Content;
            currentAnnouncement.IsActive = announcement.IsActive;

            await _announcementService.UpdateAnnouncementAsync(currentAnnouncement);
            TempData["AnnouncementSuccess"] = "Duyuru guncellendi.";

            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpGet("{id:int}", Name = "AnnouncementDetail")]
        [HttpGet("/Announcement/{id:int}")]
        public async Task<IActionResult> Detail(int id)
        {
            var announcement = await _announcementService.GetAnnouncementByIdAsync(id);
            if (announcement == null)
            {
                return NotFound();
            }

            return View(announcement);
        }

        [HttpPost("Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _announcementService.DeleteAnnouncementAsync(id);
            return RedirectToAction(nameof(Index));
        }

        private static string NormalizeStatus(string? status)
        {
            return string.Equals(status, "passive", StringComparison.OrdinalIgnoreCase)
                ? "passive"
                : "active";
        }

        private static string BuildSummary(string? htmlContent)
        {
            if (string.IsNullOrWhiteSpace(htmlContent))
            {
                return "Icerik bulunmuyor.";
            }

            var plainText = Regex.Replace(htmlContent, "<.*?>", " ");
            plainText = Regex.Replace(plainText, @"\s+", " ").Trim();

            if (plainText.Length <= 180)
            {
                return plainText;
            }

            return $"{plainText[..177]}...";
        }
    }
}
