using MonkMode.Domain.Models;
using MonkMode.Domain.Repositories;
using MonkMode.Domain.Services;
using System;
using System.Threading.Tasks;
using MonkMode.Domain.Enums;

namespace MonkMode.Infrastructure.Services
{
    public class FriendshipService : IFriendshipService
    {
        private readonly IFriendshipRepository _friendshipRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationService _notificationService;

        public FriendshipService(
            IFriendshipRepository friendshipRepository,
            IUserRepository userRepository,
            INotificationService notificationService)
        {
            _friendshipRepository = friendshipRepository;
            _userRepository = userRepository;
            _notificationService = notificationService;
        }

        public async Task<ServiceResult<Friendship>> SendFriendRequestAsync(int requesterId, string requesteeUserName)
        {
            try
            {
                // find requestee
                var requestee = await _userRepository.GetUserByUserNameAsync(requesteeUserName);
                if (requestee == null)
                {
                    return ServiceResult<Friendship>.CreateError("User not found", "NotFound");
                }

                int requesteeId = requestee.Id;

                // make sure user is not searching himself
                if (requesterId == requesteeId)
                {
                    return ServiceResult<Friendship>.CreateError("You cannot send a friend request to yourself", "Conflict");
                }

                // check if friendship already exists
                var existingFriendship = await _friendshipRepository.GetFriendshipAsync(requesterId, requesteeId);
                if (existingFriendship != null)
                {
                    switch (existingFriendship.Status)
                    {
                        case FriendshipStatus.PENDING:
                            // if current user is the requestee, accept the request
                            if (existingFriendship.RequesteeId == requesterId)
                            {
                                return await RespondToFriendRequestAsync(existingFriendship.Id, requesterId, true);
                            }
                            // otherwise, request already exists
                            return ServiceResult<Friendship>.CreateError("Friend request already sent", "Conflict");

                        case FriendshipStatus.ACCEPTED:
                            return ServiceResult<Friendship>.CreateError("Already friends", "Conflict");

                        case FriendshipStatus.REJECTED:
                            // if it was previously rejected
                            existingFriendship.Status = FriendshipStatus.PENDING;
                            existingFriendship.RequestDate = DateTime.UtcNow;
                            await _friendshipRepository.UpdateAsync(existingFriendship);
                            await _notificationService.CreateFriendRequestNotificationAsync(requesteeId, existingFriendship);
                            return ServiceResult<Friendship>.CreateSuccess(existingFriendship);

                        case FriendshipStatus.BLOCKED:
                            // don't tell user he's been blocked
                            return ServiceResult<Friendship>.CreateError("Cannot send friend request at this time", "Conflict");
                    }
                }

                // create friendship
                var friendship = new Friendship
                {
                    RequesterId = requesterId,
                    RequesteeId = requesteeId,
                    Status = FriendshipStatus.PENDING,
                    RequestDate = DateTime.UtcNow
                };

                var createdFriendship = await _friendshipRepository.AddAsync(friendship);

                // send notification to requestee
                await _notificationService.CreateFriendRequestNotificationAsync(requesteeId, createdFriendship);

                return ServiceResult<Friendship>.CreateSuccess(createdFriendship);
            }
            catch (Exception ex)
            {
                return ServiceResult<Friendship>.CreateError($"Failed to send friend request: {ex.Message}");
            }
        }

        public async Task<ServiceResult<Friendship>> RespondToFriendRequestAsync(int friendshipId, int userId, bool accept)
        {
            try
            {
                var friendship = await _friendshipRepository.FindByIdAsync(friendshipId);
                if (friendship == null)
                {
                    return ServiceResult<Friendship>.CreateError("Friend request not found", "NotFound");
                }

                // make sure user is requestee
                if (friendship.RequesteeId != userId)
                {
                    return ServiceResult<Friendship>.CreateError("You cannot respond to this friend request", "Unauthorized");
                }

                // make sure friendship is in pending state
                if (friendship.Status != FriendshipStatus.PENDING)
                {
                    return ServiceResult<Friendship>.CreateError("This friend request is no longer pending", "Conflict");
                }

                // update status
                friendship.Status = accept ? FriendshipStatus.ACCEPTED : FriendshipStatus.REJECTED;

                if (accept)
                {
                    friendship.AcceptedDate = DateTime.UtcNow;
                    await _notificationService.CreateFriendAcceptedNotificationAsync(friendship.RequesterId, friendship);
                }

                await _friendshipRepository.UpdateAsync(friendship);

                return ServiceResult<Friendship>.CreateSuccess(friendship);
            }
            catch (Exception ex)
            {
                return ServiceResult<Friendship>.CreateError($"Failed to respond to friend request: {ex.Message}");
            }
        }

        public async Task<FriendshipStats> GetFriendshipStatsAsync(int userId)
        {
            var stats = new FriendshipStats();

            var friends = await _friendshipRepository.GetFriendshipsForUserAsync(userId, FriendshipStatus.ACCEPTED);
            stats.TotalFriends = friends.Count;

            var pendingReceived = await _friendshipRepository.GetIncomingRequestsAsync(userId);
            stats.PendingIncoming = pendingReceived.Count;

            var pendingSent = await _friendshipRepository.GetOutgoingRequestsAsync(userId);
            stats.PendingOutgoing = pendingSent.Count;

            return stats;
        }
    }
}