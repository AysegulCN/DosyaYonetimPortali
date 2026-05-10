namespace DosyaYonetimPortali.MVC.Models
{
    public class DriveItemViewModel
    {
        public string Id { get; set; }
        public string ParentId { get; set; }
        public string Name { get; set; }
        public bool IsFolder { get; set; }
        public string Extension { get; set; }
        public string Size { get; set; }
        public string ModifiedDate { get; set; }
        public string Owner { get; set; }
        public bool IsDeleted { get; set; } = false;
        public bool IsShared { get; set; } = false;
        public string SharedWith { get; set; }

        // YENİ: Gerçek dosya içeriğini ve türünü hafızada tutacak alanlar
        public string FileData { get; set; }
        public string ContentType { get; set; }
    }
}