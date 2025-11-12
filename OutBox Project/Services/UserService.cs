using Microsoft.EntityFrameworkCore;
using OutBox_Project.Models;
using OutBox_Project.Services.Interfaces;

namespace OutBox_Project.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User> SignUpAsync(string username)
        {
            if (await _context.Users.AnyAsync(u => u.Username == username))
                throw new InvalidOperationException("Username already exists.");

            var wallid = Guid.NewGuid();
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                Wallet = new Wallet { Id = wallid, Balance = 0 },
                WalletId = wallid
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User?> LoginAsync(string username)
        {
            var user = await _context.Users
                .Include(u => u.Wallet)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return null;

            return user;
        }

        public async Task<User?> GetByIdAsync(Guid userId)
        {
            return await _context.Users
                .Include(u => u.Wallet)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }
    }
}
