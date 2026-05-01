using DosyaYonetimPortali.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DosyaYonetimPortali.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Sadece Token'ı olan ve Admin olanlar buraya istek atabilir!
    public class UsersController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;

        public UsersController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        // Tüm kullanıcıları listeleyen Endpoint
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var userList = new List<object>();

            // Kullanıcıların rollerini de çekip listeye ekliyoruz
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new
                {
                    Id = user.Id,
                    FullName = user.FirstName + " " + user.LastName,
                    Email = user.Email,
                    Role = roles.FirstOrDefault() ?? "User"
                });
            }

            return Ok(userList);
        }
        // 1. Kullanıcıyı Sistemden Tamamen Silme
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            // Güvenlik: Kimse başka bir Admin'i silemesin
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin")) return BadRequest("Yöneticiler sistemden silinemez.");

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded) return Ok(new { Message = "Kullanıcı başarıyla silindi." });

            return BadRequest("Silme işlemi başarısız oldu.");
        }

        // 2. Kullanıcının Yetkisini Değiştirme (Admin <-> User)
        [HttpPost("toggle-role/{id}")]
        public async Task<IActionResult> ToggleUserRole(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            var roles = await _userManager.GetRolesAsync(user);
            bool isAdmin = roles.Contains("Admin");

            if (isAdmin)
            {
                // Zaten Admin ise, yetkisini al ve normal User yap
                await _userManager.RemoveFromRoleAsync(user, "Admin");
                await _userManager.AddToRoleAsync(user, "User");
            }
            else
            {
                // Normal User ise, onu Admin statüsüne yükselt
                await _userManager.RemoveFromRoleAsync(user, "User");
                await _userManager.AddToRoleAsync(user, "Admin");
            }

            return Ok(new { Message = "Kullanıcı yetkisi güncellendi." });
        }

    }
}