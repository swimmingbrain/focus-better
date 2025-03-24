using System;

namespace MonkMode.DTOs
{
    public class FriendshipDto
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public bool IsIncoming { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? AcceptedDate { get; set; }
        public UserProfileDto Friend { get; set; }
    }

    public class FriendshipStatsDto
    {
        public int TotalFriends { get; set; }
        public int PendingIncoming { get; set; }
        public int PendingOutgoing { get; set; }
    }
}