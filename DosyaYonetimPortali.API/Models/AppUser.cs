using Microsoft.AspNetCore.Identity;
using System.IO.Compression;

namespace DosyaYonetimPortali.API.Models
{
    public class AppUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public ICollection<Folder> Folders { get; set; }
        public ICollection<AppFile> Files { get; set; }
        public string? AvatarPath { get; set; } 

        public long TotalStorageQuota { get; set; } = 104857600; // Varsayılan 100MB (veya istediğin bir değer)
        public long UsedStorage { get; set; } = 0;
    }
}