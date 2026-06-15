using System.ComponentModel.DataAnnotations;

namespace SmartInventoryManagement.Models.DTOs
{
    public class CreateUserDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public int RoleId { get; set; }

        public int? AssignedWarehouseId { get; set; }

        public string? SupplierCompanyName { get; set; }

        public string? SupplierEmail { get; set; }

        public string? SupplierPhone { get; set; }

        public string? SupplierAddress { get; set; }
    }
}