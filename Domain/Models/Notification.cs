using MonkMode.Domain.Repositories;
using System;
using System.Security.Principal;
using MonkMode.Domain.Enums;

namespace MonkMode.Domain.Models
{
    public class Notification : IEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public NotificationType Type { get; set; }
        public string Message { get; set; }
        public string RelatedEntityId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        // navigation properties
        public User User { get; set; }
    }
}