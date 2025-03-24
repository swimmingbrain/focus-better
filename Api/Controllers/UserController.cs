using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonkMode.Domain.Models;
using MonkMode.Domain.Repositories;
using MonkMode.DTOs;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MonkMode.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        public UserController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpGet("profile")]
        public async Task<ActionResult<UserProfileDto>> GetProfile()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userWithProfile = await _userRepository.GetUserWithProfileAsync(userId);

            if (userWithProfile == null)
                return NotFound();

            return new UserProfileDto
            {
                UserName = userWithProfile.UserName,
                DisplayName = userWithProfile.Profile?.DisplayName ?? userWithProfile.UserName,
                ProfilePictureUrl = userWithProfile.Profile?.ProfilePictureUrl,
                Bio = userWithProfile.Profile?.Bio
            };
        }

        [HttpPut("profile")]
        public async Task<ActionResult<UserProfileDto>> UpdateProfile(UpdateProfileDto profileDto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userWithProfile = await _userRepository.GetUserWithProfileAsync(userId);

            if (userWithProfile == null)
                return NotFound();

            if (userWithProfile.Profile == null)
            {
                userWithProfile.Profile = new UserProfile
                {
                    UserId = userId,
                    DisplayName = profileDto.DisplayName ?? userWithProfile.UserName,
                    ProfilePictureUrl = profileDto.ProfilePictureUrl,
                    Bio = profileDto.Bio
                };
            }
            else
            {
                userWithProfile.Profile.DisplayName = profileDto.DisplayName ?? userWithProfile.Profile.DisplayName;
                userWithProfile.Profile.ProfilePictureUrl = profileDto.ProfilePictureUrl ?? userWithProfile.Profile.ProfilePictureUrl;
                userWithProfile.Profile.Bio = profileDto.Bio ?? userWithProfile.Profile.Bio;
            }

            await _userRepository.UpdateAsync(userWithProfile);

            return new UserProfileDto
            {
                UserName = userWithProfile.UserName,
                DisplayName = userWithProfile.Profile.DisplayName,
                ProfilePictureUrl = userWithProfile.Profile.ProfilePictureUrl,
                Bio = userWithProfile.Profile.Bio
            };
        }

        [HttpGet("{userName}")]
        public async Task<ActionResult<UserProfileDto>> GetUserByUserName(string userName)
        {
            var user = await _userRepository.GetUserByUserNameAsync(userName);

            if (user == null)
                return NotFound();

            // only return public profile information to other users
            return new UserProfileDto
            {
                UserName = user.UserName,
                DisplayName = user.Profile?.DisplayName ?? user.UserName,
                ProfilePictureUrl = user.Profile?.ProfilePictureUrl,
                Bio = user.Profile?.Bio
            };
        }

        [HttpGet("search")]
        public async Task<ActionResult<UserProfileDto[]>> SearchUsers([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
                return BadRequest("Search query must be at least 3 characters long");

            var users = await _userRepository.SearchUsersAsync(query);

            var userDtos = users.Select(u => new UserProfileDto
            {
                UserName = u.UserName,
                DisplayName = u.Profile?.DisplayName ?? u.UserName,
                ProfilePictureUrl = u.Profile?.ProfilePictureUrl,
                Bio = u.Profile?.Bio
            }).ToArray();

            return userDtos;
        }
    }
}