using DosyaYonetimPortali.API.Models;
using DosyaYonetimPortali.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DosyaYonetimPortali.API.Controllers
{
    // DİKKAT: Burası işin sihri! Sadece "Admin" rolüne sahip Token'lar buraya girebilir.
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IGenericRepository<SystemLog> _logRepository;

        public AdminController(UserManager<AppUser> userManager, IGenericRepository<SystemLog> logRepository)
        {
            _userManager = userManager;
            _logRepository = logRepository;
        }

        // 1. ADMİN İŞLEMİ: Tüm Kullanıcıları Listele
        [HttpGet("all-users")]
        public IActionResult GetAllUsers()
        {
            var users = _userManager.Users.Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email
            }).ToList();

            return Ok(users);
        }

        // 2. ADMİN İŞLEMİ: Sistemdeki Tüm Hareketleri (Logları) Gör
        [HttpGet("system-logs")]
        public async Task<IActionResult> GetSystemLogs()
        {
            var logs = await _logRepository.GetAllAsync();
            return Ok(logs);
        }
    }
}