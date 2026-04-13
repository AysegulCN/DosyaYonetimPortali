namespace DosyaYonetimPortali.API.DTOs
{
    public class ShareRequestDto
    {
        public int FileId { get; set; }
        public string SharedWithUserId { get; set; }
        public bool CanEdit { get; set; }
    }
}