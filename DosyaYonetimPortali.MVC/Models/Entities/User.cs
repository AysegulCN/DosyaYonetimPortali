using System;

namespace DosyaYonetimPortali.MVC.Models.Entities
{
    public class User
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public string ProfilePictureUrl { get; set; }

        public bool EmailNotifications { get; set; } = true;
        public bool SystemNotifications { get; set; } = true;
    }
}