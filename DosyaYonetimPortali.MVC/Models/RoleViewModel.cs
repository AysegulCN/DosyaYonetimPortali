namespace DosyaYonetimPortali.MVC.Models
{
    public class RoleViewModel
    {
        public string RoleName { get; set; }
        public int UserCount { get; set; }
        public bool IsSystemRole { get; set; } // Admin gibi silinemez roller için
    }
}