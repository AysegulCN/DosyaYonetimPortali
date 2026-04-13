using DosyaYonetimPortali.API.Data;
using DosyaYonetimPortali.API.Data;
using DosyaYonetimPortali.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DosyaYonetimPortali.API.Controllers
{
    [Authorize] 
    [Route("api/[controller]")]
    [ApiController]
    public class InteractionController : ControllerBase
    {
        private readonly AppDbContext _context;

        public InteractionController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("add-comment/{fileId}")]
        public async Task<IActionResult> AddComment(int fileId, [FromBody] string text)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var comment = new FileComment { AppFileId = fileId, AppUserId = userId, CommentText = text };
            await _context.FileComments.AddAsync(comment);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Yorum eklendi." });
        }

        [HttpGet("my-notifications")]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _context.Notifications
                .Where(n => n.AppUserId == userId)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();
            return Ok(notifications);
        }
    }
}