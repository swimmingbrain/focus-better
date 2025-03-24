using MonkMode.Domain.Models;
using MonkMode.DTOs;
using System.Threading.Tasks;

namespace MonkMode.Domain.Services
{
    public interface IAuthService
    {
        Task<ServiceResult<User>> RegisterUserAsync(RegisterDto registerDto);
        Task<ServiceResult<User>> AuthenticateAsync(string email, string password);
        Task<string> GenerateJwtTokenAsync(User user);
        Task<ServiceResult<User>> GetUserFromTokenAsync(string token);
    }

    public class ServiceResult<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ErrorType { get; set; }
        public T Data { get; set; }

        public static ServiceResult<T> CreateSuccess(T data, string message = null)
        {
            return new ServiceResult<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ServiceResult<T> CreateError(string message, string errorType = null)
        {
            return new ServiceResult<T>
            {
                Success = false,
                Message = message,
                ErrorType = errorType,
                Data = default
            };
        }
    }
}