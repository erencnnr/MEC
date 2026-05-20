using MEC.Application.Abstractions.Service.EmployeeService;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Domain.Common.Enum;
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

        public HomeController(
            ILogger<HomeController> logger,
            IAnnouncementService announcementService,
            IEmployeePortalService employeePortalService,
            ISliderService sliderService,
            ISliderImageApiClient sliderImageApiClient)
        {
            _logger = logger;
            _announcementService = announcementService;
            _employeePortalService = employeePortalService;
            _sliderService = sliderService;
            _sliderImageApiClient = sliderImageApiClient;
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

            return View(new HomeIndexViewModel
            {
                Announcements = activeAnnouncements,
                News = activeNews,
                Employees = employees,
                SliderItems = sliderItems
            });
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
    }
}
