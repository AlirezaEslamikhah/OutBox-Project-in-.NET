using OutBox_Project.Models;

namespace OutBox_Project.Services.Interfaces
{
    public interface IUserService
    {
        Task<User> SignUpAsync(string username);
        Task<User?> LoginAsync(string username);
        Task<User?> GetByIdAsync(Guid userId);
    }

}
