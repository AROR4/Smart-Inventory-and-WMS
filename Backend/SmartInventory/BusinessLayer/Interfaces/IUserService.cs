using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.DTOs.Common;

namespace SmartInventoryManagement.BusinessLayer.Interfaces
{
    public interface IUserService
    {
        Task CreateUserAsync(CreateUserDto request);
        Task<PagedResponseDto<UserResponseDto>> GetUsersAsync(PaginationParams pagination);
        Task<UserResponseDto?> GetUserByIdAsync(int id);
        Task<IEnumerable<UserResponseDto>> GetInactiveUsersAsync();
        Task ResendInviteAsync(int userId);
        Task<PagedResponseDto<UserResponseDto>>GetUsersAsync( PaginationParams pagination, string? role = null);
    }
}