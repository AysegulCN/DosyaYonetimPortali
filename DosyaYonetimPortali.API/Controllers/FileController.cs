using DosyaYonetimPortali.API.Models;
using DosyaYonetimPortali.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IO.Compression;

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

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".docx", ".xlsx", ".txt", ".zip" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { Message = "Güvenlik İhlali: Sadece resim, ofis belgeleri, pdf ve zip dosyaları yüklenebilir. Çalıştırılabilir (.exe, .bat vb.) dosyalar sistem tarafından engellenmiştir!" });
            }

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
        // 5. ÇÖP KUTUSUNDAN GERİ GETİRME (Restore)
        [HttpPut("restore-from-trash/{id}")]
        public async Task<IActionResult> RestoreFromTrash(int id)
        {
            var file = await _fileRepository.GetByIdAsync(id);
            if (file == null || !file.IsDeleted) return NotFound("Çöp kutusunda böyle bir dosya bulunamadı.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (file.AppUserId != userId) return Unauthorized("Bu işlem için yetkiniz yok.");

            // Dosyayı hayata döndürüyoruz!
            file.IsDeleted = false;
            _fileRepository.Update(file);
            await _fileRepository.SaveAsync();

            return Ok(new { Message = "Dosya başarıyla çöp kutusundan çıkarıldı ve geri getirildi." });
        }

        // 6. DOSYAYI PAYLAŞIMA AÇMA (Share Link)
        [HttpPost("share/{id}")]
        public async Task<IActionResult> CreateShareLink(int id, [FromQuery] int expireDays = 7)
        {
            var file = await _fileRepository.GetByIdAsync(id);
            if (file == null || file.IsDeleted) return NotFound("Dosya bulunamadı.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (file.AppUserId != userId) return Unauthorized("Bu dosyayı paylaşma yetkiniz yok.");

            // Dosyaya benzersiz bir şifre (GUID) ve son kullanma tarihi atıyoruz
            file.ShareToken = Guid.NewGuid();
            file.ShareExpiration = DateTime.Now.AddDays(expireDays);

            _fileRepository.Update(file);
            await _fileRepository.SaveAsync();

            // Kullanıcıya vereceğimiz indirme linki (Swagger'da test edebilmek için API ucumuzu veriyoruz)
            var downloadLink = $"{Request.Scheme}://{Request.Host}/api/Shared/download/{file.ShareToken}";

            return Ok(new
            {
                Message = $"{expireDays} gün geçerli paylaşım linki oluşturuldu.",
                ShareUrl = downloadLink,
                ExpiresAt = file.ShareExpiration
            });
        }

        [HttpGet("my-files")]
        public async Task<IActionResult> GetMyFiles(
            [FromQuery] int? folderId = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. Temel Sorgu: Kullanıcının silinmemiş dosyaları (Eğer folderId varsa o klasörün içindekiler)
            var files = await _fileRepository.WhereAsync(f => f.AppUserId == userId && !f.IsDeleted && f.FolderId == folderId);

            // 2. Arama (Search) Filtresi: Eğer kullanıcı bir kelime girdiyse o kelimeyi isminde ara
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                files = files.Where(f => f.FileName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Toplam dosya sayısını al (Sayfa sayısını hesaplamak için)
            int totalRecords = files.Count();
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            // 3. Sayfalama (Pagination): İstenilen sayfaya git ve sadece o sayfanın verisini kopar
            var pagedFiles = files
                .OrderByDescending(f => f.UploadDate) // En son yüklenen en üstte görünsün
                .Skip((pageNumber - 1) * pageSize)    // Önceki sayfaları atla
                .Take(pageSize)                       // Sadece bu sayfanın boyutu kadarını al
                .Select(f => new
                {
                    f.Id,
                    f.FileName,
                    f.Extension,
                    f.Size,
                    f.UploadDate,
                    f.ShareToken // Paylaşımda olup olmadığını arayüzde göstermek için
                }).ToList();

            // Sonuçları ve sayfa bilgilerini şık bir paket halinde dışarı yolla
            return Ok(new
            {
                TotalRecords = totalRecords,
                TotalPages = totalPages,
                CurrentPage = pageNumber,
                PageSize = pageSize,
                Files = pagedFiles
            });
        }
        // 8. DOSYA İSMİ DEĞİŞTİRME (Rename)
        [HttpPut("rename/{id}")]
        public async Task<IActionResult> RenameFile(int id, [FromBody] string newName)
        {
            var file = await _fileRepository.GetByIdAsync(id);
            if (file == null || file.IsDeleted) return NotFound("Dosya bulunamadı.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (file.AppUserId != userId) return Unauthorized("Yetkiniz yok.");

            if (string.IsNullOrWhiteSpace(newName)) return BadRequest("Yeni isim boş olamaz.");

            file.FileName = newName;
            _fileRepository.Update(file);
            await _fileRepository.SaveAsync();

            return Ok(new { Message = "Dosya ismi başarıyla değiştirildi.", NewName = file.FileName });
        }

        // 9. YILDIZLAMA / FAVORİLERE EKLEME-ÇIKARMA (Toggle Star)
        [HttpPut("toggle-star/{id}")]
        public async Task<IActionResult> ToggleStar(int id)
        {
            var file = await _fileRepository.GetByIdAsync(id);
            if (file == null || file.IsDeleted) return NotFound("Dosya bulunamadı.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (file.AppUserId != userId) return Unauthorized("Yetkiniz yok.");

            // Yıldızlıysa yıldızı kaldır, değilse yıldızla (Aç/Kapat mantığı)
            file.IsStarred = !file.IsStarred;
            _fileRepository.Update(file);
            await _fileRepository.SaveAsync();

            var status = file.IsStarred ? "Favorilere eklendi." : "Favorilerden çıkarıldı.";
            return Ok(new { Message = status, IsStarred = file.IsStarred });
        }
        // 10. DOSYA TAŞIMA (Klasör Değiştirme)
        [HttpPut("move/{id}")]
        public async Task<IActionResult> MoveFile(int id, [FromQuery] int? newFolderId)
        {
            var file = await _fileRepository.GetByIdAsync(id);
            if (file == null || file.IsDeleted) return NotFound("Dosya bulunamadı.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (file.AppUserId != userId) return Unauthorized("Yetkiniz yok.");

            // Dosyanın klasör ID'sini güncelliyoruz (null ise ana dizine çıkar)
            file.FolderId = newFolderId;
            _fileRepository.Update(file);
            await _fileRepository.SaveAsync();

            return Ok(new { Message = "Dosya başarıyla taşındı." });
        }

        // 11. SEÇİLİ DOSYALARI ZİP OLARAK İNDİRME (Toplu İndirme)
        [HttpPost("download-zip")]
        public async Task<IActionResult> DownloadFilesAsZip([FromBody] List<int> fileIds)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Sadece kullanıcıya ait ve silinmemiş dosyaları bul
            var files = await _fileRepository.WhereAsync(f => fileIds.Contains(f.Id) && f.AppUserId == userId && !f.IsDeleted);

            if (!files.Any()) return NotFound("İndirilecek geçerli dosya bulunamadı.");

            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var file in files)
                {
                    if (System.IO.File.Exists(file.PhysicalPath))
                    {
                        // Dosyayı Zip arşivine ekle
                        archive.CreateEntryFromFile(file.PhysicalPath, file.FileName);
                    }
                }
            }

            memoryStream.Position = 0;
            return File(memoryStream.ToArray(), "application/zip", "Dosyalarim.zip");
        }

    }
}