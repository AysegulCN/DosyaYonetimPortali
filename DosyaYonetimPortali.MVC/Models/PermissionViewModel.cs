using System.Collections.Generic;

namespace DosyaYonetimPortali.MVC.Models
{
    public class PermissionViewModel
    {
        public string ModuleName { get; set; }
        public bool IsCore { get; set; }
        public List<RoleAccessViewModel> RoleAccesses { get; set; } = new List<RoleAccessViewModel>();
    }
}