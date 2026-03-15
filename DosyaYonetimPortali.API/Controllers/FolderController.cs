using DosyaYonetimPortali.API.DTOs;
using DosyaYonetimPortali.API.Models;
using DosyaYonetimPortali.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DosyaYonetimPortali.API.Controllers
{
    [Authorize] // DİKKAT: Bu etiket sayesinde artık buraya sadece Token'ı olanlar girebilir!
    [Route("api/[controller]")]
    [ApiController]
    public class FolderController : ControllerBase
    {
        private readonly IGenericRepository<Folder> _folderRepository;

        // Garsonumuz (Controller), Aşçıyı (Repository) çağırıyor
        public FolderController(IGenericRepository<Folder> folderRepository)
        {
            _folderRepository = folderRepository;
        }

        // 1. YENİ KLASÖR OLUŞTURMA
        [HttpPost("create")]
        public async Task<IActionResult> CreateFolder([FromBody] FolderDto request)
        {
            // Giriş yapmış kullanıcının ID'sini sistemin hafızasından (Token'dan) otomatik yakalıyoruz!
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var newFolder = new Folder
            {
                Name = request.Name,
                ParentFolderId = request.ParentFolderId, // Eğer null ise ana dizindedir
                AppUserId = userId,
                CreatedDate = DateTime.Now
            };

            await _folderRepository.AddAsync(newFolder);
            await _folderRepository.SaveAsync(); // Veri tabanına yaz

            return Ok(new { Message = "Klasör başarıyla oluşturuldu.", FolderId = newFolder.Id });
        }

        // 2. KULLANICININ KLASÖRLERİNİ GETİRME (Google Drive Ana Ekranı)
        [HttpGet("my-folders")]
        public async Task<IActionResult> GetMyFolders([FromQuery] int? parentFolderId = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Sadece bu kullanıcının ve istediği dizindeki klasörleri veri tabanından getir
            var folders = await _folderRepository.WhereAsync(f => f.AppUserId == userId && f.ParentFolderId == parentFolderId);

            // Gelen verileri güvenli DTO'ya çevirip dışarı yolla
            var folderDtos = folders.Select(f => new FolderDto
            {
                Id = f.Id,
                Name = f.Name,
                CreatedDate = f.CreatedDate,
                ParentFolderId = f.ParentFolderId
            }).ToList();

            return Ok(folderDtos);
        }
    }
}