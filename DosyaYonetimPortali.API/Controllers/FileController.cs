using DosyaYonetimPortali.API.Models;
using DosyaYonetimPortali.API.Repositories;
using Microsoft.AspNetCore.Authorization;
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
        private readonly IWebHostEnvironment _env;

        public FileController(IGenericRepository<AppFile> fileRepository, IWebHostEnvironment env)
        {
            _fileRepository = fileRepository;
            _env = env; // Sunucu klasör yollarını bulmak için
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] int? folderId)
        {
            if (file == null || file.Length == 0) return BadRequest("Dosya seçilmedi.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Dosyaları sunucuda "Uploads" klasörüne kaydedeceğiz
            var uploadsFolder = Path.Combine(_env.ContentRootPath, "Uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            // Çakışmayı önlemek için benzersiz isim
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

            return Ok(new { Message = "Dosya yüklendi.", FileId = newFile.Id });
        }

        // 2. DOSYA İNDİRME (DOWNLOAD)
        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadFile(int id)
        {
            var file = await _fileRepository.GetByIdAsync(id);
            if (file == null) return NotFound("Dosya bulunamadı.");

            // Sadece kendi dosyasını indirebilir güvenlik kontrolü
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (file.AppUserId != userId) return Unauthorized("Bu dosyayı indirme yetkiniz yok.");

            if (!System.IO.File.Exists(file.PhysicalPath))
                return NotFound("Fiziksel dosya sunucuda bulunamadı.");

            var fileBytes = await System.IO.File.ReadAllBytesAsync(file.PhysicalPath);
            return File(fileBytes, "application/octet-stream", file.FileName);
        }

        // 3. DOSYA SİLME (DELETE)
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteFile(int id)
        {
            var file = await _fileRepository.GetByIdAsync(id);
            if (file == null) return NotFound("Dosya bulunamadı.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (file.AppUserId != userId) return Unauthorized("Bu dosyayı silme yetkiniz yok.");

            // Önce sunucudan (klasörden) fiziksel olarak siliyoruz
            if (System.IO.File.Exists(file.PhysicalPath))
            {
                System.IO.File.Delete(file.PhysicalPath);
            }

            // Sonra veri tabanından siliyoruz
            _fileRepository.Delete(file);
            await _fileRepository.SaveAsync();

            return Ok(new { Message = "Dosya başarıyla silindi." });
        }

    }
}