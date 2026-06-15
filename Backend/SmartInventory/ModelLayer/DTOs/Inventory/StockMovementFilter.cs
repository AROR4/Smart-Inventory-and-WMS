using SmartInventoryManagement.Models.Enums;

namespace SmartInventoryManagement.Models.DTOs
{
    public class StockMovementFilterDto
    {
        public int? ProductId { get; set; }

        public int? WarehouseId { get; set; }

        public StockMovementType? Type { get; set; }

        public string? Search { get; set; }
    }
}