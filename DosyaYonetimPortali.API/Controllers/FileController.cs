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
    public class FileController : ControllerBase
    {
        private readonly IGenericRepository<AppFile> _fileRepository;
        private readonly UserManager<AppUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public FileController(IGenericRepository<AppFile> fileRepository, UserManager<AppUser> userManager, IWebHostEnvironment env)
        {
            _fileRepository = fileRepository;
            _userManager = userManager;
            _env = env;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] int? folderId)
        {
            if (file == null || file.Length == 0) return BadRequest("Dosya seçilmedi.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            var roles = await _userManager.GetRolesAsync(user);

            // 1. KOTA KONTROLÜ (Freemium Mantığı)
            long maxQuota = roles.Contains("PremiumUser") ? 5368709120 : 104857600; // Premium 5GB, Normal 100MB

            var userFiles = await _fileRepository.WhereAsync(f => f.AppUserId == userId && !f.IsDeleted);
            long totalUsedSpace = userFiles.Sum(f => f.Size);

            if (totalUsedSpace + file.Length > maxQuota)
            {
                return BadRequest($"Kota aşıldı! Mevcut kullanım: {totalUsedSpace / 1024 / 1024} MB. Sınırınız: {maxQuota / 1024 / 1024} MB.");
            }

            // 2. DOSYA YÜKLEME İŞLEMİ
            var uploadsFolder = Path.Combine(_env.ContentRootPath, "Uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var newFile = new AppFile
            {
                FileName = file.FileName,
                Extension = Path.GetExtension(file.FileName),
                Size = file.Length,
                PhysicalPath = filePath,
                FolderId = folderId,
                AppUserId = userId,
                UploadDate = DateTime.Now
            };

            await _fileRepository.AddAsync(newFile);
            await _fileRepository.SaveAsync();

            return Ok(new { Message = "Dosya başarıyla yüklendi.", FileId = newFile.Id, KalanKotaMB = (maxQuota - (totalUsedSpace + file.Length)) / 1024 / 1024 });
        }

        // 3. ÇÖP KUTUSUNA GÖNDERME (Soft Delete)
        [HttpDelete("move-to-trash/{id}")]
        public async Task<IActionResult> MoveToTrash(int id)
        {
            var file = await _fileRepository.GetByIdAsync(id);
            if (file == null || file.IsDeleted) return NotFound("Dosya bulunamadı.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (file.AppUserId != userId) return Unauthorized("Bu işlem için yetkiniz yok.");

            file.IsDeleted = true; // Fiziksel olarak silmiyoruz, sadece işaretliyoruz!
            _fileRepository.Update(file);
            await _fileRepository.SaveAsync();

            return Ok(new { Message = "Dosya çöp kutusuna taşındı." });
        }

        // 4. ÇÖP KUTUSUNDAKİLERİ GETİR (Trash Bin Arayüzü İçin)
        [HttpGet("trash-bin")]
        public async Task<IActionResult> GetTrashBin()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var trashedFiles = await _fileRepository.WhereAsync(f => f.AppUserId == userId && f.IsDeleted);

            var result = trashedFiles.Select(f => new { f.Id, f.FileName, f.Size, f.UploadDate }).ToList();
            return Ok(result);
        }
    }
}