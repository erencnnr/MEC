using MEC.Application.Abstractions.Service.SchoolService;
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
        private readonly ISliderService _sliderService;
        private readonly ISliderImageApiClient _sliderImageApiClient;

        public HomeController(
            ILogger<HomeController> logger,
            IAnnouncementService announcementService,
            ISliderService sliderService,
            ISliderImageApiClient sliderImageApiClient)
        {
            _logger = logger;
            _announcementService = announcementService;
            _sliderService = sliderService;
            _sliderImageApiClient = sliderImageApiClient;
        }

        public async Task<IActionResult> Index()
        {
            var activeAnnouncements = (await _announcementService.GetActiveAnnouncementsAsync())
                .OrderByDescending(x => x.CreatedDate)
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
            var activeAnnouncements = await _announcementService.GetActiveAnnouncementsAsync();
            var sortedAnnouncements = activeAnnouncements.OrderByDescending(x => x.CreatedDate).ToList();
            return View(sortedAnnouncements);
        }
    }
}
