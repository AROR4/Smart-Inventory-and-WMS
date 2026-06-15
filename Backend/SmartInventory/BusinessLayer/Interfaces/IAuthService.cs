using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.DTOs.Common;

namespace SmartInventoryManagement.BusinessLayer.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(
            LoginRequestDto request);

        Task SetPasswordAsync(SetPasswordDto request);
    }
}