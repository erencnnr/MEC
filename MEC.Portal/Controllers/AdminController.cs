using MEC.Application.Abstractions.Common.Models;
using MEC.Application.Abstractions.Service.EmployeeService;
using MEC.Application.Abstractions.Service.EmployeeService.Model;
using MEC.Application.Abstractions.Service.LeaveService;
using MEC.Application.Abstractions.Service.LeaveService.Model;
using MEC.Application.Abstractions.Service.SchoolService;
using MEC.Application.Abstractions.Service.SchoolService.Model;
using MEC.Domain.Common.Enum;
using MEC.Portal.Models;
using MEC.Portal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;
using System.IO;
using System.Text;

namespace MEC.Portal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private const string UpdateLeaveStatusMethodName = "UpdateLeaveStatus";
        private const long MaxLeaveAgreementUploadSizeBytes = 10 * 1024 * 1024;
        private const long MaxLeaveAgreementPdfUploadSizeBytes = 10 * 1024 * 1024;
        private const string LeaveAgreementPdfUploadFolder = "uploads/leave-agreements";
        private const int LeaveRequestsPageSize = 10;
        private const int LeaveAgreementsPageSize = 10;
        private const int PortalUsersPageSize = 10;
        private const long MaxFoodMenuUploadSizeBytes = 15 * 1024 * 1024;

        private readonly ILeaveService _leaveService;
        private readonly IAnnouncementService _announcementService;
        private readonly IEmployeePortalService _employeePortalService;
        private readonly ISliderService _sliderService;
        private readonly ISliderImageApiClient _sliderImageApiClient;
        private readonly IBirthdayPopupService _birthdayPopupService;
        private readonly IBirthdayPopupImageApiClient _birthdayPopupImageApiClient;
        private readonly IFoodMenuService _foodMenuService;
        private readonly ISurveyService _surveyService;
        private readonly IFoodMenuAttachmentApiClient _foodMenuAttachmentApiClient;
        private readonly IPortalUserSyncApiClient _portalUserSyncApiClient;
        private readonly IWebHostEnvironment _environment;

        public AdminController(
            ILeaveService leaveService,
            IAnnouncementService announcementService,
            IEmployeePortalService employeePortalService,
            ISliderService sliderService,
            ISliderImageApiClient sliderImageApiClient,
            IBirthdayPopupService birthdayPopupService,
            IBirthdayPopupImageApiClient birthdayPopupImageApiClient,
            IFoodMenuService foodMenuService,
            ISurveyService surveyService,
            IFoodMenuAttachmentApiClient foodMenuAttachmentApiClient,
            IPortalUserSyncApiClient portalUserSyncApiClient,
            IWebHostEnvironment environment)
        {
            _leaveService = leaveService;
            _announcementService = announcementService;
            _employeePortalService = employeePortalService;
            _sliderService = sliderService;
            _sliderImageApiClient = sliderImageApiClient;
            _birthdayPopupService = birthdayPopupService;
            _birthdayPopupImageApiClient = birthdayPopupImageApiClient;
            _foodMenuService = foodMenuService;
            _surveyService = surveyService;
            _foodMenuAttachmentApiClient = foodMenuAttachmentApiClient;
            _portalUserSyncApiClient = portalUserSyncApiClient;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            var dashboard = await _leaveService.GetAdminDashboardAsync(User.Identity?.Name ?? "Admin");

            var model = new AdminDashboardViewModel
            {
                AdminName = dashboard.AdminName,
                GeneratedAt = dashboard.GeneratedAt,
                PendingLeaveCount = dashboard.PendingLeaveCount,
                TotalAnnouncementCount = dashboard.TotalAnnouncementCount,
                TodayAnnouncementCount = dashboard.TodayAnnouncementCount,
                NegativeLeaveBalanceCount = dashboard.NegativeLeaveBalanceCount,
                RecentLeaveRequests = dashboard.RecentLeaveRequests
                    .Take(5)
                    .Select(x => new AdminRecentLeaveItemViewModel
                    {
                        EmployeeName = x.EmployeeName,
                        LeaveType = x.LeaveType,
                        RequestedDays = x.RequestedDays,
                        Status = x.Status,
                        StartDate = x.StartDate,
                        EndDate = x.EndDate,
                        CreatedDate = x.CreatedDate
                    })
                    .ToList(),
                RecentAnnouncements = dashboard.RecentAnnouncements
                    .Select(x => new AdminRecentAnnouncementItemViewModel
                    {
                        Title = x.Title,
                        IsActive = x.IsActive,
                        CreatedDate = x.CreatedDate
                    })
                    .ToList()
            };

            return View(model);
        }

        public IActionResult Announcements()
        {
            return RedirectToAction("Index", "Announcement");
        }

        [HttpGet("/Admin/LeaveRequests")]
        public async Task<IActionResult> LeaveRequests(int page = 1, int? status = null)
        {
            var selectedStatus = NormalizeLeaveStatusFilter(status);
            var result = await _leaveService.GetAdminLeaveRequestsAsync(new AdminLeaveRequestListQueryModel
            {
                Page = page,
                PageSize = LeaveRequestsPageSize,
                Status = selectedStatus
            });

            var model = new AdminLeaveRequestListViewModel
            {
                Items = result.Items.Select(MapLeaveRequestItem).ToList(),
                StatusOptions = CreateLeaveStatusOptions(),
                SelectedStatus = selectedStatus,
                CurrentPage = result.CurrentPage,
                TotalPages = result.TotalPages,
                TotalCount = result.TotalCount,
                PageSize = result.PageSize
            };

            return View(model);
        }

        [HttpGet("/Admin/LeaveRequests/{id:int}")]
        public async Task<IActionResult> LeaveRequestDetail(int id)
        {
            var item = await _leaveService.GetAdminLeaveRequestDetailAsync(id);
            if (item == null)
            {
                return RedirectToAction(nameof(LeaveRequests));
            }

            var model = new AdminLeaveRequestDetailViewModel
            {
                Item = MapLeaveRequestItem(item)
            };

            return View(model);
        }

        [HttpGet("/Admin/LeaveReport")]
        public async Task<IActionResult> LeaveReport(int? employeeId = null, int? leaveTypeId = null, string? startDate = null, string? endDate = null)
        {
            var result = await _leaveService.GetAdminLeaveReportAsync(new AdminLeaveReportQueryModel
            {
                EmployeeId = employeeId,
                LeaveTypeId = leaveTypeId,
                StartDate = startDate,
                EndDate = endDate
            });

            return View(MapLeaveReport(result));
        }

        [HttpGet("/Admin/LeaveAgreement")]
        public async Task<IActionResult> LeaveAgreement(string? searchText = null, bool? isSigned = null, string? sortOrder = null, int page = 1)
        {
            var result = await _leaveService.GetAdminLeaveAgreementsAsync(new AdminLeaveAgreementListQueryModel
            {
                SearchText = searchText,
                IsSigned = isSigned,
                SortOrder = sortOrder,
                Page = page,
                PageSize = LeaveAgreementsPageSize
            });

            return View(MapLeaveAgreementList(result, searchText, isSigned, sortOrder));
        }

        [HttpPost("/Admin/LeaveAgreement/Sync")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncLeaveAgreement(string? returnUrl = null)
        {
            var result = await _leaveService.SyncLeaveAgreementsAsync();
            TempData["LeaveAgreementLevel"] = result.IsSuccess ? "success" : "error";
            TempData["LeaveAgreementMessage"] = result.Message;

            return RedirectToLeaveAgreementReturnUrl(returnUrl);
        }

        [HttpPost("/Admin/LeaveAgreement/Upload")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadLeaveAgreement(IFormFile? file, string? returnUrl = null)
        {
            if (file == null || file.Length == 0)
            {
                TempData["LeaveAgreementLevel"] = "error";
                TempData["LeaveAgreementMessage"] = "Yüklenecek Excel dosyası seçiniz.";
                return RedirectToLeaveAgreementReturnUrl(returnUrl);
            }

            if (file.Length > MaxLeaveAgreementUploadSizeBytes)
            {
                TempData["LeaveAgreementLevel"] = "error";
                TempData["LeaveAgreementMessage"] = "Excel dosyası 10 MB sınırını aşamaz.";
                return RedirectToLeaveAgreementReturnUrl(returnUrl);
            }

            var extension = Path.GetExtension(file.FileName);
            if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                TempData["LeaveAgreementLevel"] = "error";
                TempData["LeaveAgreementMessage"] = "Sadece .xlsx uzantılı Excel dosyası yükleyebilirsiniz.";
                return RedirectToLeaveAgreementReturnUrl(returnUrl);
            }

            using var stream = file.OpenReadStream();
            var result = await _leaveService.UploadLeaveAgreementsAsync(new LeaveAgreementUploadRequestModel
            {
                ExcelStream = stream,
                CurrentUser = User.Identity?.Name ?? "anonymous",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                MethodName = "UploadLeaveAgreement"
            });

            TempData["LeaveAgreementLevel"] = result.IsSuccess
                ? string.Equals(result.Level, "warning", StringComparison.OrdinalIgnoreCase) ? "warning" : "success"
                : "error";
            TempData["LeaveAgreementMessage"] = result.Message;

            return RedirectToLeaveAgreementReturnUrl(returnUrl);
        }

        [HttpPost("/Admin/LeaveAgreement/Update/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateLeaveAgreement(int id, decimal agreedLeaveDays, bool isSigned, string? returnUrl = null)
        {
            var result = await _leaveService.UpdateLeaveAgreementAsync(new AdminLeaveAgreementUpdateModel
            {
                Id = id,
                AgreedLeaveDays = agreedLeaveDays,
                IsSigned = isSigned
            });

            TempData["LeaveAgreementLevel"] = result.IsSuccess ? "success" : "error";
            TempData["LeaveAgreementMessage"] = result.Message;

            return RedirectToLeaveAgreementReturnUrl(returnUrl);
        }

        [HttpGet("/Admin/LeaveAgreement/Print/{id:int}")]
        public async Task<IActionResult> PrintLeaveAgreement(int id)
        {
            var agreement = await _leaveService.GetAdminLeaveAgreementAsync(id);
            if (agreement == null)
            {
                return NotFound();
            }

            var pdfBytes = BuildLeaveAgreementPdf(agreement);
            Response.Headers.ContentDisposition = $"inline; filename=\"mutabakat-{id}.pdf\"";

            return File(pdfBytes, "application/pdf");
        }

        [HttpPost("/Admin/LeaveAgreement/UploadPdf/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadLeaveAgreementPdf(int id, IFormFile? file, string? returnUrl = null)
        {
            if (file == null || file.Length == 0)
            {
                TempData["LeaveAgreementLevel"] = "error";
                TempData["LeaveAgreementMessage"] = "Yüklenecek PDF dosyası seçiniz.";
                return RedirectToLeaveAgreementReturnUrl(returnUrl);
            }

            if (file.Length > MaxLeaveAgreementPdfUploadSizeBytes)
            {
                TempData["LeaveAgreementLevel"] = "error";
                TempData["LeaveAgreementMessage"] = "PDF dosyası 10 MB sınırını aşamaz.";
                return RedirectToLeaveAgreementReturnUrl(returnUrl);
            }

            if (!IsAllowedPdf(file))
            {
                TempData["LeaveAgreementLevel"] = "error";
                TempData["LeaveAgreementMessage"] = "Sadece .pdf uzantılı dosya yükleyebilirsiniz.";
                return RedirectToLeaveAgreementReturnUrl(returnUrl);
            }

            var uploadFolder = GetLeaveAgreementPdfUploadFolder();
            Directory.CreateDirectory(uploadFolder);

            var fileName = $"{id}-{Guid.NewGuid():N}.pdf";
            var filePath = Path.Combine(uploadFolder, fileName);

            await using (var stream = System.IO.File.Create(filePath))
            {
                await file.CopyToAsync(stream);
            }

            var result = await _leaveService.UpdateLeaveAgreementPdfAsync(new AdminLeaveAgreementPdfUpdateModel
            {
                Id = id,
                FileName = fileName,
                OriginalFileName = Path.GetFileName(file.FileName),
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/pdf" : file.ContentType,
                SizeBytes = file.Length
            });

            if (!result.IsSuccess && System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            TempData["LeaveAgreementLevel"] = result.IsSuccess ? "success" : "error";
            TempData["LeaveAgreementMessage"] = result.Message;

            return RedirectToLeaveAgreementReturnUrl(returnUrl);
        }

        [HttpGet("/Admin/LeaveAgreement/ViewPdf/{id:int}")]
        public async Task<IActionResult> ViewLeaveAgreementPdf(int id)
        {
            var agreement = await _leaveService.GetAdminLeaveAgreementAsync(id);
            if (agreement == null || !agreement.HasAgreementPdf || string.IsNullOrWhiteSpace(agreement.AgreementPdfFileName))
            {
                return NotFound();
            }

            var filePath = Path.Combine(GetLeaveAgreementPdfUploadFolder(), agreement.AgreementPdfFileName);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var contentType = string.IsNullOrWhiteSpace(agreement.AgreementPdfContentType)
                ? "application/pdf"
                : agreement.AgreementPdfContentType;

            return PhysicalFile(filePath, contentType, enableRangeProcessing: true);
        }

        [HttpGet("/Admin/PortalUsers")]
        public async Task<IActionResult> PortalUsers(string? status = "active", string? search = null, int page = 1)
        {
            var result = await _employeePortalService.GetPortalUsersAsync(new PortalUserListQueryModel
            {
                Status = status,
                SearchTerm = search,
                Page = page,
                PageSize = PortalUsersPageSize
            });

            var model = new AdminPortalUserListViewModel
            {
                Status = result.Status,
                SearchTerm = result.SearchTerm,
                CurrentPage = result.CurrentPage,
                TotalPages = result.TotalPages,
                TotalCount = result.TotalCount,
                PageSize = result.PageSize,
                Items = result.Items.Select(MapPortalUserListItem).ToList()
            };

            if (string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            {
                return PartialView("_PortalUserResults", model);
            }

            return View(model);
        }

        [HttpPost("/Admin/PortalUsers/Sync")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncPortalUsers(CancellationToken cancellationToken)
        {
            var result = await _portalUserSyncApiClient.SyncPortalUsersAsync(cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(new
                {
                    success = true,
                    level = "success",
                    message = "Kullanıcılar başarıyla senkronize edildi.",
                    processedUsers = result.ProcessedUsers
                });
            }

            return StatusCode(500, new
            {
                success = false,
                level = "danger",
                message = "Hata! Senkronizasyon başarısız.",
                detail = result.Message
            });
        }

        [HttpGet("/Admin/PortalUsers/{id:int}")]
        public async Task<IActionResult> PortalUserDetail(int id)
        {
            var portalUser = await _employeePortalService.GetPortalUserEditAsync(id);
            if (portalUser == null)
            {
                return RedirectToAction(nameof(PortalUsers));
            }

            return View(await BuildPortalUserEditViewModelAsync(portalUser));
        }

        [HttpPost("/Admin/PortalUsers/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PortalUserDetail(int id, AdminPortalUserEditViewModel model)
        {
            if (id != model.Id)
            {
                model.Id = id;
            }

            EnsurePortalChildInputs(model);

            var existingPortalUser = await _employeePortalService.GetPortalUserEditAsync(id);
            if (existingPortalUser == null)
            {
                return RedirectToAction(nameof(PortalUsers));
            }

            if (!ModelState.IsValid)
            {
                PopulateActiveLoans(model, existingPortalUser);
                await PopulateLocationOptionsAsync(model);
                return View(model);
            }

            var result = await _employeePortalService.UpdatePortalUserAsync(new PortalUserEditModel
            {
                Id = model.Id,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Title = model.Title,
                HireDate = model.HireDate,
                TerminationDate = model.TerminationDate,
                BirthDate = model.BirthDate,
                LocationIds = model.LocationIds,
                AddressText = model.AddressText,
                MaritalStatus = model.MaritalStatus,
                EducationUniversity = model.EducationUniversity,
                EducationFaculty = model.EducationFaculty,
                EducationDepartment = model.EducationDepartment,
                Children = model.Children.Select(x => new PortalUserChildEditModel
                {
                    Gender = x.Gender,
                    BirthDate = x.BirthDate,
                    EducationStatus = x.EducationStatus
                }).ToList(),
                LeaveDays = model.LeaveDays,
                IsAdmin = model.IsAdmin,
                IsDeleted = model.IsDeleted,
                ActiveLoanWarningAccepted = model.ActiveLoanWarningAccepted
            });

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                PopulateActiveLoans(model, existingPortalUser);
                await PopulateLocationOptionsAsync(model);
                return View(model);
            }

            TempData["PortalUserSuccess"] = result.Message;
            return RedirectToAction(nameof(PortalUserDetail), new { id });
        }

        [HttpGet("/Admin/Slider")]
        public async Task<IActionResult> Slider()
        {
            var model = await BuildSliderViewModelAsync();
            return View(model);
        }

        [HttpGet("/Admin/BirthdayPopup")]
        public async Task<IActionResult> BirthdayPopup()
        {
            var model = await BuildBirthdayPopupViewModelAsync();
            return View(model);
        }

        [HttpGet("/Admin/FoodMenu")]
        public async Task<IActionResult> FoodMenu(int? id = null)
        {
            var model = await BuildFoodMenuViewModelAsync(id);
            return View(model);
        }

        [HttpGet("/Admin/Surveys")]
        public async Task<IActionResult> Surveys(string? status = "all")
        {
            var model = await BuildSurveyListViewModelAsync(status);
            return View(model);
        }

        [HttpGet("/Admin/Surveys/Create")]
        public IActionResult SurveyCreate()
        {
            var model = new AdminSurveyCreateViewModel();
            EnsureSurveyQuestionInputs(model);
            return View(model);
        }

        [HttpGet("/Admin/Surveys/{id:int}")]
        public async Task<IActionResult> SurveyDetail(int id)
        {
            var model = await BuildSurveyDetailViewModelAsync(id);
            if (model == null)
            {
                return RedirectToAction(nameof(Surveys));
            }

            return View(model);
        }

        [HttpPost("/Admin/Surveys/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSurvey(AdminSurveyCreateViewModel model)
        {
            EnsureSurveyQuestionInputs(model);

            if (!ModelState.IsValid)
            {
                return View("SurveyCreate", model);
            }

            var result = await _surveyService.CreateSurveyAsync(new SurveyCreateModel
            {
                Title = model.Title,
                Description = model.Description,
                IsActive = model.IsActive,
                Questions = model.Questions.Select(question => new SurveyQuestionCreateModel
                {
                    Type = question.Type,
                    QuestionText = question.QuestionText,
                    Options = question.Type == SurveyType.MultipleChoice
                        ? question.Options.Select(option => option.Text ?? string.Empty).ToList()
                        : new List<string>()
                }).ToList()
            });

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View("SurveyCreate", model);
            }

            TempData["SurveySuccess"] = result.Message;
            return RedirectToAction(nameof(Surveys));
        }

        [HttpPost("/Admin/Surveys/{id:int}/Activate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateSurvey(int id)
        {
            var result = await _surveyService.SetSurveyActiveStateAsync(id, true);
            TempData[result.IsSuccess ? "SurveySuccess" : "SurveyError"] = result.Message;
            return RedirectToAction(nameof(SurveyDetail), new { id });
        }

        [HttpPost("/Admin/Surveys/{id:int}/Deactivate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateSurvey(int id)
        {
            var result = await _surveyService.SetSurveyActiveStateAsync(id, false);
            TempData[result.IsSuccess ? "SurveySuccess" : "SurveyError"] = result.Message;
            return RedirectToAction(nameof(SurveyDetail), new { id });
        }

        [HttpPost("/Admin/Slider/Upload")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadSliderImages(List<IFormFile>? files)
        {
            if (files == null || files.Count == 0)
            {
                TempData["SliderError"] = "YÃ¼klenecek en az bir gÃ¶rsel seÃ§in.";
                return RedirectToAction(nameof(Slider));
            }

            var uploadedCount = 0;
            var failedMessages = new List<string>();

            foreach (var file in files.Where(x => x != null && x.Length > 0))
            {
                if (!IsAllowedSliderImage(file))
                {
                    failedMessages.Add($"{file.FileName} desteklenmeyen bir dosya tÃ¼rÃ¼.");
                    continue;
                }

                var uploadResult = await _sliderImageApiClient.UploadAsync(file);
                if (!uploadResult.IsSuccess)
                {
                    failedMessages.Add($"{file.FileName} yÃ¼klenemedi: {uploadResult.Message}");
                    continue;
                }

                await _sliderService.AddSliderImageAsync(new SliderImageCreateModel
                {
                    FileName = uploadResult.FileName,
                    OriginalFileName = file.FileName,
                    RelativePath = uploadResult.RelativePath,
                    ContentType = string.IsNullOrWhiteSpace(uploadResult.ContentType) ? file.ContentType ?? string.Empty : uploadResult.ContentType,
                    SizeBytes = file.Length
                });

                uploadedCount++;
            }

            if (uploadedCount > 0)
            {
                TempData["SliderSuccess"] = uploadedCount == 1
                    ? "Slider gÃ¶rseli yÃ¼klendi."
                    : $"{uploadedCount} slider gÃ¶rseli yÃ¼klendi.";
            }

            if (failedMessages.Count > 0)
            {
                TempData["SliderError"] = string.Join(" ", failedMessages);
            }

            return RedirectToAction(nameof(Slider));
        }

        [HttpPost("/Admin/BirthdayPopup/Upload")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadBirthdayPopupImage(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["BirthdayPopupError"] = "Yüklenecek bir görsel seçin.";
                return RedirectToAction(nameof(BirthdayPopup));
            }

            if (!IsAllowedSliderImage(file))
            {
                TempData["BirthdayPopupError"] = $"{file.FileName} desteklenmeyen bir dosya türü.";
                return RedirectToAction(nameof(BirthdayPopup));
            }

            var existingImage = await _birthdayPopupService.GetActiveBirthdayPopupImageAsync();
            var uploadResult = await _birthdayPopupImageApiClient.UploadAsync(file);

            if (!uploadResult.IsSuccess)
            {
                TempData["BirthdayPopupError"] = uploadResult.Message;
                return RedirectToAction(nameof(BirthdayPopup));
            }

            if (existingImage != null)
            {
                var deleteExistingResult = await _birthdayPopupImageApiClient.DeleteAsync(existingImage.FileName);
                if (!deleteExistingResult.IsSuccess)
                {
                    await _birthdayPopupImageApiClient.DeleteAsync(uploadResult.FileName);
                    TempData["BirthdayPopupError"] = string.IsNullOrWhiteSpace(deleteExistingResult.Message)
                        ? "Mevcut doğum günü popup görseli silinemedi."
                        : deleteExistingResult.Message;
                    return RedirectToAction(nameof(BirthdayPopup));
                }
            }

            var replaceResult = await _birthdayPopupService.ReplaceBirthdayPopupImageAsync(new BirthdayPopupImageCreateModel
            {
                FileName = uploadResult.FileName,
                OriginalFileName = file.FileName,
                RelativePath = uploadResult.RelativePath,
                ContentType = string.IsNullOrWhiteSpace(uploadResult.ContentType) ? file.ContentType ?? string.Empty : uploadResult.ContentType,
                SizeBytes = file.Length
            });

            TempData[replaceResult.IsSuccess ? "BirthdayPopupSuccess" : "BirthdayPopupError"] = replaceResult.IsSuccess
                ? "Doğum günü popup görseli güncellendi."
                : replaceResult.Message;

            return RedirectToAction(nameof(BirthdayPopup));
        }

        [HttpPost("/Admin/FoodMenu/Import")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportFoodMenu(int importYear, int importMonth, IFormFile? file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                TempData["FoodMenuError"] = "Yüklenecek PDF dosyasını seçin.";
                return RedirectToAction(nameof(FoodMenu));
            }

            if (file.Length > MaxFoodMenuUploadSizeBytes)
            {
                TempData["FoodMenuError"] = "PDF dosyası 15 MB sınırını aşamaz.";
                return RedirectToAction(nameof(FoodMenu));
            }

            if (!IsAllowedPdf(file))
            {
                TempData["FoodMenuError"] = "Sadece PDF dosyası yükleyebilirsiniz.";
                return RedirectToAction(nameof(FoodMenu));
            }

            await using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, cancellationToken);
            var pdfBytes = memoryStream.ToArray();

            var existingMonth = await _foodMenuService.GetFoodMenuMonthByYearMonthAsync(importYear, importMonth);
            var ensureResult = await _foodMenuService.EnsureDraftFoodMenuMonthAsync(importYear, importMonth);
            if (!ensureResult.IsSuccess || ensureResult.Data == null)
            {
                TempData["FoodMenuError"] = ensureResult.Message;
                return RedirectToAction(nameof(FoodMenu));
            }

            var month = ensureResult.Data;
            var uploadResult = await _foodMenuAttachmentApiClient.UploadAsync(month.Id, file, cancellationToken);
            if (!uploadResult.IsSuccess)
            {
                if (existingMonth == null)
                {
                    await _foodMenuService.DeleteFoodMenuMonthAsync(month.Id);
                }

                TempData["FoodMenuError"] = uploadResult.Message;
                return RedirectToAction(nameof(FoodMenu), new { id = month.Id });
            }

            var replaceResult = await _foodMenuService.ReplaceImportedFoodMenuAsync(new FoodMenuImportModel
            {
                MonthId = month.Id,
                Year = importYear,
                Month = importMonth,
                FileName = uploadResult.FileName,
                OriginalFileName = file.FileName,
                RelativePath = uploadResult.RelativePath,
                ContentType = string.IsNullOrWhiteSpace(uploadResult.ContentType) ? file.ContentType ?? "application/pdf" : uploadResult.ContentType,
                SizeBytes = file.Length,
                PdfContent = pdfBytes
            });

            if (!replaceResult.IsSuccess || replaceResult.Data == null)
            {
                await _foodMenuAttachmentApiClient.DeleteAsync(month.Id, uploadResult.FileName, cancellationToken);
                if (existingMonth == null)
                {
                    await _foodMenuService.DeleteFoodMenuMonthAsync(month.Id);
                }

                TempData["FoodMenuError"] = replaceResult.Message;
                return RedirectToAction(nameof(FoodMenu), new { id = month.Id });
            }

            if (existingMonth != null &&
                !string.IsNullOrWhiteSpace(existingMonth.FileName) &&
                !string.Equals(existingMonth.FileName, uploadResult.FileName, StringComparison.OrdinalIgnoreCase))
            {
                var oldDeleteResult = await _foodMenuAttachmentApiClient.DeleteAsync(existingMonth.Id, existingMonth.FileName, cancellationToken);
                if (!oldDeleteResult.IsSuccess)
                {
                    TempData["FoodMenuWarning"] = "Yeni PDF kaydedildi, ancak eski PDF fiziksel olarak silinemedi.";
                }
            }

            if (string.Equals(replaceResult.Level, "warning", StringComparison.OrdinalIgnoreCase))
            {
                TempData["FoodMenuWarning"] = replaceResult.Message;
            }
            else
            {
                TempData["FoodMenuSuccess"] = replaceResult.Message;
            }

            return RedirectToAction(nameof(FoodMenu), new { id = replaceResult.Data.Id });
        }

        [HttpPost("/Admin/FoodMenu/{id:int}/SaveDays")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveFoodMenuDays(int id, AdminFoodMenuEditorViewModel model)
        {
            var result = await _foodMenuService.SaveFoodMenuDaysAsync(new FoodMenuMonthDaySaveModel
            {
                MonthId = id,
                Days = model.Days.Select(x => new FoodMenuMonthDaySaveItemModel
                {
                    MenuDate = x.MenuDate,
                    ItemsText = x.ItemsText,
                    SourcePageNumber = x.SourcePageNumber
                }).ToList()
            });

            TempData[result.IsSuccess ? "FoodMenuSuccess" : "FoodMenuError"] = result.Message;
            return RedirectToAction(nameof(FoodMenu), new { id });
        }

        [HttpPost("/Admin/FoodMenu/{id:int}/Publish")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishFoodMenu(int id)
        {
            var result = await _foodMenuService.PublishFoodMenuMonthAsync(id);
            TempData[result.IsSuccess ? "FoodMenuSuccess" : "FoodMenuError"] = result.Message;
            return RedirectToAction(nameof(FoodMenu), new { id });
        }

        [HttpPost("/Admin/FoodMenu/{id:int}/Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFoodMenu(int id, CancellationToken cancellationToken)
        {
            var month = await _foodMenuService.GetFoodMenuMonthAsync(id);
            if (month == null)
            {
                TempData["FoodMenuError"] = "Silinecek yemek menüsü bulunamadı.";
                return RedirectToAction(nameof(FoodMenu));
            }

            if (!string.IsNullOrWhiteSpace(month.FileName))
            {
                var deleteFileResult = await _foodMenuAttachmentApiClient.DeleteAsync(month.Id, month.FileName, cancellationToken);
                if (!deleteFileResult.IsSuccess)
                {
                    TempData["FoodMenuError"] = string.IsNullOrWhiteSpace(deleteFileResult.Message)
                        ? "Yemek menüsü PDF dosyası silinemedi."
                        : deleteFileResult.Message;
                    return RedirectToAction(nameof(FoodMenu), new { id });
                }
            }

            var result = await _foodMenuService.DeleteFoodMenuMonthAsync(id);
            TempData[result.IsSuccess ? "FoodMenuSuccess" : "FoodMenuError"] = result.Message;
            return RedirectToAction(nameof(FoodMenu));
        }

        [HttpPost("/Admin/Slider/Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSliderImage(int id)
        {
            var item = await _sliderService.GetSliderImageAsync(id);
            if (item == null)
            {
                TempData["SliderError"] = "Silinecek slider gÃ¶rseli bulunamadÄ±.";
                return RedirectToAction(nameof(Slider));
            }

            var deleteResult = await _sliderImageApiClient.DeleteAsync(item.FileName);
            if (!deleteResult.IsSuccess)
            {
                TempData["SliderError"] = string.IsNullOrWhiteSpace(deleteResult.Message)
                    ? "Slider gÃ¶rseli silinemedi."
                    : deleteResult.Message;
                return RedirectToAction(nameof(Slider));
            }

            await _sliderService.DeleteSliderImageMetadataAsync(id);

            TempData["SliderSuccess"] = "Slider gÃ¶rseli silindi.";
            return RedirectToAction(nameof(Slider));
        }

        [HttpPost("/Admin/BirthdayPopup/Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBirthdayPopupImage(int id)
        {
            var item = await _birthdayPopupService.GetBirthdayPopupImageAsync(id);
            if (item == null)
            {
                TempData["BirthdayPopupError"] = "Silinecek doğum günü popup görseli bulunamadı.";
                return RedirectToAction(nameof(BirthdayPopup));
            }

            var deleteResult = await _birthdayPopupImageApiClient.DeleteAsync(item.FileName);
            if (!deleteResult.IsSuccess)
            {
                TempData["BirthdayPopupError"] = string.IsNullOrWhiteSpace(deleteResult.Message)
                    ? "Doğum günü popup görseli silinemedi."
                    : deleteResult.Message;
                return RedirectToAction(nameof(BirthdayPopup));
            }

            var metadataDeleteResult = await _birthdayPopupService.DeleteBirthdayPopupImageMetadataAsync(id);
            TempData[metadataDeleteResult.IsSuccess ? "BirthdayPopupSuccess" : "BirthdayPopupError"] = metadataDeleteResult.IsSuccess
                ? "Doğum günü popup görseli silindi."
                : metadataDeleteResult.Message;

            return RedirectToAction(nameof(BirthdayPopup));
        }

        [HttpPost("/Admin/Slider/Reorder")]
        public async Task<IActionResult> ReorderSlider([FromBody] SliderReorderRequest? request)
        {
            if (request?.OrderedIds == null || request.OrderedIds.Count == 0)
            {
                return BadRequest(new { message = "GeÃ§erli bir slider sÄ±rasÄ± gÃ¶nderilmedi." });
            }

            var result = await _sliderService.ReorderSliderAsync(new SliderReorderModel
            {
                OrderedIds = request.OrderedIds
            });

            return result.IsSuccess
                ? Ok(new { message = result.Message })
                : BadRequest(new { message = result.Message });
        }

        [HttpGet("/Admin/LeaveReport/Export")]
        public async Task<IActionResult> ExportLeaveReport(int? employeeId = null, int? leaveTypeId = null, string? startDate = null, string? endDate = null)
        {
            var export = await _leaveService.ExportAdminLeaveReportAsync(new AdminLeaveReportQueryModel
            {
                EmployeeId = employeeId,
                LeaveTypeId = leaveTypeId,
                StartDate = startDate,
                EndDate = endDate
            });

            return File(
                export.Content,
                export.ContentType,
                export.FileName);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateLeaveStatus(int id, int status, string? returnUrl = null)
        {
            var result = await _leaveService.UpdateLeaveStatusWithLogAsync(new LeaveStatusUpdateRequestModel
            {
                LeaveId = id,
                Status = status,
                CurrentUser = User.Identity?.Name ?? "anonymous",
                DecisionBy = GetCurrentUserDisplayName(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                MethodName = UpdateLeaveStatusMethodName
            });

            if (result.IsSuccess)
            {
                TempData["AdminLeaveStatusLevel"] = "success";
                TempData["AdminLeaveStatusMessage"] = status == (int)LeaveStatus.Approved
                    ? "İzin talebi onaylandı."
                    : status == (int)LeaveStatus.Rejected
                        ? "İzin talebi reddedildi."
                        : result.Message;

                return RedirectToLeaveReturnUrl(returnUrl);
            }

            TempData["AdminLeaveStatusLevel"] = "error";
            TempData["AdminLeaveStatusMessage"] = string.IsNullOrWhiteSpace(result.Message)
                ? "İşlem tamamlanamadı."
                : result.Message;

            return RedirectToLeaveReturnUrl(returnUrl);
        }

        private string GetCurrentUserDisplayName()
        {
            var givenName = User.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value;
            var surname = User.FindFirst(System.Security.Claims.ClaimTypes.Surname)?.Value;
            var fullName = string.Join(" ", new[] { givenName, surname }
                .Where(x => !string.IsNullOrWhiteSpace(x)))
                .Trim();

            return string.IsNullOrWhiteSpace(fullName)
                ? User.Identity?.Name ?? "anonymous"
                : fullName;
        }

        private IActionResult RedirectToLeaveReturnUrl(string? returnUrl)
        {
            return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? LocalRedirect(returnUrl)
                : RedirectToAction(nameof(LeaveRequests));
        }

        private IActionResult RedirectToLeaveAgreementReturnUrl(string? returnUrl)
        {
            return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? LocalRedirect(returnUrl)
                : RedirectToAction(nameof(LeaveAgreement));
        }

        private static int? NormalizeLeaveStatusFilter(int? status)
        {
            return status.HasValue && Enum.IsDefined(typeof(LeaveStatus), status.Value)
                ? status.Value
                : null;
        }

        private static List<AdminLeaveStatusFilterOptionViewModel> CreateLeaveStatusOptions()
        {
            return new List<AdminLeaveStatusFilterOptionViewModel>
            {
                new() { Value = (int)LeaveStatus.Pending, Label = "Onay Bekliyor" },
                new() { Value = (int)LeaveStatus.Approved, Label = "Onaylandı" },
                new() { Value = (int)LeaveStatus.Rejected, Label = "Reddedildi" },
                new() { Value = (int)LeaveStatus.Cancelled, Label = "İptal" }
            };
        }

        private static AdminLeaveRequestViewModel MapLeaveRequestItem(AdminLeaveRequestItemModel item)
        {
            return new AdminLeaveRequestViewModel
            {
                Id = item.Id,
                EmployeeId = item.EmployeeId,
                LeaveTypeId = item.LeaveTypeId,
                EmployeeName = item.EmployeeName,
                LeaveType = item.LeaveType,
                RequestedDays = item.RequestedDays,
                StartDate = item.StartDate,
                EndDate = item.EndDate,
                Reason = item.Reason,
                Status = item.Status,
                RemainingLeaveDays = item.RemainingLeaveDays,
                CreatedDate = item.CreatedDate,
                StatusLabel = item.StatusLabel,
                StatusTone = item.StatusTone,
                DecisionDisplay = item.DecisionDisplay,
                CanTakeAction = item.CanTakeAction
            };
        }

        private static AdminLeaveReportViewModel MapLeaveReport(AdminLeaveReportResultModel result)
        {
            return new AdminLeaveReportViewModel
            {
                Items = result.Items.Select(x => new AdminLeaveReportItemViewModel
                {
                    Id = x.Id,
                    EmployeeName = x.EmployeeName,
                    LeaveType = x.LeaveType,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    RequestedDays = x.RequestedDays,
                    StatusLabel = x.StatusLabel,
                    StatusTone = x.StatusTone,
                    CreatedDate = x.CreatedDate
                }).ToList(),
                EmployeeOptions = result.EmployeeOptions.Select(x => new AdminLeaveReportFilterOptionViewModel
                {
                    Id = x.Id,
                    Label = x.Label
                }).ToList(),
                LeaveTypeOptions = result.LeaveTypeOptions.Select(x => new AdminLeaveReportFilterOptionViewModel
                {
                    Id = x.Id,
                    Label = x.Label
                }).ToList(),
                SelectedEmployeeId = result.SelectedEmployeeId,
                SelectedLeaveTypeId = result.SelectedLeaveTypeId,
                StartDate = result.StartDate,
                EndDate = result.EndDate,
                TotalCount = result.TotalCount
            };
        }

        private static AdminLeaveAgreementListViewModel MapLeaveAgreementList(
            PagedResultModel<AdminLeaveAgreementItemModel> result,
            string? searchText,
            bool? isSigned,
            string? sortOrder)
        {
            return new AdminLeaveAgreementListViewModel
            {
                Items = result.Items.Select(x => new AdminLeaveAgreementViewModel
                {
                    Id = x.Id,
                    EmployeePortalId = x.EmployeePortalId,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    EmployeeName = x.EmployeeName,
                    Email = x.Email,
                    PhoneNumber = x.PhoneNumber,
                    AgreedLeaveDays = x.AgreedLeaveDays,
                    IsSigned = x.IsSigned,
                    HasAgreementPdf = x.HasAgreementPdf,
                    AgreementPdfOriginalFileName = x.AgreementPdfOriginalFileName,
                    CreatedDate = x.CreatedDate
                }).ToList(),
                SearchText = searchText ?? string.Empty,
                SelectedIsSigned = isSigned,
                SortOrder = string.Equals(sortOrder, "CreatedDate_Asc", StringComparison.OrdinalIgnoreCase)
                    ? "CreatedDate_Asc"
                    : "CreatedDate_Desc",
                CurrentPage = result.CurrentPage,
                TotalPages = result.TotalPages,
                TotalCount = result.TotalCount,
                PageSize = result.PageSize
            };
        }

        private async Task<AdminSliderViewModel> BuildSliderViewModelAsync()
        {
            var items = (await _sliderService.GetSliderImagesAsync())
                .Select(MapSliderItem)
                .ToList();

            return new AdminSliderViewModel
            {
                Items = items
            };
        }

        private async Task<AdminBirthdayPopupViewModel> BuildBirthdayPopupViewModelAsync()
        {
            var item = await _birthdayPopupService.GetActiveBirthdayPopupImageAsync();
            return new AdminBirthdayPopupViewModel
            {
                Item = item == null ? null : MapBirthdayPopupItem(item)
            };
        }

        private async Task<AdminFoodMenuViewModel> BuildFoodMenuViewModelAsync(int? id)
        {
            var months = await _foodMenuService.GetFoodMenuMonthsAsync();
            var selectedMonthId = id ?? months.FirstOrDefault()?.Id;
            var selectedMonth = selectedMonthId.HasValue
                ? await _foodMenuService.GetFoodMenuMonthAsync(selectedMonthId.Value)
                : null;

            return new AdminFoodMenuViewModel
            {
                ImportYear = selectedMonth?.Year ?? DateTime.Today.Year,
                ImportMonth = selectedMonth?.Month ?? DateTime.Today.Month,
                Months = months.Select(x => MapFoodMenuMonthListItem(x, selectedMonthId)).ToList(),
                SelectedMonth = selectedMonth == null ? null : MapFoodMenuEditor(selectedMonth)
            };
        }

        private async Task<AdminSurveyListViewModel> BuildSurveyListViewModelAsync(string? status)
        {
            var normalizedStatus = NormalizeSurveyStatusFilter(status);
            var items = await _surveyService.GetAdminSurveysAsync(normalizedStatus);

            return new AdminSurveyListViewModel
            {
                Status = NormalizeSurveyStatusValue(status),
                Items = items.Select(MapAdminSurveyListItem).ToList()
            };
        }

        private async Task<AdminSurveyDetailViewModel?> BuildSurveyDetailViewModelAsync(int id)
        {
            var detail = await _surveyService.GetAdminSurveyDetailAsync(id);
            if (detail == null)
            {
                return null;
            }

            return new AdminSurveyDetailViewModel
            {
                Id = detail.Id,
                Title = detail.Title,
                Description = detail.Description,
                IsActive = detail.IsActive,
                StatusLabel = detail.IsActive ? "Aktif" : "Pasif",
                StatusTone = detail.IsActive ? "approved" : "rejected",
                QuestionCount = detail.Questions.Count,
                AnswerCount = detail.AnswerCount,
                ActivePortalUserCount = detail.ActivePortalUserCount,
                ParticipationRate = detail.ParticipationRate,
                CreatedDate = detail.CreatedDate,
                Questions = detail.QuestionResults.Select(question => new AdminSurveyQuestionDetailViewModel
                {
                    Id = question.Id,
                    QuestionText = question.QuestionText,
                    Type = question.Type,
                    TypeLabel = GetSurveyTypeLabel(question.Type),
                    DisplayOrder = question.DisplayOrder,
                    AverageRating = question.AverageRating,
                    Options = question.Options.Select(x => new AdminSurveyOptionViewModel
                    {
                        Id = x.Id,
                        Text = x.Text,
                        DisplayOrder = x.DisplayOrder
                    }).ToList(),
                    RatingDistribution = question.RatingDistribution.Select(x => new AdminSurveyRatingDistributionViewModel
                    {
                        RatingValue = x.RatingValue,
                        Count = x.Count,
                        Percentage = x.Percentage
                    }).ToList(),
                    OptionResults = question.OptionResults.Select(x => new AdminSurveyOptionResultViewModel
                    {
                        OptionId = x.OptionId,
                        Text = x.Text,
                        DisplayOrder = x.DisplayOrder,
                        Count = x.Count,
                        Percentage = x.Percentage
                    }).ToList()
                }).ToList()
            };
        }

        private async Task<AdminPortalUserEditViewModel> BuildPortalUserEditViewModelAsync(PortalUserEditModel portalUser)
        {
            var model = MapPortalUserEditModel(portalUser);
            EnsurePortalChildInputs(model);
            await PopulateLocationOptionsAsync(model);
            return model;
        }

        private async Task PopulateLocationOptionsAsync(AdminPortalUserEditViewModel model)
        {
            var options = await _employeePortalService.GetLocationOptionsAsync();
            model.LocationOptions = options
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Name
                })
                .ToList();

        }

        private static AdminPortalUserListItemViewModel MapPortalUserListItem(PortalUserListItemModel portalUser)
        {
            return new AdminPortalUserListItemViewModel
            {
                Id = portalUser.Id,
                FullName = portalUser.FullName,
                Email = portalUser.Email,
                PhoneNumber = portalUser.PhoneNumber,
                Title = portalUser.Title,
                LocationNames = portalUser.LocationNames,
                LeaveDays = portalUser.LeaveDays,
                HireDate = portalUser.HireDate,
                TerminationDate = portalUser.TerminationDate,
                IsAdmin = portalUser.IsAdmin,
                IsDeleted = portalUser.IsDeleted
            };
        }

        private static AdminPortalUserEditViewModel MapPortalUserEditModel(PortalUserEditModel portalUser)
        {
            return new AdminPortalUserEditViewModel
            {
                Id = portalUser.Id,
                FirstName = portalUser.FirstName,
                LastName = portalUser.LastName,
                Email = portalUser.Email,
                PhoneNumber = portalUser.PhoneNumber,
                Title = portalUser.Title,
                HireDate = portalUser.HireDate,
                TerminationDate = portalUser.TerminationDate,
                BirthDate = portalUser.BirthDate,
                LocationIds = portalUser.LocationIds,
                AddressText = portalUser.AddressText,
                MaritalStatus = portalUser.MaritalStatus,
                EducationUniversity = portalUser.EducationUniversity,
                EducationFaculty = portalUser.EducationFaculty,
                EducationDepartment = portalUser.EducationDepartment,
                Children = portalUser.Children.Select(x => new ProfileChildInputViewModel
                {
                    Gender = x.Gender,
                    BirthDate = x.BirthDate,
                    EducationStatus = x.EducationStatus
                }).ToList(),
                LeaveDays = portalUser.LeaveDays,
                IsAdmin = portalUser.IsAdmin,
                IsDeleted = portalUser.IsDeleted,
                ActiveLoans = portalUser.ActiveLoans.Select(MapActiveLoan).ToList(),
                LoanRecordCount = portalUser.LoanRecordCount
            };
        }

        private static void PopulateActiveLoans(
            AdminPortalUserEditViewModel model,
            PortalUserEditModel portalUser)
        {
            model.ActiveLoans = portalUser.ActiveLoans.Select(MapActiveLoan).ToList();
            model.LoanRecordCount = portalUser.LoanRecordCount;
        }

        private static AdminPortalUserActiveLoanViewModel MapActiveLoan(PortalUserActiveLoanModel loan)
        {
            return new AdminPortalUserActiveLoanViewModel
            {
                LoanId = loan.LoanId,
                AssetId = loan.AssetId,
                AssetName = loan.AssetName,
                SerialNumber = loan.SerialNumber
            };
        }

        private static void EnsurePortalChildInputs(AdminPortalUserEditViewModel model)
        {
            model.Children ??= new List<ProfileChildInputViewModel>();
            model.Children = model.Children
                .Where(x => x != null)
                .Select(x => new ProfileChildInputViewModel
                {
                    Gender = x.Gender,
                    BirthDate = x.BirthDate,
                    EducationStatus = x.EducationStatus
                })
                .ToList();

            if (!model.Children.Any())
            {
                model.Children.Add(new ProfileChildInputViewModel());
            }
        }

        private static AdminSurveyListItemViewModel MapAdminSurveyListItem(AdminSurveyListItemModel survey)
        {
            return new AdminSurveyListItemViewModel
            {
                Id = survey.Id,
                Title = survey.Title,
                Description = survey.Description,
                QuestionCount = survey.QuestionCount,
                QuestionPreview = survey.QuestionPreview,
                IsActive = survey.IsActive,
                StatusLabel = survey.IsActive ? "Aktif" : "Pasif",
                StatusTone = survey.IsActive ? "approved" : "rejected",
                AnswerCount = survey.AnswerCount,
                ParticipationRate = survey.ParticipationRate,
                CreatedDate = survey.CreatedDate
            };
        }

        private AdminSliderItemViewModel MapSliderItem(SliderImageModel image)
        {
            return new AdminSliderItemViewModel
            {
                Id = image.Id,
                FileName = image.FileName,
                OriginalFileName = image.OriginalFileName,
                DisplayOrder = image.DisplayOrder,
                CreatedDate = image.CreatedDate,
                ImageUrl = _sliderImageApiClient.GetFileUrl(image.FileName)
            };
        }

        private AdminBirthdayPopupItemViewModel MapBirthdayPopupItem(BirthdayPopupImageModel image)
        {
            return new AdminBirthdayPopupItemViewModel
            {
                Id = image.Id,
                FileName = image.FileName,
                OriginalFileName = image.OriginalFileName,
                CreatedDate = image.CreatedDate,
                ImageUrl = _birthdayPopupImageApiClient.GetFileUrl(image.FileName)
            };
        }

        private static AdminFoodMenuMonthListItemViewModel MapFoodMenuMonthListItem(FoodMenuMonthModel month, int? selectedMonthId)
        {
            return new AdminFoodMenuMonthListItemViewModel
            {
                Id = month.Id,
                Year = month.Year,
                Month = month.Month,
                MonthLabel = BuildMonthLabel(month.Year, month.Month),
                Status = month.Status,
                StatusLabel = GetFoodMenuStatusLabel(month.Status),
                StatusTone = GetFoodMenuStatusTone(month.Status),
                DayCount = month.Days.Count(x => !string.IsNullOrWhiteSpace(x.ItemsText)),
                ImportedAt = month.ImportedAt,
                PublishedAt = month.PublishedAt,
                IsSelected = selectedMonthId.HasValue && month.Id == selectedMonthId.Value
            };
        }

        private static AdminFoodMenuEditorViewModel MapFoodMenuEditor(FoodMenuMonthModel month)
        {
            return new AdminFoodMenuEditorViewModel
            {
                Id = month.Id,
                Year = month.Year,
                Month = month.Month,
                MonthLabel = BuildMonthLabel(month.Year, month.Month),
                OriginalFileName = month.OriginalFileName,
                PageCount = month.PageCount,
                Status = month.Status,
                StatusLabel = GetFoodMenuStatusLabel(month.Status),
                StatusTone = GetFoodMenuStatusTone(month.Status),
                ImportedAt = month.ImportedAt,
                PublishedAt = month.PublishedAt,
                ParseWarnings = SplitWarnings(month.ParseWarnings),
                Days = BuildEditableDays(month)
            };
        }

        private static List<AdminFoodMenuDayEditItemViewModel> BuildEditableDays(FoodMenuMonthModel month)
        {
            var existingDays = month.Days.ToDictionary(x => x.MenuDate.Date);
            var dayCount = DateTime.DaysInMonth(month.Year, month.Month);
            var days = new List<AdminFoodMenuDayEditItemViewModel>(dayCount);

            for (var day = 1; day <= dayCount; day++)
            {
                var date = new DateTime(month.Year, month.Month, day);
                existingDays.TryGetValue(date.Date, out var existingDay);

                days.Add(new AdminFoodMenuDayEditItemViewModel
                {
                    MenuDate = date,
                    ItemsText = existingDay?.ItemsText ?? string.Empty,
                    SourcePageNumber = existingDay?.SourcePageNumber
                });
            }

            return days;
        }

        private static List<string> SplitWarnings(string? warnings)
        {
            if (string.IsNullOrWhiteSpace(warnings))
            {
                return new List<string>();
            }

            return warnings
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        private static string BuildMonthLabel(int year, int month)
        {
            return new DateTime(year, month, 1).ToString("MMMM yyyy", new CultureInfo("tr-TR"));
        }

        private static void EnsureSurveyQuestionInputs(AdminSurveyCreateViewModel model)
        {
            model.Questions ??= new List<AdminSurveyQuestionInputViewModel>();
            model.Questions = model.Questions
                .Where(x => x != null)
                .Select(question => new AdminSurveyQuestionInputViewModel
                {
                    Type = question.Type,
                    QuestionText = question.QuestionText ?? string.Empty,
                    Options = (question.Options ?? new List<AdminSurveyOptionInputViewModel>())
                        .Where(option => option != null)
                        .Select(option => new AdminSurveyOptionInputViewModel
                        {
                            Text = option.Text ?? string.Empty
                        })
                        .ToList()
                })
                .ToList();

            if (!model.Questions.Any())
            {
                model.Questions.Add(new AdminSurveyQuestionInputViewModel());
            }

            foreach (var question in model.Questions)
            {
                question.Options ??= new List<AdminSurveyOptionInputViewModel>();

                if (question.Type != SurveyType.MultipleChoice)
                {
                    question.Options = question.Options
                        .Where(option => option != null)
                        .Select(option => new AdminSurveyOptionInputViewModel
                        {
                            Text = option.Text ?? string.Empty
                        })
                        .ToList();
                    continue;
                }

                while (question.Options.Count < 2)
                {
                    question.Options.Add(new AdminSurveyOptionInputViewModel());
                }
            }
        }

        private static bool? NormalizeSurveyStatusFilter(string? status)
        {
            return NormalizeSurveyStatusValue(status) switch
            {
                "active" => true,
                "passive" => false,
                _ => null
            };
        }

        private static string NormalizeSurveyStatusValue(string? status)
        {
            return string.Equals(status, "active", StringComparison.OrdinalIgnoreCase)
                ? "active"
                : string.Equals(status, "passive", StringComparison.OrdinalIgnoreCase)
                    ? "passive"
                    : "all";
        }

        private static string GetSurveyTypeLabel(SurveyType type)
        {
            return type == SurveyType.MultipleChoice ? "Çoktan seçmeli" : "Puanlamalı";
        }

        private static string GetFoodMenuStatusLabel(FoodMenuMonthStatus status)
        {
            return status == FoodMenuMonthStatus.Published ? "Yayında" : "Taslak";
        }

        private static string GetFoodMenuStatusTone(FoodMenuMonthStatus status)
        {
            return status == FoodMenuMonthStatus.Published ? "success" : "draft";
        }

        private string GetLeaveAgreementPdfUploadFolder()
        {
            var webRootPath = string.IsNullOrWhiteSpace(_environment.WebRootPath)
                ? Path.Combine(_environment.ContentRootPath, "wwwroot")
                : _environment.WebRootPath;

            return Path.Combine(webRootPath, LeaveAgreementPdfUploadFolder.Replace('/', Path.DirectorySeparatorChar));
        }

        private static byte[] BuildLeaveAgreementPdf(AdminLeaveAgreementItemModel agreement)
        {
            var fullName = string.Join(" ", new[] { agreement.FirstName, agreement.LastName }
                .Where(x => !string.IsNullOrWhiteSpace(x)))
                .Trim();
            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = agreement.EmployeeName;
            }

            var lines = new[]
            {
                "Izin Mutabakat Formu",
                $"Ad Soyad: {fullName}",
                $"Mutabik Kalinacak Izin Gun Sayisi: {agreement.AgreedLeaveDays.ToString("0.##", CultureInfo.GetCultureInfo("tr-TR"))}"
            };

            var content = new StringBuilder();
            content.AppendLine("BT");
            content.AppendLine("/F1 22 Tf");
            content.AppendLine("72 760 Td");
            content.AppendLine($"({EscapePdfText(lines[0])}) Tj");
            content.AppendLine("/F1 13 Tf");

            for (var i = 1; i < lines.Length; i++)
            {
                content.AppendLine("0 -32 Td");
                content.AppendLine($"({EscapePdfText(lines[i])}) Tj");
            }

            content.AppendLine("ET");

            var contentBytes = Encoding.ASCII.GetBytes(content.ToString());
            var objects = new[]
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
                $"<< /Length {contentBytes.Length} >>\nstream\n{content}endstream"
            };

            var pdf = new StringBuilder();
            var offsets = new List<int> { 0 };
            pdf.Append("%PDF-1.4\n");

            for (var i = 0; i < objects.Length; i++)
            {
                offsets.Add(Encoding.ASCII.GetByteCount(pdf.ToString()));
                pdf.Append(i + 1).Append(" 0 obj\n");
                pdf.Append(objects[i]).Append("\n");
                pdf.Append("endobj\n");
            }

            var xrefOffset = Encoding.ASCII.GetByteCount(pdf.ToString());
            pdf.Append("xref\n");
            pdf.Append("0 ").Append(objects.Length + 1).Append('\n');
            pdf.Append("0000000000 65535 f \n");

            for (var i = 1; i < offsets.Count; i++)
            {
                pdf.Append(offsets[i].ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
            }

            pdf.Append("trailer\n");
            pdf.Append("<< /Size ").Append(objects.Length + 1).Append(" /Root 1 0 R >>\n");
            pdf.Append("startxref\n");
            pdf.Append(xrefOffset.ToString(CultureInfo.InvariantCulture)).Append('\n');
            pdf.Append("%%EOF");

            return Encoding.ASCII.GetBytes(pdf.ToString());
        }

        private static string EscapePdfText(string value)
        {
            var normalized = value
                .Replace('İ', 'I')
                .Replace('ı', 'i')
                .Replace('Ş', 'S')
                .Replace('ş', 's')
                .Replace('Ğ', 'G')
                .Replace('ğ', 'g')
                .Replace('Ü', 'U')
                .Replace('ü', 'u')
                .Replace('Ö', 'O')
                .Replace('ö', 'o')
                .Replace('Ç', 'C')
                .Replace('ç', 'c');

            return normalized
                .Replace("\\", "\\\\")
                .Replace("(", "\\(")
                .Replace(")", "\\)");
        }

        private static bool IsAllowedSliderImage(IFormFile file)
        {
            var extension = System.IO.Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                return false;
            }

            return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".gif", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAllowedPdf(IFormFile file)
        {
            var extension = System.IO.Path.GetExtension(file.FileName);
            return extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        }
    }
}

