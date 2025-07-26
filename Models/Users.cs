using Microsoft.AspNetCore.Identity;

namespace SmartComply.Models
{
    public class Users : IdentityUser
    {
        public string FullName { get; set; }
    }
}
