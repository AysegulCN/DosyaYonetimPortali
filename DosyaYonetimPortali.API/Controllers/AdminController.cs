using DosyaYonetimPortali.API.Models;
using DosyaYonetimPortali.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace DosyaYonetimPortali.API.Controllers
{
    [AllowAnonymous] // MVC'den rahatça erişebilmek için kilidi açık bırakmıştık
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IGenericRepository<SystemLog> _logRepository;

        // YENİ: Dosya veritabanına bağlanmak için File Repository'yi ekliyoruz
        private readonly IGenericRepository<AppFile> _fileRepository;

        public AdminController(
            UserManager<AppUser> userManager,
            IGenericRepository<SystemLog> logRepository,
            IGenericRepository<AppFile> fileRepository) // Constructor'a dahil ettik
        {
            _userManager = userManager;
            _logRepository = logRepository;
            _fileRepository = fileRepository;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetAdminSummary()
        {
            // 1. GERÇEK: Sistemdeki Toplam Kullanıcı Sayısı
            var totalUsers = _userManager.Users.Count();

            // 2. GERÇEK: "Moderator" veya "PremiumUser" rolündeki kullanıcı sayısı
            var premiumUsersList = await _userManager.GetUsersInRoleAsync("Moderator");
            var premiumUsers = premiumUsersList.Count;

            // 3. GERÇEK: Veritabanındaki tüm dosyaları çek
            var allFiles = await _fileRepository.GetAllAsync();

            // Sadece çöp kutusunda OLMAYAN (aktif) dosyaları filtrele
            var activeFiles = allFiles.Where(f => !f.IsDeleted).ToList();

            // 4. GERÇEK: Aktif Dosya Sayısı
            var totalFiles = activeFiles.Count;

            // 5. GERÇEK: Aktif Dosyaların Toplam Boyutu (Byte cinsinden toplayıp gönderiyoruz)
            var totalStorageUsed = activeFiles.Sum(f => (long)f.Size);

            // Verileri paketleyip MVC'nin beklediği isimlerle fırlat!
            return Ok(new
            {
                totalUsers = totalUsers,
                premiumUsers = premiumUsers,
                totalFiles = totalFiles,
                totalStorageUsed = totalStorageUsed
            });
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
    }
}