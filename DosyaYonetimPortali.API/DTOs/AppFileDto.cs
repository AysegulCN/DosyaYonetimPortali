namespace DosyaYonetimPortali.API.DTOs
{
    public class AppFileDto
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string Extension { get; set; }
        public long Size { get; set; }
        public string PhysicalPath { get; set; } 
        public DateTime UploadDate { get; set; }
        public int? FolderId { get; set; }
    }
}