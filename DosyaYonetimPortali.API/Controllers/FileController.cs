using DosyaYonetimPortali.API.Data; // Notification ve AppDbContext için eklendi
using DosyaYonetimPortali.API.Models;
using DosyaYonetimPortali.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
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
        private readonly AppDbContext _context; // Bildirim eklemek için eklendi

        public FileController(IGenericRepository<AppFile> fileRepository, UserManager<AppUser> userManager, IWebHostEnvironment env, AppDbContext context)
        {
            _fileRepository = fileRepository;
            _userManager = userManager;
            _env = env;
            _context = context;
        }

        // 1. DOSYA YÜKLEME (Anti-Virüs + Akıllı İsimlendirme + Kota)
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] int? folderId)
        {
            if (file == null || file.Length == 0) return BadRequest("Dosya seçilmedi.");

            // GÜVENLİK FİLTRESİ
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".docx", ".xlsx", ".txt", ".zip" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { Message = "Güvenlik İhlali: Sadece resim, ofis belgeleri, pdf ve zip dosyaları yüklenebilir!" });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            var roles = await _userManager.GetRolesAsync(user);

            // KOTA KONTROLÜ
            long maxQuota = roles.Contains("PremiumUser") ? 5368709120 : 104857600;
            var userFiles = await _fileRepository.WhereAsync(f => f.AppUserId == userId && !f.IsDeleted);
            long totalUsedSpace = userFiles.Sum(f => f.Size);

            if (totalUsedSpace + file.Length > maxQuota)
            {
                return BadRequest($"Kota aşıldı! Sınırınız: {maxQuota / 1024 / 1024} MB.");
            }

            // AKILLI İSİM ÇAKIŞMA ÇÖZÜCÜ (Dropbox Mantığı: Dosya(1).pdf)
            var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
            var finalFileName = file.FileName;
            int counter = 1;

            var folderFiles = await _fileRepository.WhereAsync(f => f.FolderId == folderId && f.AppUserId == userId && !f.IsDeleted);

            while (folderFiles.Any(f => f.FileName == finalFileName))
            {
                finalFileName = $"{originalFileName}({counter}){extension}";
                counter++;
            }

            // FİZİKSEL KAYIT
            var uploadsFolder = Path.Combine(_env.ContentRootPath, "Uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + finalFileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // VERİTABANI KAYIT
            var newFile = new AppFile
            {
                FileName = finalFileName, // Çakışma çözülmüş isim
                Extension = extension,
                Size = file.Length,
                PhysicalPath = filePath,
                FolderId = folderId,
                AppUserId = userId,
                UploadDate = DateTime.Now
            };

            await _fileRepository.AddAsync(newFile);
            await _fileRepository.SaveAsync();

            return Ok(new { Message = "Dosya başarıyla yüklendi.", SavedName = finalFileName, FileId = newFile.Id });
        }

        // 2. ÇÖP KUTUSUNA TAŞIMA (Premium Bildirimli)
        [HttpDelete("move-to-trash/{id}")]
        public async Task<IActionResult> MoveToTrash(int id)
        {
            var file = await _fileRepository.GetByIdAsync(id);
            if (file == null || file.IsDeleted) return NotFound("Dosya bulunamadı.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (file.AppUserId != userId) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            var roles = await _userManager.GetRolesAsync(user);

            file.IsDeleted = true;
            _fileRepository.Update(file);
            await _fileRepository.SaveAsync();

            // Freemium Pazarlama Bildirimi
            if (!roles.Contains("PremiumUser"))
            {
                var notification = new Notification
                {
                    AppUserId = userId,
                    Message = "Dosya çöpe atıldı. Silinen dosyalarınıza 30 gün boyunca erişebilmek için Premium pakete geçin!",
                    CreatedDate = DateTime.Now
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }

            return Ok(new { Message = "Dosya çöp kutusuna taşındı." });
        }

        // 3. ÇÖP KUTUSU ERİŞİMİ (Sadece Premiumlar İçin Kilit)
        [HttpGet("trash-bin")]
        public async Task<IActionResult> GetTrashBin()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            var roles = await _userManager.GetRolesAsync(user);

            if (!roles.Contains("PremiumUser"))
            {
                return BadRequest(new { Message = "Çöp kutusunu görmek ve dosyaları kurtarmak için Premium abonelik gereklidir." });
            }

            var trashedFiles = await _fileRepository.WhereAsync(f => f.AppUserId == userId && f.IsDeleted);
            return Ok(trashedFiles.Select(f => new { f.Id, f.FileName, f.Size, f.UploadDate }));
        }

        // 4. RESTORE (Geri Getirme)
        [HttpPut("restore-from-trash/{id}")]
        public async Task<IActionResult> RestoreFromTrash(int id)
        {
            var file = await _fileRepository.GetByIdAsync(id);
            if (file == null || !file.IsDeleted) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (file.AppUserId != userId) return Unauthorized();

            file.IsDeleted = false;
            _fileRepository.Update(file);
            await _fileRepository.SaveAsync();

            return Ok(new { Message = "Dosya geri getirildi." });
        }

        // 5. LİSTELEME, ARAMA VE SAYFALAMA
        [HttpGet("my-files")]
        public async Task<IActionResult> GetMyFiles([FromQuery] int? folderId = null, [FromQuery] string? searchTerm = null, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var files = await _fileRepository.WhereAsync(f => f.AppUserId == userId && !f.IsDeleted && f.FolderId == folderId);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                files = files.Where(f => f.FileName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            int totalRecords = files.Count();
            var pagedFiles = files.OrderByDescending(f => f.UploadDate).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            return Ok(new { TotalRecords = totalRecords, CurrentPage = pageNumber, Files = pagedFiles });
        }

        // 6. DİĞER FONKSİYONLAR (Share, Rename, Star, Move, Zip)
        [HttpPost("share/{id}")]
        public async Task<IActionResult> CreateShareLink(int id, [FromQuery] int expireDays = 7)
        {
            var file = await _fileRepository.GetByIdAsync(id);
            if (file == null || file.IsDeleted) return NotFound();
            file.ShareToken = Guid.NewGuid();
            file.ShareExpiration = DateTime.Now.AddDays(expireDays);
            _fileRepository.Update(file);
            await _fileRepository.SaveAsync();
            return Ok(new { ShareUrl = $"{Request.Scheme}://{Request.Host}/api/Shared/download/{file.ShareToken}" });
        }

        [HttpPut("rename/{id}")]
        public async Task<IActionResult> RenameFile(int id, [FromBody] string newName)
        {
            var file = await _fileRepository.GetByIdAsync(id);
            if (file == null || file.IsDeleted) return NotFound();
            file.FileName = newName;
            _fileRepository.Update(file);
            await _fileRepository.SaveAsync();
            return Ok(new { Message = "Dosya ismi güncellendi." });
        }

        [HttpPut("toggle-star/{id}")]
        public async Task<IActionResult> ToggleStar(int id)
        {
            var file = await _fileRepository.GetByIdAsync(id);
            if (file == null || file.IsDeleted) return NotFound();
            file.IsStarred = !file.IsStarred;
            _fileRepository.Update(file);
            await _fileRepository.SaveAsync();
            return Ok(new { IsStarred = file.IsStarred });
        }

        [HttpPut("move/{id}")]
        public async Task<IActionResult> MoveFile(int id, [FromQuery] int? newFolderId)
        {
            var file = await _fileRepository.GetByIdAsync(id);
            if (file == null || file.IsDeleted) return NotFound();
            file.FolderId = newFolderId;
            _fileRepository.Update(file);
            await _fileRepository.SaveAsync();
            return Ok(new { Message = "Dosya taşındı." });
        }

        [HttpPost("download-zip")]
        public async Task<IActionResult> DownloadFilesAsZip([FromBody] List<int> fileIds)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var files = await _fileRepository.WhereAsync(f => fileIds.Contains(f.Id) && f.AppUserId == userId && !f.IsDeleted);
            if (!files.Any()) return NotFound();

            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var file in files)
                {
                    if (System.IO.File.Exists(file.PhysicalPath))
                        archive.CreateEntryFromFile(file.PhysicalPath, file.FileName);
                }
            }
            memoryStream.Position = 0;
            return File(memoryStream.ToArray(), "application/zip", "Dosyalarim.zip");
        }
    }
}