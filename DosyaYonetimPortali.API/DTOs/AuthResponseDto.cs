namespace DosyaYonetimPortali.API.DTOs
{
    public class AuthResponseDto
    {
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Token { get; set; }
        public DateTime? Expiration { get; set; }
        public string? FirstName { get; set; }
    }
}