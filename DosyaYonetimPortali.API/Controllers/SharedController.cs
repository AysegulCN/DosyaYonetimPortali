using DosyaYonetimPortali.API.Models;
using DosyaYonetimPortali.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DosyaYonetimPortali.API.Controllers
{
    // DİKKAT: Burada [Authorize] YOK! Çünkü dışarıdan biri linke tıklayıp indirecek.
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class SharedController : ControllerBase
    {
        private readonly IGenericRepository<AppFile> _fileRepository;

        public SharedController(IGenericRepository<AppFile> fileRepository)
        {
            _fileRepository = fileRepository;
        }

        // DIŞARIDAN BİRİ PAYLAŞIM LİNKİNE TIKLADIĞINDA ÇALIŞACAK UÇ
        [HttpGet("download/{shareToken}")]
        public async Task<IActionResult> DownloadSharedFile(Guid shareToken)
        {
            // Token'a sahip olan dosyayı veri tabanından buluyoruz
            var files = await _fileRepository.WhereAsync(f => f.ShareToken == shareToken && !f.IsDeleted);
            var file = files.FirstOrDefault();

            if (file == null) return NotFound("Geçersiz veya silinmiş bir bağlantı.");

            // Süresi dolmuş mu kontrolü
            if (file.ShareExpiration.HasValue && file.ShareExpiration.Value < DateTime.Now)
            {
                return BadRequest("Bu paylaşım bağlantısının süresi dolmuş.");
            }

            if (!System.IO.File.Exists(file.PhysicalPath))
                return NotFound("Fiziksel dosya sunucuda bulunamadı.");

            // Her şey tamamsa dosyayı indir!
            var fileBytes = await System.IO.File.ReadAllBytesAsync(file.PhysicalPath);
            return File(fileBytes, "application/octet-stream", file.FileName);
        }
    }
}