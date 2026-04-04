using DosyaYonetimPortali.API.Models;
using DosyaYonetimPortali.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DosyaYonetimPortali.API.Controllers
{
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

        [HttpGet("system-logs")]
        public async Task<IActionResult> GetSystemLogs()
        {
            var logs = await _logRepository.GetAllAsync();
            return Ok(logs);
        }
        // Herkese açık olması için AllowAnonymous ekliyoruz!
        [AllowAnonymous]
        [HttpGet("summary")]
        public IActionResult GetAdminSummary()
        {
            // Sistemdeki toplam kullanıcı sayısını çekiyoruz
            var totalUsers = _userManager.Users.Count();

            // Verileri Admin Dashboard'un beklediği isimlerle gönderiyoruz
            return Ok(new
            {
                totalUsers = totalUsers,
                premiumUsers = 3, // Şimdilik deneme verisi
                totalFiles = 145, // Şimdilik deneme verisi
                totalStorageUsed = 5368709120 // Yaklaşık 5 GB bayt karşılığı
            });
        }

    }
}