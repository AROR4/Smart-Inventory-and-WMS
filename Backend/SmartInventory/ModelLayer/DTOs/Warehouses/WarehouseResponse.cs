using SmartInventoryManagement.Models.Enums;

namespace SmartInventoryManagement.Models.DTOs
{
    public class WarehouseResponseDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string AddressLine1 { get; set; } = string.Empty;

        public string? AddressLine2 { get; set; }

        public string City { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public string PostalCode { get; set; } = string.Empty;

        public int Capacity { get; set; }

        public int AvailableCapacity { get; set; }

        public decimal ReservedCapacity { get; set; }

        public decimal EffectiveCapacity { get; set; }

        public StorageType StorageType { get; set; }

        public bool IsActive { get; set; }
    }
}