using MonkMode.Domain.Repositories;
using System.Security.Principal;

namespace MonkMode.Domain.Models
{
    public class UserProfile : IEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string DisplayName { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string Bio { get; set; }

        // navigation properties
        public User User { get; set; }
    }
}