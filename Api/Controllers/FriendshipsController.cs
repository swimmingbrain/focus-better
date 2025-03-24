using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonkMode.Domain.Models;
using MonkMode.Domain.Repositories;
using MonkMode.Domain.Services;
using MonkMode.DTOs;
using MonkMode.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace MonkMode.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class FriendshipsController : ControllerBase
    {
        private readonly IFriendshipRepository _friendshipRepository;
        private readonly IUserRepository _userRepository;
        private readonly IFriendshipService _friendshipService;
        private readonly INotificationService _notificationService;

        public FriendshipsController(
            IFriendshipRepository friendshipRepository,
            IUserRepository userRepository,
            IFriendshipService friendshipService,
            INotificationService notificationService)
        {
            _friendshipRepository = friendshipRepository;
            _userRepository = userRepository;
            _friendshipService = friendshipService;
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FriendshipDto>>> GetFriendships([FromQuery] string status = null)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // parse status string
            FriendshipStatus? statusFilter = null;
            if (!string.IsNullOrEmpty(status))
            {
                // manual parsing
                if (Enum.TryParse(typeof(FriendshipStatus), status, true, out object result))
                {
                    statusFilter = (FriendshipStatus)result;
                }
            }

            // get friendship without status, avoid conflicts
            List<Friendship> friendships;
            if (statusFilter.HasValue)
            {
                // if still type conflicts:
                friendships = await _friendshipRepository.GetFriendshipsForUserAsync(userId);
                friendships = friendships.Where(f => f.Status == statusFilter.Value).ToList();
            }
            else
            {
                friendships = await _friendshipRepository.GetFriendshipsForUserAsync(userId);
            }

            var friendshipDtos = new List<FriendshipDto>();

            foreach (var friendship in friendships)
            {
                // user requester or requestee?
                bool isRequester = friendship.RequesterId == userId;
                int otherUserId = isRequester ? friendship.RequesteeId : friendship.RequesterId;

                // get other user'S details
                var otherUser = await _userRepository.FindByIdAsync(otherUserId);
                if (otherUser == null) continue;

                friendshipDtos.Add(new FriendshipDto
                {
                    Id = friendship.Id,
                    Status = friendship.Status.ToString(),
                    IsIncoming = !isRequester,
                    RequestDate = friendship.RequestDate,
                    AcceptedDate = friendship.AcceptedDate,
                    Friend = new UserProfileDto
                    {
                        UserName = otherUser.UserName,
                        DisplayName = otherUser.Profile?.DisplayName ?? otherUser.UserName,
                        ProfilePictureUrl = otherUser.Profile?.ProfilePictureUrl,
                        Bio = otherUser.Profile?.Bio
                    }
                });
            }

            return friendshipDtos;
        }

        [HttpGet("pending")]
        public async Task<ActionResult<IEnumerable<FriendshipDto>>> GetPendingRequests()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // incoming freind requests
            var pendingRequests = await _friendshipRepository.GetIncomingRequestsAsync(userId);

            var requestDtos = new List<FriendshipDto>();

            foreach (var request in pendingRequests)
            {
                var requester = await _userRepository.FindByIdAsync(request.RequesterId);
                if (requester == null) continue;

                requestDtos.Add(new FriendshipDto
                {
                    Id = request.Id,
                    Status = request.Status.ToString(),
                    IsIncoming = true,
                    RequestDate = request.RequestDate,
                    Friend = new UserProfileDto
                    {
                        UserName = requester.UserName,
                        DisplayName = requester.Profile?.DisplayName ?? requester.UserName,
                        ProfilePictureUrl = requester.Profile?.ProfilePictureUrl,
                        Bio = requester.Profile?.Bio
                    }
                });
            }

            return requestDtos;
        }

        [HttpPost("request/{userName}")]
        public async Task<ActionResult<FriendshipDto>> SendFriendRequest(string userName)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var result = await _friendshipService.SendFriendRequestAsync(userId, userName);

            if (!result.Success)
            {
                return result.ErrorType switch
                {
                    "NotFound" => NotFound(result.Message),
                    "Conflict" => Conflict(result.Message),
                    _ => BadRequest(result.Message)
                };
            }

            // get other user's details for the response
            var otherUser = await _userRepository.GetUserByUserNameAsync(userName);

            return new FriendshipDto
            {
                Id = result.Data.Id,
                Status = result.Data.Status.ToString(),
                IsIncoming = false,
                RequestDate = result.Data.RequestDate,
                Friend = new UserProfileDto
                {
                    UserName = otherUser.UserName,
                    DisplayName = otherUser.Profile?.DisplayName ?? otherUser.UserName,
                    ProfilePictureUrl = otherUser.Profile?.ProfilePictureUrl,
                    Bio = otherUser.Profile?.Bio
                }
            };
        }

        [HttpPost("{id}/accept")]
        public async Task<ActionResult<FriendshipDto>> AcceptFriendRequest(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var result = await _friendshipService.RespondToFriendRequestAsync(id, userId, true);

            if (!result.Success)
            {
                return result.ErrorType switch
                {
                    "NotFound" => NotFound(result.Message),
                    "Unauthorized" => Forbid(),
                    "Conflict" => Conflict(result.Message),
                    _ => BadRequest(result.Message)
                };
            }

            // get requester's details
            var otherUserId = result.Data.RequesterId;
            var otherUser = await _userRepository.FindByIdAsync(otherUserId);

            return new FriendshipDto
            {
                Id = result.Data.Id,
                Status = result.Data.Status.ToString(),
                IsIncoming = true,
                RequestDate = result.Data.RequestDate,
                AcceptedDate = result.Data.AcceptedDate,
                Friend = new UserProfileDto
                {
                    UserName = otherUser.UserName,
                    DisplayName = otherUser.Profile?.DisplayName ?? otherUser.UserName,
                    ProfilePictureUrl = otherUser.Profile?.ProfilePictureUrl,
                    Bio = otherUser.Profile?.Bio
                }
            };
        }

        [HttpPost("{id}/reject")]
        public async Task<ActionResult> RejectFriendRequest(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var result = await _friendshipService.RespondToFriendRequestAsync(id, userId, false);

            if (!result.Success)
            {
                return result.ErrorType switch
                {
                    "NotFound" => NotFound(result.Message),
                    "Unauthorized" => Forbid(),
                    "Conflict" => Conflict(result.Message),
                    _ => BadRequest(result.Message)
                };
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> RemoveFriendship(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var friendship = await _friendshipRepository.FindByIdAsync(id);

            if (friendship == null)
                return NotFound();

            if (friendship.RequesterId != userId && friendship.RequesteeId != userId)
                return Forbid();

            await _friendshipRepository.DeleteAsync(friendship);
            return NoContent();
        }

        [HttpGet("stats")]
        public async Task<ActionResult<FriendshipStatsDto>> GetFriendshipStats()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var stats = await _friendshipService.GetFriendshipStatsAsync(userId);

            return new FriendshipStatsDto
            {
                TotalFriends = stats.TotalFriends,
                PendingIncoming = stats.PendingIncoming,
                PendingOutgoing = stats.PendingOutgoing
            };
        }
    }
}