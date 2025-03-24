using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MonkMode.Domain.Models;
using MonkMode.Domain.Services;
using MonkMode.DTOs;
using MonkMode.Infrastructure.Identity;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace MonkMode.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        public async Task<ServiceResult<User>> RegisterUserAsync(RegisterDto registerDto)
        {
            // check if email is already in use
            if (await _userManager.FindByEmailAsync(registerDto.Email) != null)
            {
                return ServiceResult<User>.CreateError("Email is already in use");
            }

            // check if username is already in use
            if (await _userManager.FindByNameAsync(registerDto.UserName) != null)
            {
                return ServiceResult<User>.CreateError("Username is already in use");
            }

            // create new application user
            var appUser = new ApplicationUser
            {
                UserName = registerDto.UserName,
                Email = registerDto.Email,
                CreatedAt = DateTime.UtcNow
            };

            // create user in identity system
            var result = await _userManager.CreateAsync(appUser, registerDto.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ServiceResult<User>.CreateError($"Failed to create user: {errors}");
            }

            // create domain user
            var user = new User
            {
                Id = appUser.Id,
                UserName = appUser.UserName,
                Email = appUser.Email,
                CreatedAt = appUser.CreatedAt,
                Profile = new UserProfile
                {
                    UserId = appUser.Id,
                    DisplayName = appUser.UserName
                }
            };

            return ServiceResult<User>.CreateSuccess(user);
        }

        public async Task<ServiceResult<User>> AuthenticateAsync(string email, string password)
        {
            var appUser = await _userManager.FindByEmailAsync(email);
            if (appUser == null)
            {
                return ServiceResult<User>.CreateError("Invalid email or password");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(appUser, password, false);
            if (!result.Succeeded)
            {
                return ServiceResult<User>.CreateError("Invalid email or password");
            }

            var user = new User
            {
                Id = appUser.Id,
                UserName = appUser.UserName,
                Email = appUser.Email,
                CreatedAt = appUser.CreatedAt
            };

            return ServiceResult<User>.CreateSuccess(user);
        }

        public async Task<string> GenerateJwtTokenAsync(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
            var expires = DateTime.UtcNow.AddDays(Convert.ToDouble(_configuration["Jwt:ExpiryInDays"]));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expires,
                SigningCredentials = creds,
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        public async Task<ServiceResult<User>> GetUserFromTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                {
                    return ServiceResult<User>.CreateError("Invalid token");
                }

                var appUser = await _userManager.FindByIdAsync(userId);
                if (appUser == null)
                {
                    return ServiceResult<User>.CreateError("User not found");
                }

                var user = new User
                {
                    Id = appUser.Id,
                    UserName = appUser.UserName,
                    Email = appUser.Email,
                    CreatedAt = appUser.CreatedAt
                };

                return ServiceResult<User>.CreateSuccess(user);
            }
            catch (Exception ex)
            {
                return ServiceResult<User>.CreateError($"Failed to validate token: {ex.Message}");
            }
        }
    }
}