using DosyaYonetimPortali.API.DTOs;
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
    public class FolderController : ControllerBase
    {
        private readonly IGenericRepository<Folder> _folderRepository;

        public FolderController(IGenericRepository<Folder> folderRepository)
        {
            _folderRepository = folderRepository;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateFolder([FromBody] FolderDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var newFolder = new Folder
            {
                Name = request.Name,
                ParentFolderId = request.ParentFolderId, 
                AppUserId = userId,
                CreatedDate = DateTime.Now
            };

            await _folderRepository.AddAsync(newFolder);
            await _folderRepository.SaveAsync(); 

            return Ok(new { Message = "Klasör başarıyla oluşturuldu.", FolderId = newFolder.Id });
        }

        [HttpGet("my-folders")]
        public async Task<IActionResult> GetMyFolders([FromQuery] int? parentFolderId = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var folders = await _folderRepository.WhereAsync(f => f.AppUserId == userId && f.ParentFolderId == parentFolderId);

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