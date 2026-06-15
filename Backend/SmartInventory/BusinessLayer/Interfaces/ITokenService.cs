using System.Security.Claims;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Models.DTOs;

namespace SmartInventoryManagement.BusinessLayer.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(TokenRequest request);
        string GeneratePasswordSetupToken(int userId);

        ClaimsPrincipal ValidateToken(string token);
    }
}