using DosyaYonetimPortali.API.Models;
using DosyaYonetimPortali.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DosyaYonetimPortali.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IGenericRepository<AppFile> _fileRepository;
        private readonly UserManager<AppUser> _userManager;

        public DashboardController(IGenericRepository<AppFile> fileRepository, UserManager<AppUser> userManager)
        {
            _fileRepository = fileRepository;
            _userManager = userManager;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            var roles = await _userManager.GetRolesAsync(user);

            long maxQuota = roles.Contains("PremiumUser") ? 5368709120 : 104857600; 

            var allUserFiles = await _fileRepository.WhereAsync(f => f.AppUserId == userId);

            long usedSpace = allUserFiles.Where(f => !f.IsDeleted).Sum(f => f.Size);
            long trashSpace = allUserFiles.Where(f => f.IsDeleted).Sum(f => f.Size);

            int activeFileCount = allUserFiles.Count(f => !f.IsDeleted);
            int starredFileCount = allUserFiles.Count(f => !f.IsDeleted && f.IsStarred);
            int trashedFileCount = allUserFiles.Count(f => f.IsDeleted);

            var recentFiles = allUserFiles
                .Where(f => !f.IsDeleted)
                .OrderByDescending(f => f.UploadDate)
                .Take(5)
                .Select(f => new { f.Id, f.FileName, f.Extension, f.Size, f.UploadDate, f.IsStarred })
                .ToList();

            return Ok(new
            {
                WelcomeMessage = $"Hoş geldin, {user.FirstName}!",
                Role = roles.FirstOrDefault() ?? "User",
                Storage = new
                {
                    MaxQuotaMB = maxQuota / 1024 / 1024,
                    UsedSpaceMB = usedSpace / 1024 / 1024,
                    TrashSpaceMB = trashSpace / 1024 / 1024,
                    FreeSpaceMB = (maxQuota - usedSpace) / 1024 / 1024
                },
                Counts = new
                {
                    ActiveFiles = activeFileCount,
                    StarredFiles = starredFileCount,
                    TrashedFiles = trashedFileCount
                },
                RecentFiles = recentFiles
            });
        }
    }
}