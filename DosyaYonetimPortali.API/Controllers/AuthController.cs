using DosyaYonetimPortali.API.DTOs;
using DosyaYonetimPortali.API.Models;
using DosyaYonetimPortali.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.IO; 
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
        public async Task<IActionResult> Login([FromBody] UserLoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.UserName),
                };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var token = GetToken(authClaims);

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token), 
                    roles = userRoles 
                });
            }
            return Unauthorized(new { message = "Giriş başarısız. Şifrenizi veya e-postanızı kontrol edin." });
        }

        private SecurityToken GetToken(List<Claim> authClaims)
        {
            throw new NotImplementedException();
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
            if (user == null) return NotFound("Bu e-posta adresine ait bir kullanıcı bulunamadı.");

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            
            return Ok(new
            {
                Message = "Şifre sıfırlama bağlantısı e-posta adresinize gönderildi (Simülasyon).",
                SimulatedEmailContent = $"Şifrenizi sıfırlamak için şu kodu kullanın: {resetToken}"
            });
        }

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