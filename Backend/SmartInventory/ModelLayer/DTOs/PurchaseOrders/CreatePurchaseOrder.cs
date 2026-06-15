using System.ComponentModel.DataAnnotations;

namespace SmartInventoryManagement.Models.DTOs
{
    public class CreatePurchaseOrderDto
    {
        [Required]
        public int SupplierId { get; set; }

        [Required]
        public int WarehouseId { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [Required]
        public List<CreatePurchaseOrderItemDto> Items { get; set; }
            = new();
    }
}