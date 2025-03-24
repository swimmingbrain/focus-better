using MonkMode.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkMode.Domain.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User> GetUserWithProfileAsync(int userId);
        Task<User> GetUserByUserNameAsync(string userName);
        Task<User> GetUserByEmailAsync(string email);
        Task<List<User>> SearchUsersAsync(string query);
    }
}