using SmartInventoryManagement.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace SmartInventoryManagement.Models.DTOs
{
    public class CreateWarehouseDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string AddressLine1 { get; set; } = string.Empty;

        [StringLength(200)]
        public string? AddressLine2 { get; set; }

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string State { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string PostalCode { get; set; } = string.Empty;

        [Range(0.0001, double.MaxValue, ErrorMessage = "Capacity is required.")]
        public decimal Capacity { get; set; }

        [Required]
        public StorageType StorageType { get; set; }
    }
}