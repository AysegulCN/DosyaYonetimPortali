using DosyaYonetimPortali.API.DTOs;
using DosyaYonetimPortali.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DosyaYonetimPortali.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;

        public SubscriptionController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        [AllowAnonymous] 
        [HttpGet("packages")]
        public IActionResult GetPackages()
        {
            var packages = new List<object>
            {
                new { Id = 1, Name = "Basic Plan", Price = 0, Quota = "100 MB", Description = "Sadece temel dosyalarınız için ücretsiz başlangıç." },
                new { Id = 2, Name = "Premium Plan", Price = 49.99, Quota = "5 GB", Description = "Daha fazla alan ve rahat kullanım." },
                new { Id = 3, Name = "Ultra Pro Plan", Price = 149.99, Quota = "50 GB", Description = "Profesyoneller ve şirketler için devasa alan." }
            };

            return Ok(packages);
        }

        [Authorize]
        [HttpPost("upgrade")]
        public async Task<IActionResult> UpgradeAccount([FromBody] PaymentRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (request.CardNumber.StartsWith("4") == false)
            {
                return BadRequest(new { Message = "Ödeme reddedildi. Geçersiz kart numarası!" });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (request.PackageId == 2)
            {
                await _userManager.AddToRoleAsync(user, "PremiumUser");
            }
            else if (request.PackageId == 3)
            {
                await _userManager.AddToRoleAsync(user, "PremiumUser");
            }
            else
            {
                return BadRequest("Geçersiz paket seçimi.");
            }

            
            return Ok(new
            {
                Message = "Ödeme başarılı! Hesabınız Premium'a yükseltildi.",
                ActionRequired = "Lütfen yetkilerinizin güncellenmesi için çıkış yapıp tekrar giriş yapın."
            });
        }
    }
}