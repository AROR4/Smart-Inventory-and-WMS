namespace SmartInventoryManagement.Models.DTOs
{
    public class InventoryFilterDto
    {
        public int? WarehouseId { get; set; }

        public int? ProductId { get; set; }

        public string? Search { get; set; }
    }
}