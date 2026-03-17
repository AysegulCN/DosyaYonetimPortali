using DosyaYonetimPortali.API.DTOs;
using DosyaYonetimPortali.API.Models;
using DosyaYonetimPortali.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IO; // Dosya işlemleri için eklendi

namespace DosyaYonetimPortali.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly UserManager<AppUser> _userManager; // HATAYI ÇÖZEN SİHİRLİ SATIR

        // Constructor güncellendi, _userManager içeriye alındı
        public AuthController(IAuthService authService, UserManager<AppUser> userManager)
        {
            _authService = authService;
            _userManager = userManager;
        }

        [HttpPost("setup-roles")]
        public async Task<IActionResult> SetupRoles()
        {
            await _authService.CreateDefaultRolesAsync();
            return Ok(new { Message = "Roller başarıyla oluşturuldu." });
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

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _authService.LoginAsync(request);
            if (result.IsSuccessful)
                return Ok(result);

            return Unauthorized(result);
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return Ok(new { Message = "Başarıyla çıkış yapıldı. Lütfen istemci tarafındaki Token'ı temizleyin." });
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

            // Sadece resim dosyalarına izin ver
            if (!file.ContentType.StartsWith("image/")) return BadRequest("Sadece resim dosyası yükleyebilirsiniz.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            // Resmi sunucuya kaydet
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Avatars");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{userId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Eski avatar varsa fiziksel olarak sil (sunucu şişmesin)
            if (!string.IsNullOrEmpty(user.AvatarPath) && System.IO.File.Exists(user.AvatarPath))
            {
                System.IO.File.Delete(user.AvatarPath);
            }

            user.AvatarPath = filePath;
            await _userManager.UpdateAsync(user);

            return Ok(new { Message = "Profil fotoğrafınız başarıyla güncellendi." });
        }

        // 1. Şifre Sıfırlama Talebi (Email Girilir)
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound("Bu e-posta adresine ait bir kullanıcı bulunamadı.");

            // Şifre sıfırlama için tek kullanımlık güvenli bir Token (Anahtar) üretilir
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            // NOT: Gerçek projede bu Token kullanıcıya e-posta olarak gönderilir.
            // Vize projesi olduğu için SMTP (Mail) sunucusu kurmadık, Swagger ekranına basıyoruz.
            return Ok(new
            {
                Message = "Şifre sıfırlama bağlantısı e-posta adresinize gönderildi (Simülasyon).",
                SimulatedEmailContent = $"Şifrenizi sıfırlamak için şu kodu kullanın: {resetToken}"
            });
        }

        // 2. Yeni Şifreyi Belirleme İşlemi
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromQuery] string email, [FromQuery] string resetToken, [FromBody] string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (result.Succeeded)
            {
                return Ok(new { Message = "Şifreniz başarıyla sıfırlandı. Yeni şifrenizle giriş yapabilirsiniz." });
            }

            return BadRequest(result.Errors);
        }
    }
}