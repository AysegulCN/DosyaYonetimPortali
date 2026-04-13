using DosyaYonetimPortali.API.Data;
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
        private readonly AppDbContext _context;

        public FileController(IGenericRepository<AppFile> fileRepository, UserManager<AppUser> userManager, IWebHostEnvironment env, AppDbContext context)
        {
            _fileRepository = fileRepository;
            _userManager = userManager;
            _env = env;
            _context = context;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] int? folderId)
        {
            if (file == null || file.Length == 0) return BadRequest("Dosya seçilmedi.");
            if (folderId == 0) folderId = null;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".docx", ".xlsx", ".txt", ".zip" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                return BadRequest(new { Message = "Güvenlik İhlali: Geçersiz dosya tipi!" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user.UsedStorage + file.Length > user.TotalStorageQuota)
                return BadRequest(new { Message = "Yetersiz depolama alanı!" });

            var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
            var finalFileName = file.FileName;
            int counter = 1;

            var folderFiles = await _fileRepository.WhereAsync(f => f.FolderId == folderId && f.AppUserId == userId && !f.IsDeleted);

            while (folderFiles.Any(f => f.FileName == finalFileName))
            {
                finalFileName = $"{originalFileName}({counter}){extension}";
                counter++;
            }

            var uploadsFolder = Path.Combine(_env.ContentRootPath, "Uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + finalFileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var newFile = new AppFile
            {
                FileName = finalFileName,
                Extension = extension,
                Size = file.Length,
                PhysicalPath = filePath,
                FolderId = folderId,
                AppUserId = userId,
                UploadDate = DateTime.Now,
                CreatedDate = DateTime.Now
            };

            await _fileRepository.AddAsync(newFile);
            await _fileRepository.SaveAsync();

            user.UsedStorage += file.Length;
            await _userManager.UpdateAsync(user);

            return Ok(new { Message = "Dosya yüklendi.", FileId = newFile.Id });
        }

        [HttpDelete("move-to-trash/{id}")]
        public async Task<IActionResult> MoveToTrash(int id)
        {
            var file = await _fileRepository.GetByIdAsync(id);
            if (file == null || file.IsDeleted) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (file.AppUserId != userId) return Unauthorized();

            file.IsDeleted = true;
            file.DeletedDate = DateTime.Now;
            _fileRepository.Update(file);
            await _fileRepository.SaveAsync();

            return Ok(new { Message = "Çöpe taşındı." });
        }

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

            return Ok(new { TotalRecords = totalRecords, Files = pagedFiles });
        }

        [HttpPut("toggle-star/{id}")]
        public async Task<IActionResult> ToggleStar(int id)
        {
            var file = await _fileRepository.GetByIdAsync(id);
            if (file == null || file.IsDeleted) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (file.AppUserId != userId) return Unauthorized();

            file.IsStarred = !file.IsStarred;
            file.UpdatedDate = DateTime.Now;

            _fileRepository.Update(file);
            await _fileRepository.SaveAsync();
            return Ok(new { IsStarred = file.IsStarred });
        }

        [HttpPut("rename/{id}")]
        public async Task<IActionResult> RenameFile(int id, [FromBody] string newName)
        {
            var file = await _fileRepository.GetByIdAsync(id);
            if (file == null || file.IsDeleted) return NotFound();

            file.FileName = newName;
            file.UpdatedDate = DateTime.Now;
            _fileRepository.Update(file);
            await _fileRepository.SaveAsync();
            return Ok(new { Message = "İsim güncellendi." });
        }

        [HttpPost("download-zip")]
        public async Task<IActionResult> DownloadFilesAsZip([FromBody] List<int> fileIds)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var files = await _fileRepository.WhereAsync(f => fileIds.Contains(f.Id) && f.AppUserId == userId && !f.IsDeleted);

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
            return File(memoryStream.ToArray(), "application/zip", "Dosyalar.zip");
        }

        // FileController'ın içine, diğer metotların altına ekle:

        [HttpGet("trash-bin")]
        public async Task<IActionResult> GetTrashBin()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // HERKESE AÇIK: Premium kontrolü yok!
            var trashedFiles = await _fileRepository.WhereAsync(f => f.AppUserId == userId && f.IsDeleted);

            return Ok(trashedFiles.Select(f => new { f.Id, f.FileName, f.Size, f.UploadDate, f.DeletedDate }));
        }

        [HttpPut("restore-from-trash/{id}")]
        public async Task<IActionResult> RestoreFromTrash(int id)
        {
            var file = await _fileRepository.GetByIdAsync(id);
            if (file == null || !file.IsDeleted) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (file.AppUserId != userId) return Unauthorized();

            file.IsDeleted = false; // Çöpten çıkart
            _fileRepository.Update(file);
            await _fileRepository.SaveAsync();

            return Ok(new { Message = "Dosya başarıyla geri getirildi." });
        }
    }
}