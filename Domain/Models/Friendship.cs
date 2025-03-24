using MonkMode.Domain.Repositories;
using System;
using MonkMode.Domain.Enums;

namespace MonkMode.Domain.Models
{
    public class Friendship : IEntity
    {
        public int Id { get; set; }
        public int RequesterId { get; set; }
        public int RequesteeId { get; set; }
        public FriendshipStatus Status { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? AcceptedDate { get; set; }

        // navigation properties
        public User Requester { get; set; }
        public User Requestee { get; set; }
    }
}