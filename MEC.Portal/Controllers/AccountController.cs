using MEC.Application.Abstractions.Service.EmployeeService;
using MEC.Application.Abstractions.Service.LoginService;
using MEC.Portal.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MEC.Portal.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILoginService _loginService;
        private readonly IEmployeePortalService _employeePortalService;
        private readonly IConfiguration _configuration;

        public AccountController(
            ILoginService loginService,
            IEmployeePortalService employeePortalService,
            IConfiguration configuration)
        {
            _loginService = loginService;
            _employeePortalService = employeePortalService;
            _configuration = configuration;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            bool isAuthenticated = await _loginService.ValidateUserAsync(model.Email, model.Password);
            
            if (isAuthenticated)
            {
                var portalUser = await _employeePortalService.GetActivePortalUserByEmailAsync(model.Email);

                var claims = new List<Claim>
                {
                    new(ClaimTypes.Name, model.Email)
                };

                if (portalUser != null)
                {
                    if (portalUser.IsAdmin)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                    }

                    if (!string.IsNullOrWhiteSpace(portalUser.FirstName))
                    {
                        claims.Add(new Claim(ClaimTypes.GivenName, portalUser.FirstName.Trim()));
                    }

                    if (!string.IsNullOrWhiteSpace(portalUser.LastName))
                    {
                        claims.Add(new Claim(ClaimTypes.Surname, portalUser.LastName.Trim()));
                    }
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                if (portalUser != null && await _employeePortalService.RequiresProfileCompletionAsync(model.Email))
                {
                    return RedirectToAction("Edit", "Profile", new { required = true });
                }

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Kullanıcı adı veya şifre hatalı.";
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
