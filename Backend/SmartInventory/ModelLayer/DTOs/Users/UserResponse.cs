namespace SmartInventoryManagement.Models.DTOs
{
    public class UserResponseDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public int? AssignedWarehouseId { get; set; }

        public string? SupplierCompanyName { get; set; }

        public string? SupplierEmail { get; set; }

        public string? SupplierPhone { get; set; }

        public string? SupplierAddress { get; set; }

    }
}