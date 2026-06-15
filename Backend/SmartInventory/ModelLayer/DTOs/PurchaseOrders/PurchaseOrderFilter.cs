using SmartInventoryManagement.Models.Enums;

namespace SmartInventoryManagement.Models.DTOs
{
    public class PurchaseOrderFilterDto
    {
        public PurchaseOrderStatus? Status { get; set; }

        public int? SupplierId { get; set; }

        public int? WarehouseId { get; set; }

        public string? Search { get; set; }
    }
}