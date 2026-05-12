using System.ComponentModel.DataAnnotations;

namespace DosyaYonetimPortali.MVC.Models.Entities
{
    

    public class DriveItem
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ParentId { get; set; }
        public string Name { get; set; }
        public bool IsFolder { get; set; }
        public string Extension { get; set; }
        public string Size { get; set; }
        public long SizeBytes { get; set; }
        public string ModifiedDate { get; set; }
        public string OwnerEmail { get; set; }
        public bool IsShared { get; set; }
        public string SharedWith { get; set; }
        public bool IsDeleted { get; set; }
        public string ContentType { get; set; }
        public string FileData { get; set; }
    }

    public class SystemLog
    {
        [Key]
        public int Id { get; set; }
        public string Status { get; set; }
        public string UserEmail { get; set; }
        public string Message { get; set; }
        public string Date { get; set; }
    }

    public class LoginRecord
    {
        [Key]
        public int Id { get; set; }
        public string UserEmail { get; set; }
        public string IpAddress { get; set; }
        public string BrowserDevice { get; set; }
        public string Status { get; set; }
        public string Date { get; set; }
    }
}