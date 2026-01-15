using MEC.Application.Abstractions.Service.LdapService;
using Microsoft.AspNetCore.Mvc;

namespace MEC.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LdapController : Controller
    {
        private readonly ILdapService _ldapService;
        public LdapController(ILdapService ldapService)
        {
            _ldapService = ldapService;
        }
        [HttpGet("SyncUsers")]
        public async Task<IActionResult> SyncUsers()
        {
            try
            {
                int count = await _ldapService.SyncUsersFromLdapAsync();
                return Ok(new { Message = "Senkronizasyon başarıyla tamamlandı.", ProcessedUsers = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Senkronizasyon sırasında hata oluştu.", Error = ex.Message });
            }
        }
    }
}
