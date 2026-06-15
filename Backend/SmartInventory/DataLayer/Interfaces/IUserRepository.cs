using SmartInventoryManagement.Models;

namespace SmartInventoryManagement.DataLayer.Interfaces
{
    public interface IUserRepository: IRepository<User>
    {
        Task<User?> GetUserByEmailAsync(string email);

        Task<User?> GetUserWithRoleAsync(int id);

        Task<IEnumerable<User>> GetAllUsersWithRoleAsync();

    }
}