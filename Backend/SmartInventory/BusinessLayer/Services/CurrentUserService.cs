using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SmartInventoryManagement.BusinessLayer.Interfaces;

namespace SmartInventoryManagement.BusinessLayer.Services
{
    public class CurrentUserService
        : ICurrentUserService
    {
        private readonly IHttpContextAccessor
            _httpContextAccessor;

        public CurrentUserService(
            IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor =
                httpContextAccessor;
        }

        public int UserId
        {
            get
            {
                var userId =
                    _httpContextAccessor
                        .HttpContext?
                        .User
                        .FindFirst(
                            ClaimTypes.NameIdentifier)
                        ?.Value;

                return int.Parse(userId!);
            }
        }

        public string Email =>
            _httpContextAccessor
                .HttpContext?
                .User
                .FindFirst(
                    ClaimTypes.Email)
                ?.Value ?? string.Empty;

        public string Role =>
            _httpContextAccessor
                .HttpContext?
                .User
                .FindFirst(
                    ClaimTypes.Role)
                ?.Value ?? string.Empty;

        public int? AssignedWarehouseId =>
            _httpContextAccessor
                .HttpContext?
                .User
                .FindFirst(
                    "AssignedWarehouseId")
                ?.Value != null
                ? int.Parse(
                    _httpContextAccessor
                        .HttpContext?
                        .User
                        .FindFirst(
                            "AssignedWarehouseId")
                        ?.Value!)
                : null;

       public int? SupplierId =>
            _httpContextAccessor
                .HttpContext?
                .User
                .FindFirst(
                    "SupplierId")
                ?.Value != null
                ? int.Parse(
                    _httpContextAccessor
                        .HttpContext?
                        .User
                        .FindFirst(
                            "SupplierId")
                        ?.Value!)
                : null;

}
}