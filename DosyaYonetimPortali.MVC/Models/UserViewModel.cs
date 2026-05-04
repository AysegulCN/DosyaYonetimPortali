namespace DosyaYonetimPortali.MVC.Models
{
    public class UserViewModel
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }

        // Arayüzde Ad ve Soyadı birleşik göstermek için pratik bir özellik:
        public string FullName => $"{FirstName} {LastName}";
    }
}