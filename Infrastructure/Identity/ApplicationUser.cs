using Microsoft.AspNetCore.Identity;
using System;

namespace MonkMode.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser<int>
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}