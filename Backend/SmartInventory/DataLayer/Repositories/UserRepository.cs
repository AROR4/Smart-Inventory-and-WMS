using Microsoft.EntityFrameworkCore;
using SmartInventoryManagement.Data;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.Models;
namespace SmartInventoryManagement.DataLayer.Repositories
{
    public class UserRepository
        : Repository<User>,
          IUserRepository
    {
        public UserRepository(
            ApplicationDbContext context)
            : base(context)
        {
        }

        public async Task<User?> GetUserByEmailAsync(
            string email)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(
                    u => u.Email == email);
        }

        public async Task<User?> GetUserWithRoleAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<IEnumerable<User>> GetAllUsersWithRoleAsync()
        {
            return await _context.Users
                .Include(u => u.Role)
                .Include(u=>u.Supplier)
                .ToListAsync();
        }

        
    }
}