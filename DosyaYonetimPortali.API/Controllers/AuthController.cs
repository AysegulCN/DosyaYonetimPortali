using DosyaYonetimPortali.API.DTOs;
using DosyaYonetimPortali.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace DosyaYonetimPortali.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // Sistemi ilk kurduğumuzda rolleri (Admin, User, Premium) oluşturmak için tek seferlik tetikleyeceğimiz uç
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
    }
}