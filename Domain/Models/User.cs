using MonkMode.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace MonkMode.Domain.Models
{
    public class User : IEntity
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }

        // navigation properties
        public UserProfile Profile { get; set; }
        public List<TaskItem> Tasks { get; set; }
        public List<TimeBlock> TimeBlocks { get; set; }
        public List<FocusSession> FocusSessions { get; set; }
        public List<Notification> Notifications { get; set; }
        public List<Friendship> SentFriendRequests { get; set; }
        public List<Friendship> ReceivedFriendRequests { get; set; }
        public ExportSettings ExportSettings { get; set; }
    }
}