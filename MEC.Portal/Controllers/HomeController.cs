using MEC.Application.Abstractions.Service.SchoolService;
using MEC.DAL.Config.Abstractions.Common;
using MEC.Domain.Entity.School;
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
        private readonly IGenericRepository<SliderImage> _sliderImageRepository;
        private readonly ISliderImageApiClient _sliderImageApiClient;

        public HomeController(
            ILogger<HomeController> logger,
            IAnnouncementService announcementService,
            IGenericRepository<SliderImage> sliderImageRepository,
            ISliderImageApiClient sliderImageApiClient)
        {
            _logger = logger;
            _announcementService = announcementService;
            _sliderImageRepository = sliderImageRepository;
            _sliderImageApiClient = sliderImageApiClient;
        }

        public async Task<IActionResult> Index()
        {
            var activeAnnouncements = (await _announcementService.GetActiveAnnouncementsAsync())
                .OrderByDescending(x => x.CreatedDate)
                .ToList();

            var sliderItems = (await _sliderImageRepository.GetAllAsync())
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.CreatedDate)
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
