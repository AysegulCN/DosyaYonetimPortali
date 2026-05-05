namespace DosyaYonetimPortali.MVC.Models
{
    public class PermissionViewModel
    {
        public string ModuleName { get; set; }
        public bool AdminAccess { get; set; }
        public bool UserAccess { get; set; }
        public bool IsCore { get; set; }
    }
}