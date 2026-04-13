using DosyaYonetimPortali.API.DTOs;
using DosyaYonetimPortali.API.Models;
using DosyaYonetimPortali.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DosyaYonetimPortali.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly UserManager<AppUser> _userManager;

        public AuthController(IAuthService authService, UserManager<AppUser> userManager)
        {
            _authService = authService;
            _userManager = userManager;
        }

        
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _authService.RegisterAsync(request);
            if (result.IsSuccessful)
                return Ok(result);

            return BadRequest(result);
        }

        // BÜYÜK DÜZELTME: Servis üzerinden temiz giriş
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto model)
        {
            var result = await _authService.LoginAsync(model);
            if (result.IsSuccessful)
            {
                return Ok(result); // Token ve detaylar burada dönecek
            }

            return Unauthorized(new { message = result.ErrorMessage });
        }

        [Authorize]
        [HttpGet("my-profile")]
        public IActionResult GetMyProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            return Ok(new
            {
                Id = userId,
                Name = User.FindFirstValue(ClaimTypes.Name),
                Email = User.FindFirstValue(ClaimTypes.Email),
                Role = User.FindFirstValue(ClaimTypes.Role) ?? "User"
            });
        }

        [Authorize]
        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Lütfen bir resim seçin.");
            if (!file.ContentType.StartsWith("image/")) return BadRequest("Sadece resim dosyası yükleyebilirsiniz.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Avatars");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{userId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            if (!string.IsNullOrEmpty(user.AvatarPath) && System.IO.File.Exists(user.AvatarPath))
            {
                System.IO.File.Delete(user.AvatarPath);
            }

            user.AvatarPath = filePath;
            await _userManager.UpdateAsync(user);

            return Ok(new { Message = "Profil fotoğrafınız başarıyla güncellendi." });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound("Bu e-posta adresine ait kullanıcı bulunamadı.");

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            return Ok(new { SimulatedEmailContent = $"Şifrenizi sıfırlamak için şu kodu kullanın: {resetToken}" });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromQuery] string email, [FromQuery] string resetToken, [FromBody] string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (result.Succeeded) return Ok(new { Message = "Şifreniz sıfırlandı." });
            return BadRequest(result.Errors);
        }
        
    }
}