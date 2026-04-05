using DosyaYonetimPortali.API.Models;
using DosyaYonetimPortali.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace DosyaYonetimPortali.API.Controllers
{
    [AllowAnonymous] 
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IGenericRepository<SystemLog> _logRepository;

        private readonly IGenericRepository<AppFile> _fileRepository;

        public AdminController(
            UserManager<AppUser> userManager,
            IGenericRepository<SystemLog> logRepository,
            IGenericRepository<AppFile> fileRepository) 
        {
            _userManager = userManager;
            _logRepository = logRepository;
            _fileRepository = fileRepository;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetAdminSummary()
        {
            var totalUsers = _userManager.Users.Count();

            var premiumUsersList = await _userManager.GetUsersInRoleAsync("Moderator");
            var premiumUsers = premiumUsersList.Count;

            var allFiles = await _fileRepository.GetAllAsync();

            var activeFiles = allFiles.Where(f => !f.IsDeleted).ToList();

            var totalFiles = activeFiles.Count;

            var totalStorageUsed = activeFiles.Sum(f => (long)f.Size);

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