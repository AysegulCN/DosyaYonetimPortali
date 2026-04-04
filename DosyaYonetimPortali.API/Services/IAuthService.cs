using DosyaYonetimPortali.API.DTOs;

namespace DosyaYonetimPortali.API.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(UserRegisterDto request);

        Task<AuthResponseDto> LoginAsync(UserLoginDto request);

        Task CreateDefaultRolesAsync();
    }
}