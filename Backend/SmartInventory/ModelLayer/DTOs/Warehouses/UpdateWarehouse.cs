using SmartInventoryManagement.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace SmartInventoryManagement.Models.DTOs
{
    public class UpdateWarehouseDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? AddressLine1 { get; set; }

        [StringLength(200)]
        public string? AddressLine2 { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? State { get; set; }

        [StringLength(20)]
        public string? PostalCode { get; set; }

        [Required]
        public int Capacity { get; set; }

        [Required]
        public StorageType StorageType { get; set; }
    }
}