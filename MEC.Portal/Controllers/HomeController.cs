using MEC.Application.Abstractions.Service.EmployeeService;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Domain.Common.Enum;
using MEC.Domain.Entity.Employee;
using MEC.Portal.Models;
using MEC.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Linq;

namespace MEC.Portal.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IAnnouncementService _announcementService;
        private readonly IEmployeePortalService _employeePortalService;
        private readonly ISliderService _sliderService;
        private readonly ISliderImageApiClient _sliderImageApiClient;
        private readonly IBirthdayPopupService _birthdayPopupService;
        private readonly IBirthdayPopupImageApiClient _birthdayPopupImageApiClient;

        public HomeController(
            ILogger<HomeController> logger,
            IAnnouncementService announcementService,
            IEmployeePortalService employeePortalService,
            ISliderService sliderService,
            ISliderImageApiClient sliderImageApiClient,
            IBirthdayPopupService birthdayPopupService,
            IBirthdayPopupImageApiClient birthdayPopupImageApiClient)
        {
            _logger = logger;
            _announcementService = announcementService;
            _employeePortalService = employeePortalService;
            _sliderService = sliderService;
            _sliderImageApiClient = sliderImageApiClient;
            _birthdayPopupService = birthdayPopupService;
            _birthdayPopupImageApiClient = birthdayPopupImageApiClient;
        }

        public async Task<IActionResult> Index()
        {
            var activeAnnouncements = (await _announcementService.GetActiveAnnouncementsAsync(AnnouncementContentType.Announcement))
                .OrderByDescending(x => x.CreatedDate)
                .ToList();
            var activeNews = (await _announcementService.GetActiveAnnouncementsAsync(AnnouncementContentType.News))
                .OrderByDescending(x => x.CreatedDate)
                .ToList();
            var employees = (await _employeePortalService.GetActivePortalUsersAsync())
                .Select(x => new HomeEmployeeDirectoryItemViewModel
                {
                    Id = x.Id,
                    FullName = string.Join(" ", new[] { x.FirstName, x.LastName }.Where(value => !string.IsNullOrWhiteSpace(value))).Trim(),
                    Email = x.Email ?? string.Empty,
                    PhoneNumber = x.PhoneNumber ?? string.Empty,
                    HireDate = x.HireDate
                })
                .OrderBy(x => x.FullName)
                .ThenBy(x => x.Email)
                .ToList();

            var sliderItems = (await _sliderService.GetSliderImagesAsync())
                .Select(x => new HomeSliderItemViewModel
                {
                    Id = x.Id,
                    FileName = x.OriginalFileName,
                    ImageUrl = _sliderImageApiClient.GetFileUrl(x.FileName)
                })
                .ToList();

            var birthdayPopup = await BuildBirthdayPopupAsync();

            return View(new HomeIndexViewModel
            {
                Announcements = activeAnnouncements,
                News = activeNews,
                Employees = employees,
                SliderItems = sliderItems,
                ShowBirthdayPopup = birthdayPopup.ShouldShow,
                BirthdayPopupImageUrl = birthdayPopup.ImageUrl
            });
        }

        [HttpPost("/Home/BirthdayPopup/Seen")]
        public async Task<IActionResult> MarkBirthdayPopupSeen()
        {
            var portalUser = await GetCurrentPortalUserAsync();
            if (portalUser == null || !IsBirthdayToday(portalUser.BirthDate))
            {
                return BadRequest(new { message = "Aktif doğum günü popup kaydı oluşturulamadı." });
            }

            var activeImage = await _birthdayPopupService.GetActiveBirthdayPopupImageAsync();
            if (activeImage == null)
            {
                return BadRequest(new { message = "Aktif doğum günü popup görseli bulunamadı." });
            }

            var result = await _birthdayPopupService.MarkBirthdayPopupAsSeenAsync(portalUser.Id, DateTime.Today.Year);
            return result.IsSuccess
                ? Ok(new { message = "ok" })
                : BadRequest(new { message = result.Message });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet("/Announcements")]
        public async Task<IActionResult> Announcements()
        {
            var activeAnnouncements = await _announcementService.GetActiveAnnouncementsAsync(AnnouncementContentType.Announcement);
            var sortedAnnouncements = activeAnnouncements.OrderByDescending(x => x.CreatedDate).ToList();
            return View(sortedAnnouncements);
        }

        [HttpGet("/News")]
        public async Task<IActionResult> News()
        {
            var activeNews = await _announcementService.GetActiveAnnouncementsAsync(AnnouncementContentType.News);
            var sortedNews = activeNews.OrderByDescending(x => x.CreatedDate).ToList();
            return View(sortedNews);
        }

        private async Task<(bool ShouldShow, string ImageUrl)> BuildBirthdayPopupAsync()
        {
            var portalUser = await GetCurrentPortalUserAsync();
            if (portalUser == null || !IsBirthdayToday(portalUser.BirthDate))
            {
                return (false, string.Empty);
            }

            var activeImage = await _birthdayPopupService.GetActiveBirthdayPopupImageAsync();
            if (activeImage == null)
            {
                return (false, string.Empty);
            }

            var hasSeenThisYear = await _birthdayPopupService.HasSeenBirthdayPopupAsync(portalUser.Id, DateTime.Today.Year);
            if (hasSeenThisYear)
            {
                return (false, string.Empty);
            }

            return (true, _birthdayPopupImageApiClient.GetFileUrl(activeImage.FileName));
        }

        private async Task<EmployeePortal?> GetCurrentPortalUserAsync()
        {
            var email = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            return await _employeePortalService.GetActivePortalUserByEmailAsync(email);
        }

        private static bool IsBirthdayToday(DateTime? birthDate)
        {
            if (!birthDate.HasValue || birthDate.Value.Year <= 1000)
            {
                return false;
            }

            var today = DateTime.Today;
            return birthDate.Value.Month == today.Month && birthDate.Value.Day == today.Day;
        }
    }
}
